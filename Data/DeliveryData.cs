using System;
using System.Collections.Generic;
using ZapSurgical.Data;

namespace ZapClient.Data
{
    public class DeliveryData
    {
        internal DeliveryBeamSet ZapObject { get; }

        public string Desc { get; }

        public string Version { get; }

        public string PlanUUID { get; }

        public string CollimatorSetUUID { get; }

        public string PathSetUUID { get; }

        public List<double> HeadCenter { get; }
        
        public List<double> RefPoint { get; }

        public double RefDose { get; }

        public double TotalDose { get; }

        public List<Fraction> Fractions { get; }

        public int TotalFractions { get; }

        public int TotalTreatments 
        { 
            get
            {
                var result = 0;

                Fractions.ForEach(f => result += f.Treatments.Count);

                return result;
            }
        }

        public DateTime StartTime
        {
            get
            {
                var result = DateTime.MaxValue;

                foreach (var fraction in Fractions)
                { 
                    result = fraction.StartTime < result ? fraction.StartTime : result;
                }

                return result;
            }
        }

        public DateTime EndTime
        {
            get
            {
                var result = DateTime.MinValue;

                foreach (var fraction in Fractions)
                { 
                    result = fraction.EndTime > result ? fraction.EndTime : result;
                }

                return result;
            }
        }

        public string UUID { get; }

        public DeliveryData(DeliveryBeamSet deliveryBeamSet)
        {
            deliveryBeamSet.RemoveZeroDosePath();

            ZapObject = deliveryBeamSet;

            Desc = deliveryBeamSet.Desc;
            Version = deliveryBeamSet.Version;
            PlanUUID = deliveryBeamSet.PlanUUID;
            CollimatorSetUUID = deliveryBeamSet.CollimatorSetUUID;
            PathSetUUID = deliveryBeamSet.PathSetUUID;
            HeadCenter = new List<double>(deliveryBeamSet.HeadCenter);
            RefPoint = new List<double>(deliveryBeamSet.RefPoint);
            RefDose = deliveryBeamSet.RefDose;
            TotalDose = deliveryBeamSet.TotalDose;
            TotalFractions = deliveryBeamSet.TotalFractions;
            UUID = deliveryBeamSet.UUID;

            Fractions = new List<Fraction>();
            
            foreach (var fraction in deliveryBeamSet.Fractions)
            {
                Fractions.Add(new Fraction(fraction));
            }
        }
    }
}
