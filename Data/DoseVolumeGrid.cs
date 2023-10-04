using System.IO;

namespace ZapClient.Data
{
    public class DoseVolumeGrid
    {
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string PlanUuid { get; set; } = string.Empty;
        public string BeamDataChecksum { get; set; } = string.Empty;
        public int Type { get; set; }
        public int[] GridMin { get; set; } = new int[3];
        public int[] GridMax { get; set; } = new int[3];
        public int[] GridSize { get; set; } = new int[3];
        public int SkipFactor { get; set; }
        public double[] Origin { get; set; } = new double[3];
        public double[][] UnitVectors { get; set; } = new double[3][] { new double[3], new double[3], new double[3] };
        public double[] Spacing { get; set; } = new double[3];
        public float Scale { get; set; }
        public float MaxDose { get; set; }
        public int[] MaxDosePoint { get; set; } = new int[3];
        public float[][][] Data { get; set; }

        public static DoseVolumeGrid ParseDoseVolumeGrid(Stream stream)
        {
            BinaryReader binaryReader = new BinaryReader(stream);
            DoseVolumeGrid dvGrid = new DoseVolumeGrid();

            dvGrid.Description = new string(binaryReader.ReadChars(128));
            dvGrid.Version = new string(binaryReader.ReadChars(128));
            dvGrid.PlanUuid = new string(binaryReader.ReadChars(128));
            dvGrid.BeamDataChecksum = new string(binaryReader.ReadChars(128));
            dvGrid.Type = binaryReader.ReadInt32();
            for (var i = 0; i < 3; i++)
            {
                dvGrid.GridMin[i] = binaryReader.ReadInt32();
            }

            for (var i = 0; i < 3; i++)
            {
                dvGrid.GridMax[i] = binaryReader.ReadInt32();
            }

            dvGrid.SkipFactor = binaryReader.ReadInt32();

            for (var i = 0; i < 3; i++)
            {
                dvGrid.GridSize[i] = binaryReader.ReadInt32();
            }

            for (var i = 0; i < 3; i++)
            {
                dvGrid.Origin[i] = binaryReader.ReadDouble();
            }

            for (var j = 0; j < 3; j++)
            {
                dvGrid.UnitVectors[j] = new double[3];
                for (var i = 0; i < 3; i++)
                {
                    dvGrid.UnitVectors[j][i] = binaryReader.ReadDouble();
                }
            }

            for (var i = 0; i < 3; i++)
            {
                dvGrid.Spacing[i] = binaryReader.ReadDouble();
            }

            dvGrid.Scale = binaryReader.ReadSingle();
            dvGrid.MaxDose = binaryReader.ReadSingle();

            for (var i = 0; i < 3; i++)
            {
                dvGrid.MaxDosePoint[i] = binaryReader.ReadInt32();
            }

            dvGrid.Data = new float[dvGrid.GridSize[0]][][];
            for (var k = 0; k < dvGrid.GridSize[0]; k++)
            {
                dvGrid.Data[k] = new float[dvGrid.GridSize[1]][];
                for (var j = 0; j < dvGrid.GridSize[1]; j++)
                {
                    dvGrid.Data[k][j] = new float[dvGrid.GridSize[2]];
                    for (var i = 0; i < dvGrid.GridSize[2]; i++)
                    {
                        var dose = binaryReader.ReadUInt16() * dvGrid.Scale;
                        dvGrid.Data[k][j][i] = dose;
                    }
                }
            }

            return dvGrid;
        }
    }
}
