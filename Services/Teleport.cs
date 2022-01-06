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

            if (collection.Count == 1) return collection[0];

            if (collection.Count > 1)
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
                var userToMatchTargetOn = (UnturnedUser)typeof(ITeleport.Player.Data).GetField(propToMatchRequestTargetOn).GetValue(request);

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
            Action<ITeleport.Player.Data>? onSuccess
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
                onSuccess is not null
            ) onSuccess(request);

            if (request.userTo is not null) _ledger[request.userTo.SteamId.ToString()]?.requests?.Remove(request);
            if (request.userFrom is not null) _ledger[request.userFrom.SteamId.ToString()]?.pending?.Remove(request);
        }

        private void Analysis(
            UnturnedUser user
        )
        {
            if (user is null) return;

            var name = user.DisplayName;
            var id = user.SteamId.ToString();
            var player = _ledger[id];

            if (
                player is null ||
                player.requests is null ||
                player.pending is null
            ) return;

            _logger.LogInformation($"[Digicore/Teleport/Analysis] Analyzing \"{ name }\"...");

            _logger.LogInformation("[Digicore/Teleport/Analysis/Requests] Requests to other players...");
            _logger.LogInformation($"[Digicore/Teleport/Analysis/Requests] Count: { player.requests.Count }");

            for (int i = 0; i < player.requests.Count; i++)
            {
                var request = player.requests[i];

                _logger.LogInformation($"[Digicore/Teleport/Analysis/Requests/Request] { i }) From: { request?.userFrom?.DisplayName })");
                _logger.LogInformation($"[Digicore/Teleport/Analysis/Requests/Request] { i }) To: { request?.userTo?.DisplayName })");
                _logger.LogInformation($"[Digicore/Teleport/Analysis/Requests/Request] { i }) Timestamp: { request?.timestamp.ToString() })");
            }

            _logger.LogInformation("[Digicore/Teleport/Analysis/Pending] Requests from other players...");
            _logger.LogInformation($"[Digicore/Teleport/Analysis/Pending] Count: { player.pending.Count }");

            for (int i = 0; i < player.requests.Count; i++)
            {
                var request = player.requests[i];

                _logger.LogInformation($"[Digicore/Teleport/Analysis/Pending/Request] { i }) From: { request?.userFrom?.DisplayName })");
                _logger.LogInformation($"[Digicore/Teleport/Analysis/Pending/Request] { i }) To: { request?.userTo?.DisplayName })");
                _logger.LogInformation($"[Digicore/Teleport/Analysis/Pending/Request] { i }) Timestamp: { request?.timestamp.ToString() })");
            }
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
            var player = _ledger[id];

            if (player is not null) return player.matches;

            return null;
        }

        public void Accept(
            UnturnedUser userWhoAccepted,
            UnturnedUser? userToAccept
        )
        {
            Action<ITeleport.Player.Data> onSuccess = new Action<ITeleport.Player.Data>((request) =>
            {
                if (
                    request.userFrom is null ||
                    request.userTo is null
                ) return;

                request.userFrom.Player.Player.teleportToLocationUnsafe(
                    request.userTo.Player.Player.transform.position,
                    request.userTo.Player.Player.look.yaw
                );

                _logger.LogInformation($"[Digicore/Teleport/Accept] \"{ request.userTo.DisplayName }\" teleporting \"{ request.userFrom.DisplayName }\".");
            });

            HandleUserRequestSentTo(
                userToAccept,
                _ledger[userWhoAccepted.SteamId.ToString()].requests,
                "userFrom",
                onSuccess
            );

            Analysis(userWhoAccepted);
        }

        public void Deny(
            UnturnedUser userWhoDenied,
            UnturnedUser? userToDeny
        )
        {
            Action<ITeleport.Player.Data> onSuccess = new Action<ITeleport.Player.Data>(async (request) =>
            {
                _logger.LogInformation($"[Digicore/Teleport/Deny] \"{ request?.userTo?.DisplayName }\" denied teleport to \"{ request?.userFrom?.DisplayName }\".");

                if (request?.userFrom is not null) await request.userFrom.PrintMessageAsync($"[Digicore/Teleport] \"{ userWhoDenied.DisplayName }\" has denied request to teleport.");
            });

            HandleUserRequestSentTo(
                userToDeny,
                _ledger[userWhoDenied.SteamId.ToString()].requests,
                "userFrom",
                onSuccess
            );

            Analysis(userWhoDenied);
        }

        public void Cancel(
            UnturnedUser userWhoCanceled,
            UnturnedUser? userToCancel
        )
        {
            Action<ITeleport.Player.Data> onSuccess = new Action<ITeleport.Player.Data>(async (request) =>
            {
                _logger.LogInformation($"[Digicore/Teleport/Cancel] \"{ request?.userFrom?.DisplayName }\" canceled request for \"{ request?.userTo?.DisplayName }\".");

                if (request?.userTo is not null) await request.userTo.PrintMessageAsync($"[Digicore/Teleport] \"{ request?.userFrom?.DisplayName }\" has canceled request to teleport.");
            });

            HandleUserRequestSentTo(
                userToCancel,
                _ledger[userWhoCanceled.SteamId.ToString()].pending,
                "userTo",
                onSuccess
            );

            Analysis(userWhoCanceled);
        }

        public async Task Request(
            UnturnedUser userWhoRequested,
            UnturnedUser userToRequest
        )
        {
            var data = new ITeleport.Player.Data()
            {
                userFrom = userWhoRequested,
                userTo = userToRequest,
                timestamp = DateTime.UtcNow
            };

            var userWhoRequestedId = userWhoRequested?.SteamId.ToString();
            var userToRequestId = userToRequest?.SteamId.ToString();

            if (
                userWhoRequestedId is null ||
                userToRequestId is null
            ) return;

            // Prevent teleport to self.
            if (userToRequestId == userWhoRequestedId) return;

            var player = _ledger[userToRequestId];
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
            _ledger[userToRequestId]?.requests?.Add(data);

            // Track pending teleport requests on user who requested.
            _ledger[userWhoRequestedId]?.pending?.Add(data);

            // Let the player know that the request has been made.
            if (
                userWhoRequested is not null &&
                userToRequest is not null
            )
            {
                await userWhoRequested.PrintMessageAsync($"[Digicore/Teleport] Teleport request sent to { userToRequest.DisplayName }.");
                await userToRequest.PrintMessageAsync($"[Digicore/Teleport] Teleport requested by { userWhoRequested.DisplayName }.");

                _logger.LogInformation($"[Digicore/Teleport/Request] From: \"{ userWhoRequested.DisplayName }\", To: \"{ userToRequest.DisplayName }\"");

                Analysis(userWhoRequested);
            }
        }

        public async Task List(
            UnturnedUser user
        )
        {
            if(user is null) return;

            var id = user.SteamId.ToString();
            var player = _ledger[id];

            var requests = player.requests;
            var pending = player.pending;

            if(
                pending is not null &&
                pending.Count > 0
            ) {
                await user.PrintMessageAsync("[Digicore/Teleport] Outgoing:");

                for (int i = 0; i < pending.Count; i++)
                {
                    var number = i + 1;
                    var request = pending[i];

                    if(request.userTo is null) return;

                    await user.PrintMessageAsync($"{ number }) { request.userTo.DisplayName }.");
                }
            }

            if(
                requests is not null &&
                requests.Count > 0
            ) {
                await user.PrintMessageAsync("[Digicore/Teleport] Incoming:");

                for (int i = 0; i < requests.Count; i++)
                {
                    var number = i + 1;
                    var request = requests[i];

                    if(request.userFrom is null) return;

                    await user.PrintMessageAsync($"{ number }) { request.userFrom.DisplayName }.");
                }
            }
        }
    }
}
