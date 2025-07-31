using ZapSurgical.Data;

namespace ZapClient.Data
{
    public class PlanData
    {
        // From Broker plan definition
        public string ActiveSecondaryDatasetUUID;
        public string Approver;
        public int BeamsToSkip;
        public string CollimatorSetUUID;
        public string CollisionModel;
        public Constrain[] Constraints;
        public string DensityModelName;
        public string Desc;
        public double DistToCTPost;
        public double DistToCTSup;
        public DoseSet DoseSet;
        public double DoseVolumePixelSpacing;
        public bool EnableBeamDelivery;
        public string FractionTimeEstimate;
        public int FuseMethod;
        public int HeadCenterSagittalSliceIndex;
        public IsocenterSet IsocenterSet;
        public string OptimizationType;
        public Patient Patient;
        public string PlanName;
        public DatasetSetting PrimaryDatasetSetting;
        public string PrimaryDatasetUUID;
        public int Revision;
        public Dataset[] SecondaryDatasets;
        public bool Show3dIsocenters;
        public bool Show3dVOIs;
        public bool ShowBeams;
        public bool ShowContourColorWash;
        public bool ShowDoseClouds;
        public bool ShowIsocenters;
        public bool Simulated;
        public string TPSBuildVersion;
        public string TrackingMaskUUID;
        public string UUID;
        public string Version;
        public bool IsGyroscopicCorrectionOn;
        // Calculated
        public int Isocenters;
        public int Beams;
    }
}
