using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZapClient.Data;
using ZapClient.Helpers;
using ZapSurgical;
using ZapSurgical.Data;

namespace ZapClient
{
    /// <summary>
    /// ZapClient provides methods to connect to the Zap Surgical system, retrieve and manage patient, plan, and treatment data,
    /// and perform calculations related to dose and treatment times. It handles authentication, data exchange, and file downloads.
    /// </summary>
    public class ZapClient
    {
        DateTime _lastConnection;
        readonly Config _config;
        readonly ZClient _client;
        readonly Logger _logger;
        string _username = string.Empty;
        string _password = string.Empty;
        Func<string, string, (string, string)> _getUsernameAndPassword;
        GlobalNodeSet _globalNodeSet;

        #region Constructor

        public ZapClient(Func<string, string, (string, string)> getUsernameAndPassword, object logger = null)
        {
            _logger = (Logger)logger;
            _getUsernameAndPassword = getUsernameAndPassword;

            _config = LoadConfigData();
            _client = new ZClient(_config.Server, _config.Port);

            _username = _config.Username;

            if (_config.Encrypted)
            {
                _password = new System.Net.NetworkCredential(string.Empty, _config.Password).Password;
            }
            else
            {
                _password = _config.Password;
            }
        }

        public string Username => _username;

        public string Password => _password;

        public Config Config => new Config(_config);

        public bool IsConnected => _client.IsLoggedIn;

        /// <summary>
        /// Opens a connection to the Zap Surgical system using the configured username and password.
        /// If login fails, prompts for new credentials until successful or cancelled. Cancel by 
        /// returning empty strings.
        /// </summary>
        public bool OpenConnection()
        {
            _logger?.Info("Open connection");

            var data = _client.Login(_username, _password);

            while (!_client.IsLoggedIn)
            {
                (_username, _password) = _getUsernameAndPassword(_username, _password);

                if (_username.Equals(string.Empty) && _password.Equals(string.Empty))
                {
                    return false;
                }

                _logger?.Info($"Login {_username}");

                data = _client.Login(_username, _password);

                if (data.IsError())
                {
                    _logger?.Error($"{data.ZErrorMessage()}");
                }
            }

            _lastConnection = DateTime.Now;

            return true;
        }

        /// <summary>
        /// Closes the connection to the Zap Surgical system if currently logged in.
        /// Logs the action using the configured logger.
        /// </summary>
        public void CloseConnection()
        {
            if (_client.IsLoggedIn)
            {
                _client.Logout();

                _logger?.Info("Closed connection");
            }
        }

        #endregion

        #region Retriving data

        /// <summary>
        /// Get a list of all TPS users
        /// </summary>
        public List<User> GetTPSUsers()
        {
            var userList = SafeExchange<UserList>(new UserQueryRequest(), "Get TPS users");
            return userList?.Users ?? new List<User>();
        }

        /// <summary>
        /// Get a list of patients with a special patient status
        /// </summary>
        /// <param name="patientStatus">Status of patients to select</param>
        /// <returns>List of patients with patient status</returns>
        public List<Patient> GetPatientsWithStatus(PatientStatus patientStatus = PatientStatus.Planning)
        {
            var patientList = SafeExchange<PatientList>(new PatientQueryRequest { PatientToStatus = patientStatus }, $"Get patients with status '{patientStatus}'");
            return patientList?.Patients ?? new List<Patient>();
        }

        /// <summary>
        /// Get a list of all patients
        /// </summary>
        /// <returns>List of all patients</returns>
        public List<Patient> GetAllPatients()
        {
            List<Patient> result = new List<Patient>();

            AddPatientsToList(ref result, GetPatientsWithStatus(PatientStatus.New));
            AddPatientsToList(ref result, GetPatientsWithStatus(PatientStatus.Planning));
            AddPatientsToList(ref result, GetPatientsWithStatus(PatientStatus.Treating));
            AddPatientsToList(ref result, GetPatientsWithStatus(PatientStatus.Treated));
            AddPatientsToList(ref result, GetPatientsWithStatus(PatientStatus.Archived));

            return result;
        }

        private void AddPatientsToList(ref List<Patient> result, List<Patient> list)
        {
            var existingIds = new HashSet<string>(result.Select(p => p.MedicalId));
            foreach (var patient in list)
            {
                if (existingIds.Add(patient.MedicalId))
                    result.Add(patient);
            }
        }

        /// <summary>
        /// Get all plans for a patient
        /// </summary>
        /// <param name="patient">Patient for lookup of plans</param>
        /// <returns>List of plans</returns>
        /// <exception cref="ArgumentNullException">Thrown, when no patient is given</exception>
        public List<Plan> GetPlansForPatient(Patient patient)
        {
            if (patient == null)
            {
                throw new ArgumentNullException(nameof(patient));
            }

            var planList = SafeExchange<PlanList>(new PlanQueryRequest { Patient = patient }, $"Get plans for patient '{patient.MedicalId.Trim()}'");
            return planList?.Plans ?? new List<Plan>();
        }

        /// <summary>
        /// Get data for patient picture
        /// </summary>
        /// <param name="patient">Patient for lookup picture</param>
        /// <returns>Bytes for the stored patient picture</returns>
        /// <exception cref="ArgumentNullException">Thrown, when no patient is given</exception>
        public byte[] GetPictureForPatient(Patient patient)
        {
            if (patient == null)
            {
                throw new ArgumentNullException(nameof(patient));
            }

            var boList = SafeExchange<BOList>(new PatientBOQueryRequest { Patient = patient, PatientFileType = PatientFileType.PatientPhoto }, $"Get picture for patient '{patient.MedicalId.Trim()}'");

            if (boList is null || boList.BOs.Count == 0)
            {
                _logger?.Info($"Picture of patient '{patient.MedicalId.Trim()}' not found");

                return null;
            }

            using (var stream = new MemoryStream())
            {
                var data = _client.Download(boList.BOs.Last(), stream);

                if (data.IsError())
                {
                    _logger?.Error($"{data.ZErrorMessage()}");
                    return null;
                }

                if (stream.Length == 0)
                {
                    return null;
                }

                stream.Position = 0;

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Get a DICOM series for a given patient and UUID
        /// </summary>
        /// <param name="patient">Patient for lookup</param>
        /// <param name="uuid">UUID of series</param>
        /// <returns>DICOM series</returns>
        /// <exception cref="ArgumentNullException">Thrown, when no patient is given</exception>
        public DicomSeries GetDicomSeriesForUuid(Patient patient, string uuid)
        {
            if (patient == null)
            {
                throw new ArgumentNullException(nameof(patient));
            }

            var dcmSeriesList = SafeExchange<DicomSeriesList>(new DcmSeriesQueryRequest { Patient = patient }, $"Get information for DICOM series '{uuid}'");

            if (dcmSeriesList is null || dcmSeriesList.List.Count == 0)
            {
                _logger?.Info($"DICOM series for patient '{patient.MedicalId.Trim()}' not found");
                return null;
            }

            var orthancId = string.Empty;

            foreach (var dcmSeries in dcmSeriesList.List)
            {
                if (dcmSeries.Uuid == uuid)
                    orthancId = dcmSeries.OrthancID;
            }

            if (orthancId == string.Empty)
            {
                _logger?.Info($"DICOM series '{uuid}' not found");
                return null;
            }

            var dicomSeriesList = SafeExchange<DicomSeriesList>(new DicomSeriesListRequest { Patient = patient }, $"Orthanc Id '{orthancId}' to search");

            foreach (var dicomSeries in dicomSeriesList.List)
            {
                if (dicomSeries.OrthancID == orthancId)
                {
                    _logger?.Info($"{dicomSeries.OrthancID}, {dicomSeries.Manufacturer}");
                    return dicomSeries;
                }
            }

            return null;
        }

        /// <summary>
        /// Get plan data for a given plan
        /// </summary>
        /// <param name="plan">Plan for lookup plan data</param>
        /// <returns>PlanData</returns>
        /// <exception cref="ArgumentNullException">Thrown, when no patient is given</exception>
        public PlanData GetPlanDataForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return SafeDownload<PlanData>(plan.PlanName, plan.PlanBOUuid);
        }

        /// <summary>
        /// Calculates and returns a summary of the plan, including total MUs, treatment time, and other metrics.
        /// </summary>
        /// <param name="plan">The plan for which to retrieve the summary.</param>
        /// <returns>A PlanSummary object containing calculated metrics for the plan.</returns>
        public PlanSummary GetPlanSummaryForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var result = SafeDownload<PlanSummary>(plan.PlanName, plan.StatusDetailBoUuid);

            // This information isn't always provided, so the TotalMUs could be 0.
            var beamData = GetBeamsForPlan(plan);

            foreach (var isocenter in beamData.IsocenterSet.Isocenters)
            {
                // Count them, because Isocenters.TotalIsocenters is 0
                // result.TotalIsocenters++;

                foreach (var beam in isocenter.IsocenterBeamSet.Beams)
                {
                    result.TotalMUs += beam.MU;
                    // result.TotalBeamsWithMU += beam.MU != 0 ? 1 : 0;
                }
            }

            if (beamData.IsocenterSet.Isocenters.Length == 0)
            {
                return result;
            }

            var firstIsocenter = beamData.IsocenterSet.Isocenters[0];
            var travelDistanceInDegrees = 0.0;
            var travelTADistanceInDegrees = 0.0;
            var numOfNodes = 0;
            var numOfTANodes = 0;
            var numOfExtraAA = 0;

            foreach (var isocenter in beamData.IsocenterSet.Isocenters)
            {
                var nodes = 0;
                var startNode = 0;
                var distance = 0.0;

                if (isocenter.DeliveryInstructions == null || isocenter.DeliveryInstructions.Length == 0)
                {
                    // Could happen, if you don't look into the Plan Summary
                    continue;
                }
                
                if (isocenter != firstIsocenter)
                {
                    // Get TA distance
                    (distance, nodes, startNode) = CalcDistanceForTA(isocenter);

                    if (distance == 0 && nodes == 0)
                    {
                        // There are not enough beams to make a TA, so add an extra AA
                        numOfExtraAA++;
                    }
                    else
                    {
                        travelTADistanceInDegrees += distance;
                        numOfTANodes += numOfTANodes;
                    }
                }

                numOfNodes += isocenter.DeliveryInstructions.Length - startNode - 1; // One less, because the first is take into account for TA

                // Always round to third decimal, because Zap does the same
                var lastAxial = Math.Round(isocenter.DeliveryInstructions[startNode].AxialNodePosition, 3);
                var lastOblique = Math.Round(isocenter.DeliveryInstructions[startNode].ObliqueNodePosition, 3);
                var lastNodeDirection = isocenter.DeliveryInstructions[startNode].Direction;

                // Calc treatment time for this isocenter
                for (var i = startNode + 1; i < isocenter.DeliveryInstructions.Length; i++)
                {
                    var node = isocenter.DeliveryInstructions[i];

                    // Always round to third decimal, because Zap does the same
                    var axial = Math.Round(node.AxialNodePosition, 3);
                    var oblique = Math.Round(node.ObliqueNodePosition, 3);

                    distance = CalcDistanceBetweenNodes(axial, lastAxial, oblique, lastOblique, lastNodeDirection);

                    travelDistanceInDegrees += distance;

                    if (isocenter != firstIsocenter)
                    { 

                    }

                    lastAxial = axial;
                    lastOblique = oblique;
                    lastNodeDirection = node.Direction;
                }
            }

            var doseDeliveryTime = result.TotalMUs / result.TotalFractions / 25.0;
            var gantryMotionTime = travelDistanceInDegrees * 1.0 / 6.0 + 2.0 * numOfNodes;
            var imagingTrackingTime = Math.Max(1, (doseDeliveryTime + gantryMotionTime) / 45.0) * 2.0;
            var pressHVTime = result.TotalIsocenters * 2.0;
            var transitionalAlignmentTime = travelTADistanceInDegrees * 1.0 / 6.0 + 2.0 * numOfTANodes + (2.0 + 10.0 + 2.0) * (result.TotalIsocenters - 1); // Travel time + 2 s for each node + 2 s for pressing the flash + 10 s for TA image check + 2 s for pressing the flash
            var autoAlignmentTime = 600.0 + numOfExtraAA * 180.0;

            result.TotalTreatmentTime = doseDeliveryTime + gantryMotionTime + imagingTrackingTime + pressHVTime + transitionalAlignmentTime + autoAlignmentTime;

            return result;
        }

        private (double, int, int) CalcDistanceForTA(Isocenter isocenter)
        {
            var startNode = 0;

            // Move forward until we get the right Check for TA
            while (startNode < isocenter.DeliveryInstructions.Length && isocenter.DeliveryInstructions[startNode].ViaNode)
            {
                startNode++;
            }

            // TA forward
            var distance = 0.0;

            // Always round to third decimal, because Zap does the same
            var lastAxial = Math.Round(isocenter.DeliveryInstructions[0].AxialNodePosition, 3);
            var lastOblique = Math.Round(isocenter.DeliveryInstructions[0].ObliqueNodePosition, 3);
            var lastNodeDirection = isocenter.DeliveryInstructions[0].Direction;

            var firstAxial = lastAxial;
            var numOfTANodes = 0;
            var i = 0;

            do
            {
                i++;
                numOfTANodes++;

                // Are there any nodes left
                if (isocenter.DeliveryInstructions.Count() <= i)
                {
                    // No more nodes left, so it isn't possible to make a TA
                    // Could happen, when there are to less beams for an isocenter

                    return (0, 0, startNode);
                }

                var node = isocenter.DeliveryInstructions[i];

                // Always round to third decimal, because Zap does the same
                var axial = Math.Round(node.AxialNodePosition, 3);
                var oblique = Math.Round(node.ObliqueNodePosition, 3);

                var dist = CalcDistanceBetweenNodes(axial, lastAxial, oblique, lastOblique, lastNodeDirection);

                distance += dist;

                if (i-1 >= startNode)
                {
                    // Add this for the backwards direction
                    numOfTANodes++;
                    distance += dist;
                }

                lastAxial = axial;
                lastOblique = oblique;
                lastNodeDirection = node.Direction;
            } while (Math.Abs(Math.Round(isocenter.DeliveryInstructions[i].AxialNodePosition, 3) - firstAxial) <= 30.0 * Math.PI / 180.0);

            return (distance, numOfTANodes, startNode);
        }

        private double CalcDistanceBetweenNodes(double axial, double lastAxial, double oblique, double lastOblique, int direction)
        {
            var axialDistance = Math.Round(axial - lastAxial, 3);
            var obliqueDistance = Math.Round(oblique - lastOblique, 3);

            switch (direction)
            {
                case 1:
                    axialDistance = Math.Round(lastAxial - axial, 3);
                    break;
                case 2:
                    obliqueDistance = Math.Round(lastOblique - oblique, 3);
                    break;
                case 3:
                    axialDistance = Math.Round(lastAxial - axial, 3);
                    obliqueDistance = Math.Round(lastOblique - oblique, 3);
                    break;
                case 4:
                    axialDistance = axialDistance > Math.PI ? Math.Abs(2 * Math.PI - axialDistance) % 360.0 : axialDistance;
                    obliqueDistance = obliqueDistance > Math.PI ? Math.Abs(2 * Math.PI - obliqueDistance) % 360.0 : obliqueDistance;
                    break;
            }

            axialDistance += axialDistance < 0 ? 2.0 * Math.PI : 0.0;
            obliqueDistance += obliqueDistance < 0 ? 2.0 * Math.PI : 0.0;

            return Math.Round(Math.Abs(Math.Max(axialDistance, obliqueDistance) / Math.PI * 180) % 360);
        }

        /// <summary>
        /// Retrieves VOI (Volume of Interest) data for the specified plan.
        /// </summary>
        /// <param name="plan">The plan for which to retrieve VOI data.</param>
        /// <returns>VOIData object containing VOI information for the plan.</returns>
        public VOIData GetVOIsForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return SafeDownload<VOIData>(plan.PlanName, plan.VOIBOUuid);
        }

        /// <summary>
        /// Retrieves the Dose Volume Data for the specified plan.
        /// </summary>
        /// <param name="plan">The plan for which to retrieve Dose Volume Data.</param>
        /// <returns>DoseVolumeData object containing dose volume information for the plan.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the plan is null.</exception>
        public DoseVolumeData GetDVDataForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return SafeDownload<DoseVolumeData>(plan.PlanName, plan.DVDataBOUuid);
        }

        /// <summary>
        /// Retrieves the BeamData for the specified plan.
        /// </summary>
        /// <param name="plan">The plan for which to retrieve beam data.</param>
        /// <returns>BeamData object containing beam information for the plan.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the plan is null.</exception>
        public BeamData GetBeamsForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return SafeDownload<BeamData>(plan.PlanName, plan.BeamSetBOUuid);
        }

        /// <summary>
        /// Retrieves the SystemData for the specified plan.
        /// </summary>
        /// <param name="plan">The plan for which to retrieve system configuration data.</param>
        /// <returns>SystemData object containing system configuration information for the plan.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the plan is null.</exception>
        public SystemData GetSystemDataForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return SafeDownload<SystemData>(plan.PlanName, plan.SystemConfigBOUuid);
        }

        /// <summary>
        /// Retrieves the DoseVolumeGrid for the specified plan.
        /// Downloads the grid data using the plan's DoseVolumeBOUuid and parses it.
        /// Returns null if the grid cannot be found or an error occurs during download or parsing.
        /// </summary>
        public DoseVolumeGrid GetDoseVolumeGridForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var doseVolumeBOUuid = plan.DoseVolumeBOUuid;

            var boList = SafeExchange<BOList>(new PlanBOQueryRequest { BoUuid = doseVolumeBOUuid }, $"Get DoseVolumeGrid with Uuid '{doseVolumeBOUuid}' for plan '{plan.PlanName}'");

            if (boList is null || boList.BOs.Count == 0)
            {
                _logger?.Info($"BoUuid '{doseVolumeBOUuid}' not found");
                return null;
            }

            using (var stream = new MemoryStream())
            {
                var data = _client.Download(boList.BOs.First(), stream);

                if (data.IsError())
                {
                    _logger?.Error($"{data.ZErrorMessage()}");
                }

                stream.Position = 0;

                // We have a dose volume grid, so read it
                try
                {
                    return DoseVolumeGrid.ParseDoseVolumeGrid(stream);
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, $"Reading dose volume grid from file '{boList.BOs.First().Uuid}'");
                    throw new Exception($"Reading dose volume grid from file '{boList.BOs.First().Uuid}'", ex);
                }
            }
        }

        /// <summary>
        /// Retrieves delivery data for the specified plan.
        /// If shortVersion is false, populates each fraction with its treatments and kV images.
        /// </summary>
        public DeliveryData GetDeliveryDataForPlan(Plan plan, bool shortVersion = false)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var deliveryBeamSet = SafeExchange<DeliveryBeamSet>(new DeliveredBeamSetQueryRequest { Plan = plan }, $"Get delivery data for plan '{plan.PlanName}'");
            var deliveryData = new DeliveryData(deliveryBeamSet);

            if (!shortVersion)
            {
                foreach (var fraction in deliveryData.Fractions)
                {
                    // Populate treatments
                    fraction.Treatments.AddRange(GetTreatmentsForFraction(fraction).OrderBy(f => f.StartTime));

                    // Populate kV images belonging to this fraction
                    fraction.KVImages.AddRange(GetRadiationDataForFraction(fraction).OrderBy(i => i.Timestamp));
                }
            }

            return deliveryData;
        }

        /// <summary>
        /// Calculates the total dose for a list of kV images on nodes by summing the dose for each image using its KV, MA, and MS values.
        /// </summary>
        /// <param name="nodeData">The node data to use.</param>
        /// <returns>Total dose</returns>
        public double CalcDoseForkVImages(List<KVImageOnNodeData> nodeData)
        {
            double totalDose = 0.0;

            foreach (var nd in nodeData)
            {
                foreach (var image in nd.KVImages)
                {
                    totalDose += CalcDoseForValues(image.KV, image.MA, image.MS);
                }
            }

            return totalDose;
        }

        /// <summary>
        /// Calculates the total dose for a list of kV images off nodes by summing the dose for each image using its KV, MA, and MS values.
        /// </summary>
        /// <param name="nodeData">The node data to use.</param>
        /// <returns>Total dose</returns>
        public double CalcDoseForkVImages(List<KVImageOffNodeData> nodeData)
        {
            double totalDose = 0.0;

            foreach (var nd in nodeData)
            {
                foreach (var image in nd.KVImages)
                {
                    totalDose += CalcDoseForValues(image.KV, image.MA, image.MS);
                }
            }

            return totalDose;
        }

        /// <summary>
        /// Calculates the dose for each isocenter in the provided BeamData using the system's commissioning data.
        /// For each isocenter, finds the matching commissioning data for the collimator size, then calculates the dose
        /// for each beam using the output factor (OF), tissue phantom ratio (TPR), and monitor units (MU).
        /// The calculated dose is assigned to the isocenter's TargetDose property.
        /// </summary>
        public void CalcDoseForIsocenters(BeamData beamData, SystemData systemData)
        {
            double ocr = 1; // Only use the center point

            foreach (var isocenter in beamData.IsocenterSet.Isocenters)
            {
                double dose = 0;

                var commisioningDataForCollimator = systemData.Commissioning.CommissioningDataMap
                    .Where(cdm => cdm.CollimatorSize == isocenter.Collimator.Size)
                    .FirstOrDefault();

                if (commisioningDataForCollimator == null)
                {
                    continue;
                }

                var of = commisioningDataForCollimator.CommissioningTables.OFValue;
                var depths = commisioningDataForCollimator.CommissioningTables.TPRTable.DepthArray;
                var tprs = commisioningDataForCollimator.CommissioningTables.TPRTable.TPRValueArray;

                foreach (var beam in isocenter.IsocenterBeamSet.Beams)
                {
                    if (beam.MU == 0)
                    {
                        continue;
                    }

                    var tpr = GetTPRValue(depths, tprs, beam.MaxEffDepth);

                    dose += beam.MU * of * ocr * tpr;
                }

                isocenter.TargetDose = dose;
            }
        }

        #endregion

        #region Helper functions

        private T Parse<T>(Stream stream)
        {
            T result;

            // Deserialize JSON directly from stream
            using (StreamReader file = new StreamReader(stream))
            {
                JsonSerializer serializer = new JsonSerializer();
                result = (T)serializer.Deserialize(file, typeof(T));
            }

            return result;
        }

        private ZData Exchange(ZRequest request)
        {
            // Check, if we still logged in
            if ((DateTime.Now - _lastConnection).TotalMinutes > 10.0)
            {
                CloseConnection();

                var result = OpenConnection();

                if (!result)
                {
                    return null;
                }
            }

            var data = _client.Exchange(request);

            if (data.IsError())
            {
                _logger?.Error($"{data.ZErrorMessage()}");
            }

            return data;
        }

        private T Download<T>(string uuid, ZData data)
        {
            var boList = (BOList)data;

            if (boList is null || boList.BOs.Count == 0)
            {
                _logger?.Info($"BoUuid '{uuid}' not found");
                return default(T);
            }

            using (var stream = new MemoryStream())
            {
                data = _client.Download(boList.BOs.First(), stream);

                if (data.IsError())
                {
                    _logger?.Error($"{data.ZErrorMessage()}");
                    return default(T);
                }

                stream.Position = 0;

                // We have a file of type T, so read it
                return Parse<T>(stream);
            }
        }

        private T SafeExchange<T>(ZRequest request, string logMessage) where T : class
        {
            _logger?.Info(logMessage);

            var data = Exchange(request);

            return data.IsError() ? null : data as T;
        }

        private T SafeDownload<T>(string name, string boUuid) where T : class
        {

            var data = SafeExchange<ZData>(new PlanBOQueryRequest { BoUuid = boUuid }, $"Load {typeof(T).Name} with Uuid '{boUuid}' for plan '{name}'");

            return Download<T>(boUuid, data);
        }

        /// <summary>
        /// Each fraction could have zero, one or more treatments
        /// This treatments contain start and end time for this treatment 
        /// and durations for different things (table, beam on and so on).
        /// </summary>
        /// <param name="fraction">Fraction to use</param>
        /// <returns>List off all Treatments</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private List<Treatment> GetTreatmentsForFraction(Data.Fraction fraction)
        {
            if (fraction == null)
            {
                throw new ArgumentNullException(nameof(fraction));
            }

            var treatmentReportList = SafeExchange<ZList<TreatmentReportData>>(new ReportQueryRequest { Fraction = fraction.ZapObject }, $"Get treatment data for fraction '{fraction.ID}'");

            var result = new List<Treatment>();

            if (treatmentReportList?.Value == null || treatmentReportList?.Value.Count == 0)
            {
                return result;
            }

            // Save the used GlobalNodeSystem for this ZSystem for later use
            if (_globalNodeSet == null)
            {
                _globalNodeSet = GetSystemGlobalNodeList(treatmentReportList?.Value[0].System);
            }

            foreach (var treatmentReportData in treatmentReportList?.Value)
            {
                var treatment = new Treatment(treatmentReportData, fraction);

                // Get beams, that belong to this treatment
                // That are all beams between Treatment.StartTime and Treatment.EndTime
                foreach (var path in fraction.PathSet)
                {
                    var validBeams = path.Beams.Where(b => treatment.StartTime <= b.TreatmentTime && treatment.EndTime >= b.TreatmentTime);

                    if (validBeams == null || validBeams.Count() == 0)
                    {
                        continue;
                    }

                    var treatmentPath = new Data.Path(path);

                    treatmentPath.Beams.Clear();

                    foreach (var beam in validBeams)
                    {
                        treatmentPath.Beams.Add(new Data.Beam(beam));
                    }

                    treatment.Paths.Add(treatmentPath);
                }

                result.Add(treatment);
            }

            return result;
        }

        private List<KVImage> GetRadiationDataForFraction(Data.Fraction fraction)
        {
            if (fraction == null)
            {
                throw new ArgumentNullException(nameof(fraction));
            }

            _logger?.Info($"Get radiation data for fraction {fraction.ID}");

            List<KVImage> result = new List<KVImage>();

            foreach (var treatment in fraction.Treatments)
            {
                var data = Exchange(new RadiationTimeReportQueryRequest { StartTime = treatment.StartTime, EndTime = treatment.EndTime });

                if (data.IsError())
                {
                    return null;
                }

                var rtr = ((RadiationTimeRecordList)data).RediationTimeRecords;

                foreach (var image in rtr.Where(i => i.Type.ToUpper().Equals("KV")))
                {
                    result.Add(new KVImage(image));
                }
            }

            // Get kV images for this fraction
            foreach (var path in fraction.PathSet)
            {
                if (path is null)
                {
                    continue;
                }

                var kvImagesOnNode = GetQueryKVImageOnNode(path);

                FillNodesWithAngles(kvImagesOnNode.KVImagesOnNodes, _globalNodeSet);

                foreach (var node in kvImagesOnNode.KVImagesOnNodes)
                {
                    foreach (var image in node.KVImages)
                    {
                        var resultImage = result.Where(i => i.Timestamp == image.UploadTime).FirstOrDefault();

                        if (resultImage == null)
                        {
                            continue;
                        }

                        resultImage.PathUUID = path.PathUUID.ToUpper();
                        resultImage.Node = node.Node;
                        resultImage.RawImage = image.RawImage;
                        resultImage.CorrectedImage = image.CorrectedImage;
                    }
                }

                var kvImagesOffNode = GetQueryKVImageOffNode(path);

                foreach (var node in kvImagesOffNode.KVImagesOffNodes)
                {
                    foreach (var image in node.KVImages)
                    {
                        var resultImage = result.Where(i => i.Timestamp == image.UploadTime).FirstOrDefault();

                        if (resultImage == null)
                        {
                            continue;
                        }

                        resultImage.PathUUID = path.PathUUID.ToUpper();
                        resultImage.RawImage = image.RawImage;
                        resultImage.CorrectedImage = image.CorrectedImage;
                    }
                }
            }


            // Get radiation data for this treatment
            /*            var radiationData = GetRadiationDataForTreatment(treatment);

                        fraction.kVImages.AddRange(radiationData.Where(i => i.Type.Equals("KV")).OrderBy(i => i.Timestamp).Reverse());

                        foreach (var path in treatment.Paths)
                        {
                        }
            */

            return result;
        }

        private GlobalNodeSet GetSystemGlobalNodeList(ZSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            return SafeExchange<GlobalNodeSet>(new SystemGlobalNodesQueryRequest { System = system }, $"Get system global node list");
        }

        private KVImageOnPathData GetQueryKVImageOnNode(ZapSurgical.Data.Path planPath)
        {
            return SafeExchange<KVImageOnPathData>(new QueryKVImageOnNodeRequest { PlanPath = planPath }, $"Get kV image on node list for plan path '{planPath}'");
        }

        private KVImageOnPathOffNodeData GetQueryKVImageOffNode(ZapSurgical.Data.Path planPath)
        {
            return SafeExchange<KVImageOnPathOffNodeData>(new QueryKVImageOffNodeRequest { PlanPath = planPath }, $"Get kV image off node list for plan path {planPath}");
        }

        private void FillNodesWithAngles(List<KVImageOnNodeData> nodeData, GlobalNodeSet globalNodeSet)
        {
            var factor = 180.0 / Math.PI;

            foreach (var nd in nodeData)
            {
                var node = globalNodeSet.Nodes.Where(n => n.Uuid == nd.Node.Uuid).FirstOrDefault();

                nd.Node.NodeID = node.NodeID;
                nd.Node.Axial = node.Axial * factor;
                nd.Node.Oblique = node.Oblique * factor;
            }
        }

        private double CalcDoseForValues(short kv, short ma, short ms)
        {
            var dose = 1.15 * 11.02 * Math.Pow(kv / 95.0, 3) * (ma / 25.0) * (ms / 20.0);

            return dose;
        }

        private double GetTPRValue(double[] depths, double[] tprs, double depth)
        {
            var pos = (int)(depth * 2) - 1;

            while (pos < depths.Length - 1)
            {
                if (depths[pos] <= depth && depth < depths[pos + 1])
                {
                    var factor = (depths[pos + 1] - depth) / (depths[pos + 1] - depths[pos]);

                    return tprs[pos + 1] + factor * (tprs[pos] - tprs[pos + 1]);
                }

                pos++;
            }

            return 0;
        }

        private Config LoadConfigData(string filename = "")
        {
            // Check first for filename with hostname, then with network adress or, at the end, use this without
            if (string.IsNullOrEmpty(filename))
            {
                filename = "ZapClient." + Network.GetHostName().ToUpper() + ".cfg";

                if (!File.Exists(filename))
                {
                    filename = "ZapClient." + Network.GetIPAdress() + ".cfg";
                }

                if (!File.Exists(filename))
                {
                    filename = "ZapClient.cfg";
                }

                if (!File.Exists(filename))
                {
                    var config = new Config
                    {
                        Server = "10.0.0.105",
                        Port = 8088,
                        Username = string.Empty,
                        Password = string.Empty
                    };

                    // Didn't find one, so create a default one
                    using (StreamWriter file = File.CreateText(filename))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(file, config, typeof(Config));
                    }
                }
            }

            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }

            using (StreamReader file = File.OpenText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (Config)serializer.Deserialize(file, typeof(Config));
            }
        }

        #endregion
    }
}
