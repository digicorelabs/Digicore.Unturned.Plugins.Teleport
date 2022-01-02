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
            List<ITeleport.Player.Data>? collection
        )
        {
            if (collection is null) return null;

            var count = collection.Count;

            if (count == 1) return collection[0];

            if (count > 1)
            {
                ITeleport.Player.Data? target = null;
                ITeleport.Player.Data? previous = null;

                if (collection is not null)
                {
                    foreach (var request in collection)
                    {
                        if (
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
            UnturnedUser target,
            string propToMatchRequestTargetOn,
            List<ITeleport.Player.Data>? collection
        )
        {
            if (collection is null) return null;

            ITeleport.Player.Data? data = null;

            var targetId = target.SteamId.ToString();

            foreach (var request in collection)
            {
                UnturnedUser userToMatchTargetOn = (UnturnedUser)typeof(ITeleport.Player.Data).GetField(propToMatchRequestTargetOn).GetValue(request);

                if (
                    userToMatchTargetOn is not null &&
                    userToMatchTargetOn.SteamId.ToString() == targetId
                ) data = request;
            }

            return data;
        }

        private void HandleUserRequestSentTo(
            UnturnedUser? target,
            List<ITeleport.Player.Data>? requestCollection,
            string propToMatchRequestTargetOn,
            Action<UnturnedUser>? onSuccess
        )
        {
            ITeleport.Player.Data? request = target is not null ?
                GetRequest(
                    target,
                    propToMatchRequestTargetOn,
                    requestCollection
                ) :
                GetRequestLatest(
                    requestCollection
                );

            if (request is null) return;

            if (
                onSuccess is not null &&
                request.userFrom is not null
            ) onSuccess(request.userFrom);

            if (request.userTo is not null) _ledger[request.userTo.SteamId.ToString()]?.requests?.Remove(request);
            if (request.userFrom is not null) _ledger[request.userFrom.SteamId.ToString()]?.pending?.Remove(request);
        }


        public void LedgerAdd(
            string id
        )
        {
            ITeleport.Player player = new ITeleport.Player();

            player.requests = new List<ITeleport.Player.Data>();
            player.matches = new List<UnturnedUser>();
            player.pending = new List<ITeleport.Player.Data>();

            _ledger.Add(id, player);

            _logger.LogInformation($"[Digicore/Teleport/Ledger/Add] {id}");
        }

        public void LedgerRemove(
            string id
        )
        {
            _ledger.Remove(id);

            _logger.LogInformation($"[Digicore/Teleport/Ledger/Remove] {id}");
        }

        public void MatchAdd(
            string id,
            UnturnedUser match
        )
        {
            _ledger[id]?.matches?.Add(match);
        }

        public void MatchRemove(
            string id
        )
        {
            _ledger[id].matches = new List<UnturnedUser>();
        }

        public List<UnturnedUser>? GetMatches(
            string id
        )
        {
            var entry = _ledger[id];

            if (entry is not null) return entry.matches;

            return null;
        }

        public void Accept(
            UnturnedUser userWhoAccepted,
            UnturnedUser? userToAccept
        )
        {
            Action<UnturnedUser> onSuccess = new Action<UnturnedUser>((target) =>
            {
                // We can't rely on `userToAccept` here, because we may of not provided it and therefore we go off of the latest request.
                target.Player.Player.teleportToLocationUnsafe(
                    userWhoAccepted.Player.Player.transform.position,
                    userWhoAccepted.Player.Player.look.yaw
                );

                _logger.LogInformation($"[Digicore/Teleport/Accept] \"{ userWhoAccepted.DisplayName }\" teleporting \"{ target.DisplayName }\".");
            });

            HandleUserRequestSentTo(
                userToAccept,
                _ledger[userWhoAccepted.SteamId.ToString()].requests,
                "userFrom",
                onSuccess
            );
        }

        public void Deny(
            UnturnedUser userWhoDenied,
            UnturnedUser? userToDeny
        )
        {
            Action<UnturnedUser> onSuccess = new Action<UnturnedUser>((target) =>
            {
                // We can't rely on `userToDeny` here, because we may of not provided it and therefore we go off of the latest request.
                _logger.LogInformation($"[Digicore/Teleport/Deny] \"{ userWhoDenied.DisplayName }\" denied teleport to \"{ target.DisplayName }\".");
            });

            HandleUserRequestSentTo(
                userToDeny,
                _ledger[userWhoDenied.SteamId.ToString()].requests,
                "userFrom",
                onSuccess
            );
        }

        public void Cancel(
            UnturnedUser userWhoCanceled,
            UnturnedUser? userToCancel
        )
        {
            Action<UnturnedUser> onSuccess = new Action<UnturnedUser>((target) =>
            {
                // We can't rely on `userToCancel` here, because we may of not provided it and therefore we go off of the latest request.
                _logger.LogInformation($"[Digicore/Teleport/Cancel] \"{ userWhoCanceled.DisplayName }\" canceled request for \"{ target.DisplayName }\".");
            });

            HandleUserRequestSentTo(
                userToCancel,
                _ledger[userWhoCanceled.SteamId.ToString()].pending,
                "userTo",
                onSuccess
            );
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

            if (
                userFromId is null ||
                userToId is null
            ) return;

            // Prevent teleport to self.
            if (userToId == userFromId) return;

            var player = _ledger[userToId];
            var requests = player.requests;

            if (requests is null) return;

            bool duplicate = false;

            // Let's prevent duplicate of the same teleport requests from being stored.
            foreach (var request in requests)
            {
                if (
                    request?.userFrom is not null &&
                    data?.userFrom is not null &&
                    request?.userFrom?.SteamId == data?.userFrom?.SteamId &&
                    request?.userTo?.SteamId == data?.userTo?.SteamId
                ) duplicate = true;
            }

            //An existing teleport request does not exist.
            if (duplicate) return;

            // Track teleport requests on user we request to teleport to.
            _ledger[userToId]?.requests?.Add(data);

            // Track pending teleport requests on user who requested.
            _ledger[userFromId]?.pending?.Add(data);


            // Let the player know that the request has been made.
            if (
                userFrom is not null &&
                userTo is not null
            )
            {
                await userFrom.PrintMessageAsync($"[Digicore/Teleport] Teleport request sent to { userTo.DisplayName }.");
                await userTo.PrintMessageAsync($"[Digicore/Teleport] Teleport requested by { userFrom.DisplayName }.");

                _logger.LogInformation($"[Digicore/Teleport/Request] From: \"{ userFrom.DisplayName }\", To: \"{ userTo.DisplayName }\"");
            }
        }
    }
}
