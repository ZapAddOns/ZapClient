namespace ZapClient.Data
{
    public class IsocenterSet
    {
        // From Broker plan definition
        public int ActiveID;
        public Isocenter[] Isocenters;
        public int TotalIsocenters;
        // From Broker planning isocenter set
        public double[] MaxInWorld;
        public double[] CenterDV;
    }
}
