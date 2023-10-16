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

        #endregion

        #region Connecting

        public string Username 
        { 
            get => _username;
        }

        public string Password 
        { 
            get => _password;
        }

        public Config Config 
        { 
            get => new Config(_config); 
        }

        public bool IsConnected
        {
            get => _client.IsLoggedIn;
        }

        public bool OpenConnection()
        {
            _logger?.Info("Open connection");

            var data = _client.Login(_username ?? string.Empty, _password ?? string.Empty);

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

        public void CloseConnection()
        {
            _client.Logout();
        }

        #endregion

        #region Retriving data

        /// <summary>
        /// Get a list of all TPS users
        /// </summary>
        /// <returns>List with all TPS users</returns>
        public List<User> GetTPSUsers()
        {
            _logger?.Info("Get TPS users");

            var data = Exchange(new UserQueryRequest());

            return data.IsError() ? null : (data as UserList).Users;
        }

        /// <summary>
        /// Get a list of patients with a special patient status
        /// </summary>
        /// <param name="patientStatus">Status of patients to select</param>
        /// <returns>List of patients with patient status</returns>
        public List<Patient> GetPatientsWithStatus(PatientStatus patientStatus = PatientStatus.Planning)
        {
            _logger?.Info($"Get patients with status '{patientStatus}'");

            var data = Exchange(new PatientQueryRequest { PatientToStatus = patientStatus });

            return data.IsError() ? null : (data as PatientList)?.Patients;
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
            foreach (var patient in list)
            {
                if (!result.Select((p) => p.MedicalId).ToList().Contains(patient.MedicalId))
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

            _logger?.Info($"Get plans for patient '{patient.MedicalId.Trim()}'");

            var data = Exchange(new PlanQueryRequest { Patient = patient });

            return data.IsError() ? null : (data as PlanList)?.Plans;
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
                throw new ArgumentNullException("patient");
            }

            _logger?.Info($"Get picture for patient '{patient.MedicalId.Trim()}'");

            var data = Exchange(new PatientBOQueryRequest { Patient = patient, PatientFileType = PatientFileType.PatientPhoto });

            if (data.IsError())
            {
                return null;
            }

            var boList = (BOList)data;

            if (boList is null || boList.BOs.Count == 0)
            {
                _logger?.Info($"Picture of patient '{patient.MedicalId.Trim()}' not found");

                return null;
            }

            using (var stream = new MemoryStream())
            {
                data = _client.Download(boList.BOs.Last(), stream);

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

            _logger?.Info($"Get information for DICOM series '{uuid}'");

            var data = Exchange(new DcmSeriesQueryRequest { Patient = patient });

            if (data.IsError())
            {
                return null;
            }

            var dcmSeriesList = (DicomSeriesList)data;

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

            _logger?.Info($"Orthanc Id '{orthancId}' to search");

            if (orthancId == string.Empty)
            {
                _logger?.Info($"DICOM series '{uuid}' not found");
                return null;
            }

            data = Exchange(new DicomSeriesListRequest { Patient = patient });

            if (data.IsError())
            {
                return null;
            }

            var dicomSeriesList = (DicomSeriesList)data;

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

            var planBOUuid = plan.PlanBOUuid;

            _logger?.Info($"Load PlanData with Uuid '{planBOUuid}' for plan '{plan.PlanName}'");

            var data = Exchange(new PlanBOQueryRequest { BoUuid = planBOUuid });

            if (data.IsError())
            {
                return null;
            }

            var result = Download<PlanData>(planBOUuid, data);

            return result;
        }

        public PlanSummary GetPlanSummaryForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var planSummaryBOUuid = plan.StatusDetailBoUuid;

            _logger?.Info($"Get PlanSummary with Uuid '{planSummaryBOUuid}' for plan '{plan.PlanName}'");

            var data = Exchange(new PlanBOQueryRequest { BoUuid = planSummaryBOUuid });

            if (data.IsError())
            {
                return null;
            }

            var result = Download<PlanSummary>(planSummaryBOUuid, data);

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

            foreach (var isocenter in beamData.IsocenterSet.Isocenters)
            {
                var nodes = 0;
                var startNode = 0;
                var distance = 0.0;

                if (isocenter != firstIsocenter)
                {
                    // Get TA distance
                    (distance, nodes, startNode) = CalcDistanceForTA(isocenter);

                    travelTADistanceInDegrees += distance;
                    numOfTANodes += numOfTANodes;
                }

                if (isocenter.DeliveryInstructions.Length == 0)
                {
                    // Could happen, if you don't look into the Plan Summary
                    continue;
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
            var autoAlignmentTime = 600.0;

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

        public VOIData GetVOIsForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var voiDataBOUuid = plan.VOIBOUuid;

            _logger?.Info($"Get VOIs with Uuid '{voiDataBOUuid}' for plan '{plan.PlanName}'");

            var data = Exchange(new PlanBOQueryRequest { BoUuid = voiDataBOUuid });

            if (data.IsError())
            {
                return null;
            }

            return Download<VOIData>(voiDataBOUuid, data);
        }

        public DoseVolumeData GetDVDataForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var planDVDataBOUuid = plan.DVDataBOUuid;

            _logger?.Info($"Get DVData with Uuid '{planDVDataBOUuid}' for plan '{plan.PlanName}'");

            var data = Exchange(new PlanBOQueryRequest { BoUuid = planDVDataBOUuid });

            if (data.IsError())
            {
                return null;
            }

            return Download<DoseVolumeData>(planDVDataBOUuid, data);
        }

        public BeamData GetBeamsForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var beamDataBOUuid = plan.BeamSetBOUuid;

            _logger?.Info($"Get BeamSet with Uuid '{beamDataBOUuid}' for plan '{plan.PlanName}'");

            var data = Exchange(new PlanBOQueryRequest { BoUuid = beamDataBOUuid });

            if (data.IsError())
            {
                return null;
            }

            return Download<BeamData>(beamDataBOUuid, data);
        }

        public SystemData GetSystemDataForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var systemDataBOUuid = plan.SystemConfigBOUuid;

            _logger?.Info($"Get SystemData with Uuid '{systemDataBOUuid}' for plan '{plan.PlanName}'");

            var data = Exchange(new PlanBOQueryRequest { BoUuid = systemDataBOUuid });

            if (data.IsError())
            {
                return null;
            }

            return Download<SystemData>(systemDataBOUuid, data);
        }

        public DoseVolumeGrid GetDoseVolumeGridForPlan(Plan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var doseVolumeBOUuid = plan.DoseVolumeBOUuid;

            _logger?.Info($"Get DoseVolumeGrid with Uuid '{doseVolumeBOUuid}' for plan '{plan.PlanName}'");

            var data = Exchange(new PlanBOQueryRequest { BoUuid = doseVolumeBOUuid });

            if (data.IsError())
            {
                return null;
            }

            var boList = (BOList)data;

            if (boList is null || boList.BOs.Count == 0)
            {
                _logger?.Info($"BoUuid '{doseVolumeBOUuid}' not found");
                return null;
            }

            using (var stream = new MemoryStream())
            {
                data = _client.Download(boList.BOs.First(), stream);

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

        public DeliveryData GetDeliveryDataForPlan(Plan plan, bool shortVersion = false)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            _logger?.Info($"Get delivery data for plan '{plan.PlanName}'");

            var data = Exchange(new DeliveredBeamSetQueryRequest { Plan = plan });

            if (data.IsError())
            {
                return null;
            }

            var result = new DeliveryData((DeliveryBeamSet)data);

            if (!shortVersion)
            {
                foreach (var fraction in result.Fractions)
                {
                    // Populate treatments
                    fraction.Treatments.AddRange(GetTreatmentsForFraction(fraction).OrderBy(f => f.StartTime));

                    // Populate kV images belonging to this fraction
                    fraction.KVImages.AddRange(GetRadiationDataForFraction(fraction).OrderBy(i => i.Timestamp));
                }
            }

            return result;
        }

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

        public void CalcDoseForIsocenters(BeamData beamData, SystemData systemData)
        {
            double ocr = 1; // Only use the center point

            foreach (var isocenter in beamData.IsocenterSet.Isocenters)
            {
                double dose = 0;

                var commisioningDataForCollimator = systemData.Commissioning.CommissioningDataMap.Where(cdm => cdm.CollimatorSize == isocenter.Collimator.Size).FirstOrDefault();

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

                    //var distanceSourceTarget = Math.Sqrt(Math.Pow(beam.CTSource[0] - beam.CTTarget[0], 2) + Math.Pow(beam.CTSource[1] - beam.CTTarget[1], 2) + Math.Pow(beam.CTSource[2] - beam.CTTarget[2], 2));
                    //var distanceDeviceTarget = Math.Sqrt(Math.Pow(beam.DeviceSource[0] - beam.CTTarget[0], 2) + Math.Pow(beam.DeviceSource[1] - beam.CTTarget[1], 2) + Math.Pow(beam.DeviceSource[2] - beam.CTTarget[2], 2));

                    var tpr = GetTPRValue(depths, tprs, beam.MaxEffDepth);

                    dose += beam.MU * of * ocr * tpr;
                }

                isocenter.TargetDose = dose;
            }
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

            _logger?.Info($"Get treatment data for fraction '{fraction.ID}'");

            var data = Exchange(new ReportQueryRequest { Fraction = fraction.ZapObject });

            if (data.IsError())
            {
                return null;
            }

            var result = new List<Treatment>();

            if (((ZList<TreatmentReportData>)data)?.Value == null || ((ZList<TreatmentReportData>)data)?.Value.Count == 0)
            {
                return result;
            }

            foreach (var treatmentReportData in ((ZList<TreatmentReportData>)data)?.Value)
            {
                // Save the used GlobalNodeSystem for this ZSystem for later use
                if (_globalNodeSet == null)
                {
                    _globalNodeSet = GetSystemGlobalNodeList(((ZList<TreatmentReportData>)data)?.Value[0].System);
                }

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

            _logger?.Info($"Get system global node list");

            var data = Exchange(new SystemGlobalNodesQueryRequest { System = system });

            return data.IsError() ? null : (GlobalNodeSet)data;
        }

        private KVImageOnPathData GetQueryKVImageOnNode(ZapSurgical.Data.Path planPath)
        {
            _logger?.Info($"Get kV image on node list for plan path '{planPath}'");

            var data = Exchange(new QueryKVImageOnNodeRequest { PlanPath = planPath });

            return data.IsError() ? null : (KVImageOnPathData)data;
        }

        private KVImageOnPathOffNodeData GetQueryKVImageOffNode(ZapSurgical.Data.Path planPath)
        {
            _logger?.Info($"Get kV image off node list for plan path {planPath}");

            var data = Exchange(new QueryKVImageOffNodeRequest { PlanPath = planPath });

            return data.IsError() ? null : (KVImageOnPathOffNodeData)data;
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
