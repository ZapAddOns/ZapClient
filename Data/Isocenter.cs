namespace ZapClient.Data
{
    public class Isocenter
    {
        public double[] CTTarget;
        public string CollID;
        public CollisionConfig CollisionConfig;
        public DeliveryInstruction[] DeliveryInstructions;
        public int ID;
        public int IsocenterDeliveryIndex;
        public double TargetDose;
        public double IsocenterScaleFactor;
        public DeliveryPath DeliveryPath;
        public IsocenterBeamSet IsocenterBeamSet;
        public Collimator Collimator;
        public double[] AlignmentRotation;
        public int IsocenterID;
        public string PathUuid;
        public int Region;
        public bool IsocenterVisible;
        public double[] AlignmentCenter;
    }
}
