using System;

namespace ZapClient.Data
{
    public class DensityModel
    {
        public string DensityModelUuid;
        public string DensityModelName;
        public DateTime DensityModelLastChangeDate;
        public int DensityModelRevision;
        public DensityModelData[] Data;
    }
}
