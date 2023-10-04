using System;
using System.Collections.Generic;
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
        }
    }
}
