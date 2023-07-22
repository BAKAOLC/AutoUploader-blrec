using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

namespace AutoUploader
{
    public static class CoreLogger
    {
        private static readonly ISetupBuilder Builder;

        static CoreLogger()
        {
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            Builder = LogManager.Setup().SetupExtensions(ext => ext.RegisterConfigSettings(config));
        }

        public static Logger GetLogger(string name)
        {
            return Builder.GetLogger(name);
        }

        public static void Flush()
        {
            LogManager.Flush();
        }
    }
}
