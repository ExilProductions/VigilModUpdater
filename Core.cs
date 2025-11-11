using MelonLoader;

[assembly: MelonInfo(typeof(VigilModUpdater.Core), "VigilModUpdater", "1.0.0", "Exil_S", null)]
[assembly: MelonGame("Singularity Studios", "Vigil")]

namespace VigilModUpdater
{
    public class Core : MelonPlugin
    {
        public static MelonLogger.Instance Logger
        {
            get {
                if (_logger != null)
                {
                    return _logger;
                }
                else
                {
                    throw new System.Exception("Logger instance is not initialized yet.");
                }
            }
        }

        private static MelonLogger.Instance _logger;

        private Updater _updater;

        public override void OnPreInitialization()
        {
            _logger = LoggerInstance;
            _updater = new Updater();
        }

        public override void OnApplicationQuit()
        {
            _updater.Dispose();
        }
    }
}