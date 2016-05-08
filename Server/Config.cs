using System.Configuration;

namespace Server
{
    public static class Config
    {
        public static readonly string ExeName =
            ConfigurationManager.AppSettings["ExeName"];

        public static readonly string ExeHash =
            ConfigurationManager.AppSettings["ExeHash"];

        public static readonly int ResponseTimeout = 
            int.Parse(ConfigurationManager.AppSettings["ResponseTimeout"]);

        public static readonly int CheckInterval =
            int.Parse(ConfigurationManager.AppSettings["CheckInterval"]);

        public static readonly int ChecksCount =
            int.Parse(ConfigurationManager.AppSettings["ChecksCount"]);

        public static readonly string DBAddress =
            ConfigurationManager.AppSettings["DBAddress"];

        public static readonly string DBName =
            ConfigurationManager.AppSettings["DBName"];

        public static readonly string Host =
            ConfigurationManager.AppSettings["Host"];

        public static readonly int Port =
            int.Parse(ConfigurationManager.AppSettings["Port"]);
    }
}
