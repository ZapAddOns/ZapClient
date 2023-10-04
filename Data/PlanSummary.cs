namespace ZapClient.Data
{
    public class PlanSummary
    {
        // Original fields
        public string Desc;
        public string Modality;
        public float PrescribedDose;
        public float PrescribedPercent;
        public int Revision;
        public string TPSBuildVersion;
        public int TotalBeamsWithMU;
        public int TotalContouredVOIs;
        public int TotalFractions;
        public int TotalIsocenters;
        public string Version;
        // Calculated fields
        public double TotalMUs;
        public double TotalTreatmentTime;
    }
}
