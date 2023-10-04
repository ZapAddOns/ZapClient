using System;
using System.Collections.Generic;
using ZapSurgical.Data;

namespace ZapClient.Data
{
    public class Treatment
    {
        public ZSystem System { get; } = new ZSystem();

        public User User { get; } = new User();

        public List<Path> Paths { get; } = new List<Path>();

        //public List<> AAImages { get; }

        //public List<> TAImages { get; }

        //public List<> KVImages { get; }

        //public List<> MVImages { get; }

        public double TotalTime { get; }

        public double InitializeTime { get; }

        public double SetupTime { get; }

        public double DeliveryTime { get; }

        public double TableTime { get; }

        public double LinacTime { get; }

        public double ImageTime { get; }

        public double GantryTime { get; }

        public DateTime StartTime { get; }

        public DateTime EndTime { get; }
        
        public Fraction Fraction { get; }

        public string Uuid { get; }

        public DateTime CreationTime { get; }

        public string DeliverySoftwareVersion { get; }

        public string AdditionalInformationJson { get; }

        public Treatment(TreatmentReportData reportData, Fraction fraction)
        {
            Fraction = fraction;
            System = reportData.System;
            User = reportData.User;
            TotalTime = reportData.TotalTime;
            InitializeTime = reportData.InitializeTime;
            SetupTime = reportData.SetupTime;
            DeliveryTime = reportData.DeliveryTime;
            TableTime = reportData.TableTime;
            LinacTime = reportData.LinacTime;
            ImageTime = reportData.ImageTime;
            GantryTime = reportData.GantryTime;
            StartTime = reportData.StartTime;
            EndTime = reportData.EndTime;
            CreationTime = reportData.CreationTime;
            Uuid = reportData.Uuid;
            DeliverySoftwareVersion = reportData.DeliverySoftwareVersion;
            AdditionalInformationJson = reportData.AdditionalInformationJson;
        }
    }
}
