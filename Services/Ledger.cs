using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;
using Digicore.Unturned.Plugins.Teleport.API;

namespace Digicore.Unturned.Plugins.Teleport.Services
{

    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class Ledger : ILedger
    {
        private readonly ILogger<Ledger> _logger;
        private Dictionary<String, List<ILedger.Data>> _ledger = new Dictionary<String, List<ILedger.Data>>();

        public Ledger(
            ILogger<Ledger> logger
        )
        {
            _logger = logger;
        }

        public Task Add(
            string id
        )
        {
            List<ILedger.Data> requests = new List<ILedger.Data>();

            _ledger.Add(id, requests);

            _logger.LogInformation($"[Digicore/Teleport/Ledger] ADDED: {id}");

            return Task.CompletedTask;
        }

        public Task Remove(
            string id
        )
        {
            _ledger.Remove(id);

            _logger.LogInformation($"[Digicore/Teleport/Ledger] REMOVED: {id}");

            return Task.CompletedTask;
        }

        public Task Request(
           string id,
           ILedger.Data data
        )
        {
            bool entered = false;

            var requests = _ledger[id];

            // TODO: PREVENT MULTIPLE OF THE SAME REQUESTS FROM OCURRING.
            // for(entry in requests) {
            //     if(entry.from) entered = true;
            // }

            if (!entered) _ledger[id].Add(data);

            return Task.CompletedTask;
        }
    }
}
