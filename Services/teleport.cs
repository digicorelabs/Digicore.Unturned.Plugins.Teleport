
using System;
using Digicore.Unturned.Plugins.Teleport.API;

namespace Digicore.Unturned.Plugins.Teleport.Services
{

    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class Service : ITeleport
    {
        public send(){
            return Task.CompletedTask;
        }

        public accept(){
            return Task.CompletedTask;
        }

        public cancel(){
            return Task.CompletedTask;
        }
    }
}
