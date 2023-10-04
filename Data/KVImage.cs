using System;
using ZapSurgical.Data;

namespace ZapClient.Data
{
    public class KVImage
    {
        public string PathUUID { get; internal set; }

        public Node Node { get; internal set; } = null;

        public string CollID { get; set; } = "~";

        public double AxialDegrees { get; internal set; }

        public double ObliqueDegrees { get; internal set; }

        public short KV { get; internal set; }

        public short MA { get; internal set; }

        public short MS { get; internal set; }

        public double KVDoseMicroGy { get => CalcDoseForValues(KV, MA, MS); }

        public double MVDoseMU { get; internal set; } = -999999.0;

        public ZFile RawImage { get; internal set; }

        public ZFile CorrectedImage { get; internal set; }

        public DateTime Timestamp { get; set; } = ZData.DefaultDateTime;

        public string Type { get; set; } = "~";

        public string TreatmentType { get; set; } = "~";

        public string PlanName { get; set; } = "~";

        public KVImage(RadiationTimeRecord rtr)
        {
            AxialDegrees = rtr.AxialAngle;
            ObliqueDegrees = rtr.ObliqueAngle;
            KV = rtr.KV;
            MA = rtr.MA;
            MS = rtr.MS;
            CollID = rtr.CollID;
            Timestamp = rtr.Timestamp;
            Type = rtr.Type;
            TreatmentType = rtr.TreatmentType;
            PlanName = rtr.PlanName;
        }

        private double CalcDoseForValues(short kv, short ma, short ms)
        {
            var dose = 1.15 * 11.02 * Math.Pow(kv / 95.0, 3) * (ma / 25.0) * (ms / 20.0);

            return dose;
        }
    }
}
