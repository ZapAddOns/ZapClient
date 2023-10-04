using System.Collections.Generic;

namespace ZapClient.Data
{
    public class Path
    {
        internal ZapSurgical.Data.Path ZapObject { get; }

        public double TotalDose;
        public int TotalBeam;
        public int TotalNode;
        public string PathUUID;
        public List<double> AlignmentCenter;
        public List<double> AlignmentRotation;
        public int IsocenterID;
        public List<Beam> Beams;
        public string UUID;
        public List<ZapSurgical.Data.KVImageOnNodeData> OnNodekVImages = new List<ZapSurgical.Data.KVImageOnNodeData>();
        public List<ZapSurgical.Data.KVImageOffNodeData> OffNodekVImages = new List<ZapSurgical.Data.KVImageOffNodeData>();

        public Path(ZapSurgical.Data.Path path)
        {
            ZapObject = path;

            TotalDose = path.TotalDose;
            TotalBeam = path.TotalBeam;
            TotalNode = path.TotalNode;
            AlignmentCenter = new List<double>(path.AlignmentCenter);
            AlignmentRotation = new List<double>(path.AlignmentRotation);
            PathUUID = path.PathUUID.ToUpper();
            IsocenterID = path.IsocenterID;
            UUID = path.UUID;
            Beams = new List<Beam>(path.Beams.Count);

            foreach (var beam in path.Beams)
            {
                Beams.Add(new Beam(beam));
            }
        }
    }
}
