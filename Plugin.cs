using System;
using Microsoft.Extensions.Logging;
using Cysharp.Threading.Tasks;
using OpenMod.Unturned.Plugins;
using OpenMod.API.Plugins;

[assembly: PluginMetadata("Digicore.Unturned.Plugins.Teleport", DisplayName = "Digicore.Unturned.Plugins.Teleport", Author = "Digicore Labs", Website = "digicorelabs.com")]
namespace Digicore.Unturned.Plugins.Teleport
{
    public class Plugin : OpenModUnturnedPlugin
    {
        private readonly ILogger<Plugin> m_Logger;

        public Plugin(
            ILogger<Plugin> logger,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Logger = logger;
        }

        protected override UniTask OnLoadAsync()
        {
            m_Logger.LogInformation("[Digicore.Unturned.Plugins.Teleport] MESSAGE: TPA has loaded.");

            return UniTask.CompletedTask;
        }
    }
}
