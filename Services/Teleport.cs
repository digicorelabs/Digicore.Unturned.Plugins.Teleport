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

        private ITeleport.Player.Data? GetRequestLatest(
            UnturnedUser userFrom,
            List<ITeleport.Player.Data>? collection
        ) {
            if(collection is null) return null;

            var count = collection?.Count;

            if(count == 1) return collection?[0];

            if(count > 1) {
                ITeleport.Player.Data? target = null;
                ITeleport.Player.Data? previous = null;

                if(collection is not null) {
                    foreach (var request in collection) {
                        if(
                            previous is not null &&
                            request.timestamp > previous.timestamp
                        ) target = request;

                        previous = request;
                    }
                }

                return target;
            }

            return null;
        }

        private ITeleport.Player.Data? GetRequest(
            UnturnedUser userFrom,
            UnturnedUser userTo,
            List<ITeleport.Player.Data>? collection
        ) {
            if(collection is null) return null;

            ITeleport.Player.Data? data = null;

            var userToId = userTo.SteamId.ToString();

            foreach(var request in collection) {
                if(request?.user?.SteamId.ToString() == userToId) data = request;
            }

            return data;
        }

        public Task Accept(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            if (userFrom is null) return Task.CompletedTask;

            var userFromId = userFrom.SteamId.ToString();

            ITeleport.Player.Data? request = userTo is not null ?
                GetRequest(
                    userFrom,
                    userTo,
                    _ledger[userFromId].requests
                ) :
                GetRequestLatest(
                    userFrom,
                    _ledger[userFromId].requests
                );

            if(request is not null) {
                var target = request.userFrom;

                if(target is null) return Task.CompletedTask;

                target.Player.Player.teleportToLocationUnsafe(
                    userFrom.Player.Player.transform.position,
                    userFrom.Player.Player.look.yaw
                );

                _ledger[userFromId]?.requests?.Remove(request);

                _logger.LogInformation($"[Digicore/Teleport/Accept] From: \"{ userFrom.DisplayName }\", To: \"{ target.DisplayName }\"");
            }

            return Task.CompletedTask;
        }

        public Task Deny(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            if (userFrom is null) return Task.CompletedTask;

            var userFromId = userFrom.SteamId.ToString();

            ITeleport.Player.Data? request = userTo is not null ?
                GetRequest(
                    userFrom,
                    userTo,
                    _ledger[userFromId].requests
                ) :
                GetRequestLatest(
                    userFrom,
                    _ledger[userFromId].requests
                );

            if(request is null) return Task.CompletedTask;

            var user = request.user;

            _ledger[userFromId]?.requests?.Remove(request);

            /*
            _ledger[userFromId]?.pending?.Remove(request);
            */

            _logger.LogInformation($"[Digicore/Teleport/Deny] From: \"{ userFrom.DisplayName }\", To: \"{ user?.DisplayName }\"");

            return Task.CompletedTask;
        }

        public Task Cancel(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            if (userFrom is null) return Task.CompletedTask;

            var userFromId = userFrom.SteamId.ToString();

            ITeleport.Player.Data? request = userTo is not null ?
                GetRequest(
                    userFrom,
                    userTo,
                    _ledger[userFromId].pending
                ) :
                GetRequestLatest(
                    userFrom,
                    _ledger[userFromId].pending
                );

            if(request is null) return Task.CompletedTask;
            if(request.user is null) return Task.CompletedTask;

            var requestUser = request.user;
            var requestUserId = request.user.SteamId.ToString();

            _ledger[requestUserId]?.requests?.Remove(request);
            _ledger[userFromId]?.pending?.Remove(request);

            _logger.LogInformation($"[Digicore/Teleport/Cancel] From: \"{ userFrom.DisplayName }\", To: \"{ requestUser?.DisplayName }\"");

            return Task.CompletedTask;
        }

        public async Task Request(
            UnturnedUser userFrom,
            UnturnedUser userTo
        )
        {
            var data = new ITeleport.Player.Data()
            {
                userFrom = userFrom,
                userTo = userTo,
                timestamp = DateTime.UtcNow
            };

            var userToId = userTo?.SteamId.ToString();
            var userFromId = userFrom?.SteamId.ToString();

            if(
                userFromId is null ||
                userToId is null
            ) return;

            // Prevent teleport to self.
            if(userToId == userFromId) return;

            var player = _ledger[userToId];
            var requests = player.requests;

            if(requests is null) return;

            bool duplicate = false;

            // Let's prevent duplicate of the same teleport requests from being stored.
            foreach(var request in requests) {
                if (
                    request?.userFrom is not null &&
                    data?.userFrom is not null &&
                    request?.userFrom?.SteamId == data?.userFrom?.SteamId &&
                    request?.userTo?.SteamId == data?.userTo?.SteamId
                ) duplicate = true;
            }

            //An existing teleport request does not exist.
            if (duplicate) return;

            _ledger[userToId]?.requests?.Add(data);
            _ledger[userFromId]?.pending?.Add(data);

            if(userFrom is not null && userTo is not null) {
                await userFrom.PrintMessageAsync($"[Digicore/Teleport] Teleport request sent to { userTo.DisplayName }.");
                await userTo.PrintMessageAsync($"[Digicore/Teleport] Teleport requested by { userFrom.DisplayName }.");

                _logger.LogInformation($"[Digicore/Teleport/Request] From: \"{ userFrom.DisplayName }\", To: \"{ userTo.DisplayName }\"");
            }
        }

        public Task LedgerAdd(
            string id
        )
        {
            ITeleport.Player player = new ITeleport.Player();

            player.requests = new List<ITeleport.Player.Data>();
            player.matches = new List<UnturnedUser>();
            player.pending = new List<ITeleport.Player.Data>();

            _ledger.Add(id, player);

            _logger.LogInformation($"[Digicore/Teleport/Ledger/Add] {id}");

            return Task.CompletedTask;
        }

        public Task LedgerRemove(
            string id
        )
        {
            _ledger.Remove(id);

            _logger.LogInformation($"[Digicore/Teleport/Ledger/Remove] {id}");

            return Task.CompletedTask;
        }

        public Task MatchAdd(
            string id,
            UnturnedUser match
        ) {
            _ledger[id]?.matches?.Add(match);

            return Task.CompletedTask;
        }

        public Task MatchRemove(
            string id
        ) {
            _ledger[id].matches = new List<UnturnedUser>();

            return Task.CompletedTask;
        }
 
        public List<UnturnedUser>? GetMatches(
            string id
        ) {
            var entry = _ledger[id];

            if(entry is not null) return entry.matches;

            return null;
        }
    }
}
