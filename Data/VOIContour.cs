namespace ZapClient.Data
{
    public class VOIContour
    {
        public int[] Color;
        public VOIContourPoints[] Contours;
        public double DVHLine;
        public int[] ImageSize;
        public double MaxDose;
        public double MinDose;
        public string Name = string.Empty;
        public int Orientation;
        public double ReducedBeamMUPercentage;
        public double ReducedBeamMUPercentageForPrescription;
        public bool Show;
        public bool ShowDVH;
        public double TargetRxDose;
        public double TargetRxPercent;
        public double TotalVolume;
        public VOIContourType Type;
        public string UUID;
        public bool isMET;

        public string TypeAsString()
        {
            return Type == VOIContourType.Target ? "T" : (Type == VOIContourType.Critical ? "C" : "W");
        }
    }
}
