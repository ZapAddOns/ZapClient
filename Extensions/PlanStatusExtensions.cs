using System;
using ZapSurgical.Data;

namespace ZapReport.Extensions
{
    public static class PlanStatusExtensions
    {
        public static string AsString(this PlanStatus status)
        {
            switch (status)
            {
                case PlanStatus.Temporary:
                    return "Temporary";
                case PlanStatus.InProgress:
                    return "In progress";
                case PlanStatus.Deliverable:
                    return "Deliverable";
                case PlanStatus.PartiallyDelivered:
                    return "Partially delivered";
                case PlanStatus.FullyDelivered:
                    return "Fully delivered";
                case PlanStatus.Deleted:
                    return "Deleted";
                case PlanStatus.Unknown:
                    return "Unknown";
            }

            return String.Empty;
        }
    }
}
