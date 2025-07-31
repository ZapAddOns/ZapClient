using System;
using System.Collections.Generic;
using System.Reflection;
using ZapSurgical.Data;

namespace ZapClient.Data
{
    public class Beam
    {
        // From ZapSurgical.Data.Node
        public string Uuid;
        public List<double> CTTarget;
        public List<double> CTSource;
        public List<double> DeviceSource;
        public int NodeID;
        public double AxialAngle;
        public double ObliqueAngle;
        // From ZapSurgical.Data.Beam
        public int ID;
        public List<double> Up;
        public string CollID;
        public double MU;
        public double DeliveredMU;
        public double PlanMU;
        public double RefDose;
        public double MaxEffDepth;
        public double EstimatedUnitDoseAtMVDetector;
        public double EstimatedUnitDoseAtMVDetector2;
        public double DeliveryTime;
        public DateTime TreatmentTime;
        public double CorrectedAxial;
        public double CorrectedOblique;
        public RotationCorrectionStatus RotationCorrectionStatus;
        // From BO data
        public double UnOptimzedMU;
        public int DeliveryIndex;
        public Collimator Collimator;
        public RadiationTimeRecord MVImage;

        public Beam(ZapSurgical.Data.Beam beam) 
        { 
            if (beam == null)
            {
                return;
            }

            Uuid = beam.Uuid;
            CTTarget = new List<double>(beam.CTTarget);
            CTSource = new List<double>(beam.CTSource);
            DeviceSource = new List<double>(beam.DeviceSource);
            NodeID = beam.NodeID;
            AxialAngle = beam.Axial;
            ObliqueAngle = beam.Oblique;
            ID = beam.ID;
            Up = new List<double>(beam.Up);
            CollID = beam.CollID;
            MU = beam.MU;
            DeliveredMU = beam.DeliveredMU;
            PlanMU = beam.PlanMU;
            RefDose = beam.RefDose;
            MaxEffDepth = beam.MaxEffDepth;
            EstimatedUnitDoseAtMVDetector = beam.EstimatedUnitDoseAtMVDetector;
            EstimatedUnitDoseAtMVDetector2 = beam.EstimatedUnitDoseAtMVDetector2;
            DeliveryTime = beam.DeliveryTime;
            TreatmentTime = beam.TreatmentTime;

            // If we have the DP-1011 version beam data, add this values
            CorrectedAxial = (double)(GetPropertyValue(beam, "CorrectedAxial") ?? beam.Axial);
            CorrectedOblique = (double)(GetPropertyValue(beam, "CorrectedOblique") ?? beam.Oblique);
            RotationCorrectionStatus = (RotationCorrectionStatus)(GetPropertyValue(beam, "RotationCorrectionStatus") ?? RotationCorrectionStatus.Uncorrected);
        }

        private object GetPropertyValue(ZapSurgical.Data.Beam beam, string propertyName)
        {
            PropertyInfo property = beam.GetType().GetProperty(propertyName);

            if (property != null)
            {
                return property.GetValue(beam, null);
            }

            return null;
        }
    }
}
