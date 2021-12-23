using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;
using Digicore.Unturned.Plugins.Teleport.API;

namespace Digicore.Unturned.Plugins.Teleport.Services
{

    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class Ledger : ILedger
    {
        private Dictionary<String, List<List<Data>>> ledger = new Dictionary<String, List<List<Data>>>();

        private class Data
        {
            public UnturnedUser From { get; set; }
            public UnturnedUser To { get; set; }
            public TimeSpan Timestamp { get; set; }
        }

        public Task Add(
            string id
        )
        {
            List<List<Data>> requests = new List<List<Data>>();

            ledger.Add(id, requests);

            return Task.CompletedTask;
        }

        public Task Remove(
            string id
        )
        {
            ledger.Remove(id);

            return Task.CompletedTask;
        }
    }
}
