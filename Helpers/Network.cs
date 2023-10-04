using System.Net;

namespace ZapClient.Helpers
{
    public static class Network
    {
        public static string GetHostName()
        {
            // Retrive the Name of HOST
            return Dns.GetHostName();
        }

        public static string GetIPAdress()
        {
            // Retrive the Name of HOST
            var hostName = Dns.GetHostName();
            // Get the IP
            string result = string.Empty;

            try
            {
                foreach (var ip in Dns.GetHostAddresses(hostName))
                {
                    // ZAP uses always a 10.0.0.255 adress
                    if (ip.ToString().StartsWith("10."))
                        result = ip.GetAddressBytes()[3].ToString("000");
                }
            }
            catch {}

            return result;
        }
    }
}
