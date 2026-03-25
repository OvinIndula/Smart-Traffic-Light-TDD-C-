using System;

namespace SmartTrafficLight.Tests
{
    public class TrafficController
    {
        private string intersectionID;
        private string currentVehicleSignalState;
        private string currentPedestrianSignalState;
        private string previousVehicleSignalState;
        private string previousPedestrianSignalState;

        private IVehicleSignalManager vehicleSignalManager;
        private IPedestrianSignalManager pedestrianSignalManager;
        private ITimeManager timeManager;
        private IWebService webService;
        private IEmailService emailService;

        private static readonly string[] ValidVehicleStates = { "red", "redamber", "green", "amber", "oosv" };
        private static readonly string[] ValidPedestrianStates = { "wait", "walk", "oosp" };

        public TrafficController(string id)
        {
            intersectionID = id?.ToLower() ?? "";
            currentVehicleSignalState = "amber";
            currentPedestrianSignalState = "wait";
            previousVehicleSignalState = "amber";
            previousPedestrianSignalState = "wait";
        }

        public TrafficController(string id, string vehicleStartState, string pedestrianStartState)
        {
            intersectionID = id?.ToLower() ?? "";
            ValidateAndSetInitialStates(vehicleStartState, pedestrianStartState);
        }

        public TrafficController(
            string id,
            IVehicleSignalManager iVehicleSignalManager,
            IPedestrianSignalManager iPedestrianSignalManager,
            ITimeManager iTimeManager,
            IWebService iWebService,
            IEmailService iEmailService)
        {
            intersectionID = id?.ToLower() ?? "";
            currentVehicleSignalState = "amber";
            currentPedestrianSignalState = "wait";
            previousVehicleSignalState = "amber";
            previousPedestrianSignalState = "wait";

            InjectDependencies(iVehicleSignalManager, iPedestrianSignalManager, iTimeManager, iWebService, iEmailService);
        }

        public TrafficController(string id, string vehicleStartState, string pedestrianStartState,
            IVehicleSignalManager iVehicleSignalManager,
            IPedestrianSignalManager iPedestrianSignalManager,
            ITimeManager iTimeManager,
            IWebService iWebService,
            IEmailService iEmailService)
        {
            intersectionID = id?.ToLower() ?? "";
            ValidateAndSetInitialStates(vehicleStartState, pedestrianStartState);
            InjectDependencies(iVehicleSignalManager, iPedestrianSignalManager, iTimeManager, iWebService, iEmailService);
        }

        private void ValidateAndSetInitialStates(string vehicleState, string pedestrianState)
        {
            string vehicleLower = vehicleState?.ToLower() ?? "";
            string pedestrianLower = pedestrianState?.ToLower() ?? "";

            bool vehicleValid = vehicleLower == "red" || vehicleLower == "redamber" ||
                               vehicleLower == "green" || vehicleLower == "amber";
            bool pedestrianValid = pedestrianLower == "wait" || pedestrianLower == "walk";

            if (!vehicleValid || !pedestrianValid)
            {
                throw new ArgumentException(
                    "Argument Exception: TrafficController can only be initialised to the following states: 'green', 'amber', 'red', 'redamber' for the vehicle signals and 'wait' or 'walk' for the pedestrian signal");
            }

            currentVehicleSignalState = vehicleLower;
            currentPedestrianSignalState = pedestrianLower;
            previousVehicleSignalState = vehicleLower;
            previousPedestrianSignalState = pedestrianLower;
        }

        private void InjectDependencies(IVehicleSignalManager vehicle, IPedestrianSignalManager pedestrian,
                                        ITimeManager time, IWebService web, IEmailService email)
        {
            vehicleSignalManager = vehicle;
            pedestrianSignalManager = pedestrian;
            timeManager = time;
            webService = web;
            emailService = email;
        }

        #region Getters

        public string GetIntersectionID() => intersectionID;

        public string GetCurrentVehicleSignalState() => currentVehicleSignalState;

        public string GetCurrentPedestrianSignalState() => currentPedestrianSignalState;

        #endregion

        #region Setters

        public void SetIntersectionID(string id)
        {
            intersectionID = id?.ToLower() ?? "";
        }

        public bool SetStateDirect(string vehicleSignalState, string pedestrianSignalState)
        {
            string vehicleLower = vehicleSignalState?.ToLower() ?? "";
            string pedestrianLower = pedestrianSignalState?.ToLower() ?? "";

            if (!IsValidVehicleState(vehicleLower) || !IsValidPedestrianState(pedestrianLower))
                return false;

            currentVehicleSignalState = vehicleLower;
            currentPedestrianSignalState = pedestrianLower;
            return true;
        }

        public bool SetCurrentState(string vehicleSignal, string pedestrianSignal)
        {
            string vehicleLower = vehicleSignal?.ToLower() ?? "";
            string pedestrianLower = pedestrianSignal?.ToLower() ?? "";

            if (!IsValidState(vehicleLower, pedestrianLower))
                return false;

            if (!IsValidTransition(currentVehicleSignalState, currentPedestrianSignalState, vehicleLower, pedestrianLower))
                return false;

            previousVehicleSignalState = currentVehicleSignalState;
            previousPedestrianSignalState = currentPedestrianSignalState;

            if (currentVehicleSignalState == "amber" && vehicleLower == "red")
                if (!ExecuteAmberToRedTransition())
                    return false;

            if (currentVehicleSignalState == "redamber" && vehicleLower == "green")
                if (!ExecuteRedAmberToGreenTransition())
                    return false;

            currentVehicleSignalState = vehicleLower;
            currentPedestrianSignalState = pedestrianLower;
            return true;
        }

        private bool ExecuteAmberToRedTransition()
        {
            if (vehicleSignalManager == null || pedestrianSignalManager == null || timeManager == null)
                return true;

            if (!timeManager.Delay(3))
                return false;

            if (!vehicleSignalManager.SetAllRed())
                return false;

            if (!pedestrianSignalManager.SetWalk(true))
                return false;

            if (!pedestrianSignalManager.SetAudible(true))
                return false;

            return true;
        }

        private bool ExecuteRedAmberToGreenTransition()
        {
            if (vehicleSignalManager == null || pedestrianSignalManager == null || timeManager == null)
                return true;

            if (!timeManager.Delay(3))
                return false;

            if (!pedestrianSignalManager.SetWalk(false))
                return false;

            if (!pedestrianSignalManager.SetWait(true))
                return false;

            if (!pedestrianSignalManager.SetAudible(false))
                return false;

            if (!vehicleSignalManager.SetAllGreen(true))
                return false;

            return true;
        }

        public bool DetectFaultAndGoOutOfService()
        {
            string vehicleStatus = vehicleSignalManager?.GetStatus() ?? "";
            string pedestrianStatus = pedestrianSignalManager?.GetStatus() ?? "";
            string timerStatus = timeManager?.GetStatus() ?? "";

            bool hasFault = vehicleStatus.Contains("FAULT") ||
                           pedestrianStatus.Contains("FAULT") ||
                           timerStatus.Contains("FAULT");

            if (!hasFault)
                return false;

            previousVehicleSignalState = currentVehicleSignalState;
            previousPedestrianSignalState = currentPedestrianSignalState;

            currentVehicleSignalState = "oosv";
            currentPedestrianSignalState = "oosp";

            webService?.FaultDetected(true);
            webService?.LogEngineerRequired("out of service");

            return true;
        }

        #endregion

        #region Status Reporting

        public string GetStatusReport()
        {
            string vehicleStatus = vehicleSignalManager?.GetStatus() ?? "";
            string pedestrianStatus = pedestrianSignalManager?.GetStatus() ?? "";
            string timerStatus = timeManager?.GetStatus() ?? "";

            string combinedReport = vehicleStatus + pedestrianStatus + timerStatus;

            if (webService != null && !string.IsNullOrEmpty(combinedReport))
            {
                LogFaultsIfDetected(vehicleStatus, pedestrianStatus, timerStatus);
            }

            return combinedReport;
        }

        private void LogFaultsIfDetected(string vehicleStatus, string pedestrianStatus, string timerStatus)
        {
            string faultyDevices = BuildFaultString(vehicleStatus, pedestrianStatus, timerStatus);

            if (string.IsNullOrEmpty(faultyDevices))
                return;

            try
            {
                webService.LogEngineerRequired(faultyDevices);
            }
            catch (Exception ex)
            {
                emailService?.SendMail("transportoffice@gmail.com", "failed to log out of service", ex.Message);
            }
        }

        private string BuildFaultString(string vehicleStatus, string pedestrianStatus, string timerStatus)
        {
            string result = "";

            if (vehicleStatus.Contains("FAULT"))
                result += "VehicleSignal,";

            if (pedestrianStatus.Contains("FAULT"))
                result += "PedestrianSignal,";

            if (timerStatus.Contains("FAULT"))
                result += "Timer,";

            return result;
        }

        #endregion

        #region Validation

        private bool IsValidVehicleState(string state) =>
            Array.Exists(ValidVehicleStates, e => e == state);

        private bool IsValidPedestrianState(string state) =>
            Array.Exists(ValidPedestrianStates, e => e == state);

        private bool IsValidState(string vehicle, string pedestrian) =>
            IsValidVehicleState(vehicle) && IsValidPedestrianState(pedestrian);

        private bool IsValidTransition(string currVehicle, string currPed, string nextVehicle, string nextPed)
        {
            // Normal operation transitions
            if (IsNormalOperation(currVehicle) && IsNormalOperation(nextVehicle))
            {
                return (currVehicle == "green" && nextVehicle == "amber" && currPed == "wait" && nextPed == "wait") ||
                       (currVehicle == "amber" && nextVehicle == "red" && currPed == "wait" && nextPed == "walk") ||
                       (currVehicle == "red" && nextVehicle == "redamber" && currPed == "walk" && nextPed == "walk") ||
                       (currVehicle == "redamber" && nextVehicle == "green" && currPed == "walk" && nextPed == "wait");
            }

            // Transition to out-of-service from any normal state
            if (IsNormalOperation(currVehicle) && nextVehicle == "oosv" && nextPed == "oosp")
                return true;

            // FIXED: Recovery from out-of-service (history state) - must return to EXACT previous state
            if (currVehicle == "oosv" && currPed == "oosp")
            {
                return nextVehicle == previousVehicleSignalState && nextPed == previousPedestrianSignalState;
            }

            return false;
        }

        private bool IsNormalOperation(string state) =>
            state == "red" || state == "redamber" || state == "green" || state == "amber";

        #endregion
    }
}