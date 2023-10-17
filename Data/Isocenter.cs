namespace ZapClient.Data
{
    public class Isocenter
    {
        // From Broker plan definition
        public double[] CTTarget;
        public string CollID;
        public CollisionConfig CollisionConfig;
        public DeliveryInstruction[] DeliveryInstructions;
        public int ID;
        public double TargetDose;
        // From Broker planning isocenter set
        public int IsocenterDeliveryIndex;
        public int IsocenterID;
        public double IsocenterScaleFactor;
        public bool IsocenterVisible;
        public string PathUuid;
        public int Region;
        public DeliveryPath DeliveryPath;
        public IsocenterBeamSet IsocenterBeamSet;
        public Collimator Collimator;
        public double[] AlignmentRotation;
        public double[] AlignmentCenter;
    }
}
