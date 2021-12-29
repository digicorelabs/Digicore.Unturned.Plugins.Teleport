using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;
using Digicore.Unturned.Plugins.Teleport.API;

namespace Digicore.Unturned.Plugins.Teleport.Services
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class Teleport : ITeleport
    {
        private static ILogger<Teleport>? _logger;
        private static Dictionary<String, ITeleport.Player> _ledger = new Dictionary<String, ITeleport.Player>();

        public Teleport(
            ILogger<Teleport> logger
        )
        {
            _logger = logger;
        }

        private ITeleport.Player.Data? GetRequestByTimestamp(
            UnturnedUser userFrom
        ) {
            _logger.LogInformation("[Digicore/GetRequestByTimestamp]");

            ITeleport.Player.Data? target = null;

            var userFromId = userFrom.SteamId.ToString();

            var requests = _ledger[userFromId].requests;
            var count = requests?.Count;

            if(count == 1) {
                target = requests?[0];
            } else if(count > 1) {
                ITeleport.Player.Data? previous = null;

                if(requests is not null) {
                    foreach (var request in requests) {
                        if(
                            previous is not null &&
                            request.timestamp > previous.timestamp
                        ) target = request;

                        previous = request;
                    }
                }
            }

            _logger.LogInformation($"[Digicore/GetRequestByTimestamp] target: {target?.user?.DisplayName.ToString()}");

            return target;
        }

        private ITeleport.Player.Data? GetRequestByUser(
            UnturnedUser userFrom,
            UnturnedUser userTo
        ) {
            _logger.LogInformation("[Digicore/GetRequestByUser]");

            ITeleport.Player.Data? target = null;

            var userFromId = userFrom.SteamId.ToString();
            var userToId = userTo.SteamId.ToString();

            var requests = _ledger[userFromId].requests;

            if(requests is not null) {
                foreach(var request in requests) {
                    if(request?.user?.SteamId.ToString() == userToId) target = request;
                }
            }

            _logger.LogInformation($"[Digicore/GetRequestByUser] target: {target?.user?.DisplayName.ToString()}");

            return target;
        }

        public Task Accept(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            if (userFrom is null) return Task.CompletedTask;

            _logger.LogInformation($"[Digicore/Accept] Accepting a request...");

            _logger.LogInformation("[Digicore/Accept] here: 5");

            ITeleport.Player.Data? request = userTo is not null ?
                GetRequestByUser(userFrom, userTo) :
                GetRequestByTimestamp(userFrom);

            _logger.LogInformation("[Digicore/Accept] here: 6");

            if(request is not null) {
                var userFromId = userFrom.SteamId.ToString();
                var user = request.user;

                if(user is not null) {
                    _logger.LogInformation($"[Digicore/Accept] request: { user.DisplayName }");

                    user.Player.Player.teleportToLocationUnsafe(
                        userFrom.Player.Player.transform.position,
                        userFrom.Player.Player.look.yaw
                    );
                }

                _ledger[userFromId]?.requests?.Remove(request);
            }

            return Task.CompletedTask;
        }

        public Task Deny(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            if (userFrom is null || userTo is null) return Task.CompletedTask;
            // IF PLAYER IS NULL THEN DENY THE LAST TELEPORT REQUEST

            return Task.CompletedTask;
        }

        public Task Cancel(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            if (userFrom is null || userTo is null) return Task.CompletedTask;
            // IF PLAYER IS NULL THEN CANCEL THE LAST TELEPORT REQUEST

            return Task.CompletedTask;
        }

        public async Task Request(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            // Request requires a user to teleport to.
            if (
                userTo is not null &&
                userFrom is not null
            ) {
                var data = new ITeleport.Player.Data()
                {
                    user = userFrom,
                    timestamp = DateTime.UtcNow
                };

                var userToId = userTo.SteamId.ToString();
                var userFromId = userFrom.SteamId.ToString();

                _logger.LogInformation($"[Digicore/Teleport/Ledger] Request made for { userTo.DisplayName }");

                // Prevent teleport to self.
                if(userToId == userFromId) return;

                var player = _ledger[userToId];
                var requests = player.requests;

                if(requests is not null) {
                    _logger.LogInformation($"[Digicore/Teleport/Ledger] requests: { requests.Count }");
                    
                    bool duplicate = false;

                    // Let's prevent duplicate of the same teleport requests from being stored.
                    foreach(var request in requests) {
                        if (
                            request.user is not null &&
                            data.user is not null &&
                            request.user.SteamId == data.user.SteamId
                        ) duplicate = true;
                    }

                    _logger.LogInformation($"[Digicore/Teleport/Ledger] duplicate: { duplicate }");

                    //An existing teleport request does not exist.
                    if (duplicate == false) {
                        _ledger[userToId]?.requests?.Add(data);

                        await userFrom.PrintMessageAsync($"Teleport request sent to {userTo.DisplayName}.");

                        await userTo.PrintMessageAsync($"Teleport requested by {userFrom.DisplayName}.");
                        await userTo.PrintMessageAsync("tp (accept|deny)");

                        _logger.LogInformation($"[Digicore/Teleport/Ledger] Request Count: {requests.Count}");
                    }
                }
            }
        }

        public Task LedgerAdd(
            string id
        )
        {
            ITeleport.Player player = new ITeleport.Player();

            player.requests = new List<ITeleport.Player.Data>();
            player.matches = new List<string>();

            _ledger.Add(id, player);

            _logger.LogInformation($"[Digicore/Teleport/Ledger] ADDED: {id}");

            return Task.CompletedTask;
        }

        public Task LedgerRemove(
            string id
        )
        {
            _ledger.Remove(id);

            _logger.LogInformation($"[Digicore/Teleport/Ledger] REMOVED: {id}");

            return Task.CompletedTask;
        }
    }
}
