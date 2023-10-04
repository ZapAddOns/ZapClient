using System;

namespace ZapClient.Data
{
    public class PlanInfo
    {
        public string Name;
        public DateTime LastSaved;
        public DoseVolumeData DoseVolumes;
        public VOIData VOIs;
        public PlanData PlanData;
        public PlanSummary PlanStatus;
        public double TotalMUs;
        public int Isocenters;
        public int Beams;
    }
}
