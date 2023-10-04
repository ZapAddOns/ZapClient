namespace ZapClient.Data
{
    public class DoseVolumeData
    {
        public string BeamDataChecksum;
        public StructureData[] DVData;
        public double DVHHundredPercentDose;
        public double DVHOverallMaxDose;
        public string Desc;
        public string PlanUUID;
        public int Revision;
        public string TPSBuildVersion;
        public int TotalDVData;
        public string Version;
    }
}
