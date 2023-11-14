using System;
using System.Collections.Generic;
using System.Linq;

namespace ZapClient.Data
{
    public class Fraction
    {
        internal ZapSurgical.Data.Fraction ZapObject { get; }

        public List<ZapSurgical.Data.Path> PathSet { get; }

        public List<Treatment> Treatments { get; } = new List<Treatment>();

        public List<KVImage> KVImages { get; } = new List<KVImage>();

        public double TotalDose { get; }

        public int TotalBeam { get; }

        public int TotalNode { get; }

        public string UUID { get; }

        public short ID { get; }

        public double FractionPlanedDose { get; }

        public bool IsMakeup { get; }

        public DateTime StartTime
        {
            get => Treatments.Select(t => t.StartTime).Min();
        }

        public DateTime EndTime
        {
            get => Treatments.Select(t => t.EndTime).Max();
        }

        public List<string> AAImages { get; set; } = new List<string>();

        public Fraction(ZapSurgical.Data.Fraction fraction)
        {
            fraction.Update();

            ZapObject = fraction;

            PathSet = new List<ZapSurgical.Data.Path>(fraction.PathSet);
            TotalDose = fraction.TotalDose;
            TotalBeam = fraction.TotalBeam;
            TotalNode = fraction.TotalNode;
            UUID = fraction.UUID;
            ID = fraction.ID;
            FractionPlanedDose = fraction.FractionPlanedDose;
            IsMakeup = fraction.IsMakeup;
        }
    }
}
