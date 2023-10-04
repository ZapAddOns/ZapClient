using ZapSurgical.Data;

namespace ZapClient.Extensions
{
    public static class PatientExtension
    {
        public static string PatientFullName(this Patient patient)
        {
            var patientName = $"{patient.LastName.Trim()}, {patient.FirstName.Trim()}";
            if (patient.MiddleName != null)
                patientName += " " + patient.MiddleName;

            return patientName;
        }
    }
}
