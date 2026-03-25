using NUnit.Framework;
using NSubstitute;
using System;

namespace SmartTrafficLight.Tests
{
    [TestFixture]
    public class TrafficControllerTests
    {
        private TrafficController controller;
        private IVehicleSignalManager mockVehicle;
        private IPedestrianSignalManager mockPedestrian;
        private ITimeManager mockTime;
        private IWebService mockWeb;
        private IEmailService mockEmail;

        [SetUp]
        public void Setup()
        {
            mockVehicle = Substitute.For<IVehicleSignalManager>();
            mockPedestrian = Substitute.For<IPedestrianSignalManager>();
            mockTime = Substitute.For<ITimeManager>();
            mockWeb = Substitute.For<IWebService>();
            mockEmail = Substitute.For<IEmailService>();
        }

        #region L1 - Basic Setup and State Management

        [Test] // L1R1
        public void ConstructorSetsIntersectionID()
        {
            controller = new TrafficController("test123");
            Assert.That(controller.GetIntersectionID(), Is.EqualTo("test123"));
        }

        [Test] // L1R2
        [TestCase("UPPERCASE")]
        [TestCase("MiXeD_CaSe")]
        [TestCase("lowercase")]
        public void ConstructorConvertsIDToLowercase(string input)
        {
            controller = new TrafficController(input);
            Assert.That(controller.GetIntersectionID(), Is.EqualTo(input.ToLower()));
        }

        [Test] // L1R2
        public void ConstructorHandlesNullID()
        {
            controller = new TrafficController(null);
            Assert.That(controller.GetIntersectionID(), Is.EqualTo(""));
        }

        [Test] // L1R3
        public void SetIntersectionIDConvertsToLowercase()
        {
            controller = new TrafficController("test");
            controller.SetIntersectionID("NEWSECTION");
            Assert.That(controller.GetIntersectionID(), Is.EqualTo("newsection"));
        }

        [Test] // L1R4
        public void ConstructorInitialStatesAreAmberAndWait()
        {
            controller = new TrafficController("test");
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("amber"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("wait"));
        }

        [Test] // L1R5
        [TestCase("red", "wait", true)]
        [TestCase("redamber", "walk", true)]
        [TestCase("green", "wait", true)]
        [TestCase("amber", "walk", true)]
        [TestCase("oosv", "oosp", true)]
        [TestCase("invalid", "wait", false)]
        [TestCase("red", "invalid", false)]
        public void SetStateDirectValidatesAndSetsStates(string vehicle, string pedestrian, bool expected)
        {
            controller = new TrafficController("test");
            bool result = controller.SetStateDirect(vehicle, pedestrian);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test] // L1R5
        public void SetStateDirectHandlesCaseInsensitivity()
        {
            controller = new TrafficController("test");
            Assert.That(controller.SetStateDirect("RED", "WALK"), Is.True);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("red"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("walk"));
        }

        [Test] // L1R5
        public void SetStateDirectDoesNotChangeStateOnInvalidInput()
        {
            controller = new TrafficController("test");
            string initialVehicle = controller.GetCurrentVehicleSignalState();
            string initialPed = controller.GetCurrentPedestrianSignalState();

            controller.SetStateDirect("invalid", "invalid");

            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo(initialVehicle));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo(initialPed));
        }

        #endregion

        #region L2 - State Transitions & Dependency Injection

        [Test] // L2R1 - Valid state transitions only
        [TestCase("green", "wait", "amber", "wait", true)]
        [TestCase("amber", "wait", "red", "walk", true)]
        [TestCase("red", "walk", "redamber", "walk", true)]
        [TestCase("redamber", "walk", "green", "wait", true)]
        [TestCase("green", "wait", "red", "wait", false)]
        [TestCase("red", "wait", "amber", "wait", false)]
        public void SetCurrentStateValidatesTransitions(string cv, string cp, string nv, string np, bool expected)
        {
            controller = new TrafficController("test", cv, cp);
            Assert.That(controller.SetCurrentState(nv, np), Is.EqualTo(expected));
        }

        [Test] // L2R2
        [TestCase("green", "wait")]
        [TestCase("amber", "walk")]
        [TestCase("red", "wait")]
        [TestCase("redamber", "walk")]
        public void ConstructorWithThreeParametersInitializesStates(string vehicle, string pedestrian)
        {
            controller = new TrafficController("test", vehicle, pedestrian);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo(vehicle));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo(pedestrian));
        }

        [Test] // L2R2
        public void ConstructorWithThreeParametersHandlesNullID()
        {
            controller = new TrafficController(null, "green", "wait");
            Assert.That(controller.GetIntersectionID(), Is.EqualTo(""));
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("green"));
        }

        [Test] // L2R2
        [TestCase("oosv", "wait")]
        [TestCase("green", "oosp")]
        [TestCase("invalid", "wait")]
        public void ConstructorThrowsOnInvalidInitialStates(string vehicle, string pedestrian)
        {
            var ex = Assert.Throws<ArgumentException>(() => new TrafficController("test", vehicle, pedestrian));
            Assert.That(ex.Message, Does.Contain("Argument Exception"));
        }

        [Test] // L2R3
        public void ConstructorWithDependencyInjection()
        {
            controller = new TrafficController("test", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            Assert.That(controller.GetIntersectionID(), Is.EqualTo("test"));
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("amber"));
        }

        [Test] // L2R3
        public void ConstructorWithDependencyInjectionHandlesNullID()
        {
            controller = new TrafficController(null, mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            Assert.That(controller.GetIntersectionID(), Is.EqualTo(""));
        }

        [Test] // L2R4
        public void GetStatusReportCombinesAllManagerStatuses()
        {
            mockVehicle.GetStatus().Returns("VehicleSignal,OK,OK,");
            mockPedestrian.GetStatus().Returns("PedestrianSignal,OK,");
            mockTime.GetStatus().Returns("Timer,OK,");

            controller = new TrafficController("test", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            string result = controller.GetStatusReport();

            Assert.That(result, Is.EqualTo("VehicleSignal,OK,OK,PedestrianSignal,OK,Timer,OK,"));
        }

        [Test] // L2R4
        public void GetStatusReportCallsAllManagerGetStatusMethods()
        {
            mockVehicle.GetStatus().Returns("VehicleSignal,OK,");
            mockPedestrian.GetStatus().Returns("PedestrianSignal,OK,");
            mockTime.GetStatus().Returns("Timer,OK,");

            controller = new TrafficController("test", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            controller.GetStatusReport();

            mockVehicle.Received(1).GetStatus();
            mockPedestrian.Received(1).GetStatus();
            mockTime.Received(1).GetStatus();
        }

        #endregion

        #region L3 - Advanced Transitions & Fault Handling

        [Test] // L3R1
        public void AmberToRedTransitionExecutesManagerCalls()
        {
            mockTime.Delay(3).Returns(true);
            mockVehicle.SetAllRed().Returns(true);
            mockPedestrian.SetWalk(true).Returns(true);
            mockPedestrian.SetAudible(true).Returns(true);

            controller = new TrafficController("test", "amber", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.SetCurrentState("red", "walk");

            Assert.That(result, Is.True);
            mockTime.Received(1).Delay(3);
            mockVehicle.Received(1).SetAllRed();
            mockPedestrian.Received(1).SetWalk(true);
            mockPedestrian.Received(1).SetAudible(true);
        }

        [Test] // L3R1
        public void AmberToRedTransitionFailsWhenDelayFails()
        {
            mockTime.Delay(3).Returns(false);

            controller = new TrafficController("test", "amber", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.SetCurrentState("red", "walk");

            Assert.That(result, Is.False);
            mockVehicle.DidNotReceive().SetAllRed();
        }

        [Test] // L3R1
        public void AmberToRedTransitionFailsWhenSetAllRedFails()
        {
            mockTime.Delay(3).Returns(true);
            mockVehicle.SetAllRed().Returns(false);

            controller = new TrafficController("test", "amber", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.SetCurrentState("red", "walk");

            Assert.That(result, Is.False);
            mockPedestrian.DidNotReceive().SetWalk(Arg.Any<bool>());
        }

        [Test] // L3R1
        public void AmberToRedTransitionFailsWhenSetWalkFails()
        {
            mockTime.Delay(3).Returns(true);
            mockVehicle.SetAllRed().Returns(true);
            mockPedestrian.SetWalk(true).Returns(false);

            controller = new TrafficController("test", "amber", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.SetCurrentState("red", "walk");

            Assert.That(result, Is.False);
            mockPedestrian.DidNotReceive().SetAudible(Arg.Any<bool>());
        }

        [Test] // L3R1
        public void AmberToRedTransitionFailsWhenSetAudibleFails()
        {
            mockTime.Delay(3).Returns(true);
            mockVehicle.SetAllRed().Returns(true);
            mockPedestrian.SetWalk(true).Returns(true);
            mockPedestrian.SetAudible(true).Returns(false);

            controller = new TrafficController("test", "amber", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.SetCurrentState("red", "walk");

            Assert.That(result, Is.False);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("amber"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("wait"));
        }

        [Test] // L3R1
        public void AmberToRedStateUnchangedAfterFailure()
        {
            mockTime.Delay(3).Returns(false);

            controller = new TrafficController("test", "amber", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            controller.SetCurrentState("red", "walk");

            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("amber"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("wait"));
        }

        [Test] // L3R2
        public void RedAmberToGreenTransitionExecutesManagerCalls()
        {
            mockTime.Delay(3).Returns(true);
            mockPedestrian.SetWalk(false).Returns(true);
            mockPedestrian.SetWait(true).Returns(true);
            mockPedestrian.SetAudible(false).Returns(true);
            mockVehicle.SetAllGreen(true).Returns(true);

            controller = new TrafficController("test", "redamber", "walk", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.SetCurrentState("green", "wait");

            Assert.That(result, Is.True);
            mockVehicle.Received(1).SetAllGreen(true);
            mockPedestrian.Received(1).SetWait(true);
        }

        [Test] // L3R2
        public void RedAmberToGreenTransitionFailsWhenManagerCallFails()
        {
            mockTime.Delay(3).Returns(true);
            mockPedestrian.SetWalk(false).Returns(false);

            controller = new TrafficController("test", "redamber", "walk", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            Assert.That(controller.SetCurrentState("green", "wait"), Is.False);
        }

        [Test] // L3R3
        public void DetectFaultCallsBothWebServiceMethods()
        {
            mockVehicle.GetStatus().Returns("VehicleSignal,FAULT,");
            mockPedestrian.GetStatus().Returns("PedestrianSignal,OK,");
            mockTime.GetStatus().Returns("Timer,OK,");

            controller = new TrafficController("test", "green", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.DetectFaultAndGoOutOfService();

            Assert.That(result, Is.True);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("oosv"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("oosp"));

            mockWeb.Received(1).FaultDetected(true);
            mockWeb.Received(1).LogEngineerRequired("out of service");
        }

        [Test] // L3R3
        [TestCase("VehicleSignal,FAULT,")]
        [TestCase("PedestrianSignal,FAULT,")]
        [TestCase("Timer,FAULT,")]
        public void DetectFaultTransitionsToOutOfService(string faultyStatus)
        {
            mockVehicle.GetStatus().Returns(faultyStatus.StartsWith("Vehicle") ? faultyStatus : "VehicleSignal,OK,");
            mockPedestrian.GetStatus().Returns(faultyStatus.StartsWith("Pedestrian") ? faultyStatus : "PedestrianSignal,OK,");
            mockTime.GetStatus().Returns(faultyStatus.StartsWith("Timer") ? faultyStatus : "Timer,OK,");

            controller = new TrafficController("test", "green", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.DetectFaultAndGoOutOfService();

            Assert.That(result, Is.True);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("oosv"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("oosp"));
        }

        [Test] // L3R3
        public void NoFaultDoesNotTransitionToOutOfService()
        {
            mockVehicle.GetStatus().Returns("VehicleSignal,OK,");
            mockPedestrian.GetStatus().Returns("PedestrianSignal,OK,");
            mockTime.GetStatus().Returns("Timer,OK,");

            controller = new TrafficController("test", "green", "wait", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            bool result = controller.DetectFaultAndGoOutOfService();

            Assert.That(result, Is.False);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("green"));
        }

        [Test] // L3R4
        [TestCase("VehicleSignal,FAULT,", "VehicleSignal,")]
        [TestCase("PedestrianSignal,FAULT,", "PedestrianSignal,")]
        [TestCase("Timer,FAULT,", "Timer,")]
        public void StatusReportLogsEngineerForEachFaultType(string faultStatus, string expectedLog)
        {
            mockVehicle.GetStatus().Returns(faultStatus.StartsWith("Vehicle") ? faultStatus : "VehicleSignal,OK,");
            mockPedestrian.GetStatus().Returns(faultStatus.StartsWith("Pedestrian") ? faultStatus : "PedestrianSignal,OK,");
            mockTime.GetStatus().Returns(faultStatus.StartsWith("Timer") ? faultStatus : "Timer,OK,");

            controller = new TrafficController("test", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            controller.GetStatusReport();

            mockWeb.Received(1).LogEngineerRequired(expectedLog);
        }

        [Test] // L3R4
        public void StatusReportLogsMultipleFaults()
        {
            mockVehicle.GetStatus().Returns("VehicleSignal,FAULT,");
            mockPedestrian.GetStatus().Returns("PedestrianSignal,FAULT,");
            mockTime.GetStatus().Returns("Timer,OK,");

            controller = new TrafficController("test", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            controller.GetStatusReport();

            mockWeb.Received(1).LogEngineerRequired("VehicleSignal,PedestrianSignal,");
        }

        [Test] // L3R5
        public void EmailSentWhenWebServiceThrowsException()
        {
            mockVehicle.GetStatus().Returns("VehicleSignal,FAULT,");
            mockPedestrian.GetStatus().Returns("PedestrianSignal,OK,");
            mockTime.GetStatus().Returns("Timer,OK,");
            mockWeb.When(x => x.LogEngineerRequired(Arg.Any<string>())).Do(x => { throw new Exception("Connection error"); });

            controller = new TrafficController("test", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            controller.GetStatusReport();

            mockEmail.Received(1).SendMail("transportoffice@gmail.com", "failed to log out of service", "Connection error");
        }

        [Test] // L3R5
        public void EmailNotSentWhenNoFault()
        {
            mockVehicle.GetStatus().Returns("VehicleSignal,OK,");
            mockPedestrian.GetStatus().Returns("PedestrianSignal,OK,");
            mockTime.GetStatus().Returns("Timer,OK,");

            controller = new TrafficController("test", mockVehicle, mockPedestrian, mockTime, mockWeb, mockEmail);
            controller.GetStatusReport();

            mockEmail.DidNotReceive().SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        #endregion

        #region Edge Cases & State Recovery

        [Test] // L3R1
        public void SetCurrentStateStoresPreviousStateForRecovery()
        {
            controller = new TrafficController("test", "green", "wait");

            controller.SetCurrentState("oosv", "oosp");

            bool result = controller.SetCurrentState("green", "wait");

            Assert.That(result, Is.True);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("green"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("wait"));
        }

        [Test] // L3R1
        public void PreviousStateIsCorrectAfterMultipleTransitions()
        {
            controller = new TrafficController("test", "green", "wait");

            controller.SetCurrentState("amber", "wait");

            controller.SetCurrentState("oosv", "oosp");

            bool result = controller.SetCurrentState("amber", "wait");

            Assert.That(result, Is.True);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("amber"));
            Assert.That(controller.GetCurrentPedestrianSignalState(), Is.EqualTo("wait"));
        }

        [Test] // L3R1
        public void CannotTransitionFromOutOfServiceToWrongState()
        {
            controller = new TrafficController("test", "green", "wait");

            controller.SetCurrentState("oosv", "oosp");

            bool result = controller.SetCurrentState("amber", "wait");

            Assert.That(result, Is.False);
            Assert.That(controller.GetCurrentVehicleSignalState(), Is.EqualTo("oosv"));
        }

        [Test] // L2R1
        public void OutOfServiceTransitionFromAnyState()
        {
            controller = new TrafficController("test", "green", "wait");
            Assert.That(controller.SetCurrentState("oosv", "oosp"), Is.True);
        }

        [Test] // L1R1
        public void NullIntersectionIDBecomesEmptyString()
        {
            controller = new TrafficController(null);
            Assert.That(controller.GetIntersectionID(), Is.EqualTo(""));
        }

        [Test] // L2R1
        public void SequentialValidTransitions()
        {
            controller = new TrafficController("test", "green", "wait");
            Assert.That(controller.SetCurrentState("amber", "wait"), Is.True);
        }

        #endregion
    }
}