using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using UnityEngine;
using OpenMod.API.Users;
using OpenMod.API.Plugins;
using OpenMod.API.Ioc;
using OpenMod.Core.Commands;
using OpenMod.Core.Ioc;
using OpenMod.Unturned.Plugins;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Players;
using Digicore.Unturned.Plugins.Teleport.API;

namespace Digicore.Unturned.Plugins.Teleport.Commands
{
    [Command("tp")]
    [CommandAlias("tpa")]
    public class Command : UnturnedCommand
    {
        private readonly ILogger<Command> _logger;
        private readonly IUnturnedUserDirectory _unturnedUserDirectory;
        private readonly ITeleport _teleport;

        private string ACTION_ACCEPT = "accept";
        private string ACTION_SHORTCUT_ACCEPT = "a";
        private string ACTION_DENY = "deny";
        private string ACTION_SHORTCUT_DENY = "a";
        private string ACTION_CANCEL = "cancel";
        private string ACTION_SHORTCUT_CANCEL = "c";

        public Command(
            ILogger<Command> logger,
            IServiceProvider serviceProvider,
            ITeleport teleport,
            IUnturnedUserDirectory unturnedUserDirectory
        ) : base(serviceProvider) {
            _unturnedUserDirectory = unturnedUserDirectory;
            _logger = logger;
            _teleport = teleport;
        }

        private async Task<UnturnedUser?> GetMatchFromMatches(
            List<UnturnedUser> matches,
            UnturnedUser userFrom
        )
        {
            if(matches.Count == 1) return matches[0];

            if(matches.Count > 1) {
                var index = 1;
                var userFromId = userFrom.SteamId.ToString();

                await userFrom.PrintMessageAsync("[Digicore/Teleport] Multiple matches found, please select from the following. (tp <match number>)");

                foreach (var match in matches)
                {
                    await userFrom.PrintMessageAsync($"{ index }) { match.DisplayName.ToString() }.");

                    _teleport.MatchAdd(userFromId, match);

                    index++;
                }
            }

            return null;
        }

        private async Task<UnturnedUser?> FindPlayerByPlayerName(
            string? playerName,
            UnturnedUser userFrom
        )
        {
            if(playerName == null) return null;

            List<UnturnedUser> matches = new List<UnturnedUser>();

            var nameToMatchOn = playerName.ToLower();
            var users = _unturnedUserDirectory.GetOnlineUsers();

            foreach (var user in users)
            {
                var username = user.DisplayName.ToLower();

                if(username.Contains(nameToMatchOn)) matches.Add(user);
            }

            return await GetMatchFromMatches(matches, userFrom);
        }

        private bool IsActionAccept(string? action)
        {
            return action == ACTION_ACCEPT || action == ACTION_SHORTCUT_ACCEPT;
        }

        private bool IsActionDeny(string? action)
        {
            return action == ACTION_DENY || action == ACTION_SHORTCUT_DENY;
        }

        private bool IsActionCancel(string? action)
        {
            return action == ACTION_CANCEL || action == ACTION_SHORTCUT_CANCEL;
        }

        private string PrintCommandStructure()
        {
            return "[Digicore/Teleport] tp (player|accept|deny|cancel)";
        }

        private async Task HandleAction(
            string? firstParameter,
            string? secondParameter,
            UnturnedUser? userFrom
        ) {
            /**
            * Optional second parameter can be a player.
            * If the second parameter is a player, then the action corresponds to target player.
            **/

            if(
                firstParameter is null ||
                userFrom is null
            ) return;

            UnturnedUser? userBySecondParameter = FindPlayerByPlayerName(
                secondParameter,
                userFrom
            ).Result;

            // In case a player is searched for, but does not exist.
            if(
                secondParameter?.Length > 0 &&
                userBySecondParameter is null
            ) {
                _logger.LogInformation($"[Digicore] Player \"{ secondParameter }\" not found.");

                return;
            }

            // Either no player's name is passed as a parameter or it was and the the player's information was found and is being passed on.
            if(IsActionAccept(firstParameter)) {
                _teleport.Accept(
                    userFrom,
                    userBySecondParameter
                );

                return;
            }

            if(IsActionDeny(firstParameter)) {
                _teleport.Deny(
                    userFrom,
                    userBySecondParameter
                );

                return;
            }

            if(IsActionCancel(firstParameter)) {
                _teleport.Cancel(
                    userFrom,
                    userBySecondParameter
                );

                return;
            }
        }

        private bool isRequestToAMatch(
            int? firstParameter,
            UnturnedUser? userFrom
        ) {
            try
            {
                var id = userFrom?.SteamId.ToString();

                if(id is null) return false;

                var matches = _teleport.GetMatches(id);

                if(
                    firstParameter is not null &&
                    matches is not null &&
                    matches.Count > 0
                ) return true;
            }
            catch (System.Exception)
            {
                return false;
            }

            return false;
        }

        private async Task HandleMatchRequest(
            int selection,
            UnturnedUser userFrom
        ) {
            if(userFrom is null) return;
            if(selection <= 0) return;

            var id = userFrom.SteamId.ToString();
            var index = selection - 1;
            var matches = _teleport.GetMatches(id);

            if(matches is null) return;

            var match = matches[index];

            if(match is null) return;

            await _teleport.Request(
                userFrom,
                match
            );

            _teleport.MatchRemove(id);
        }

        private async Task<string?> GetParameterAsString(
            OpenMod.API.Commands.ICommandContext Context,
            int index
        ) {
            try
            {
                return await Context.Parameters.GetAsync<string>(index);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private async Task<int> GetParameterAsInt(
            OpenMod.API.Commands.ICommandContext Context,
            int index
        ) {
            try
            {
                return await Context.Parameters.GetAsync<int>(index);
            }
            catch (System.Exception)
            {
                return -1;
            }
        }

        protected override async UniTask OnExecuteAsync()
        {
            var countOfParameters = Context.Parameters.Length;

            if(
                countOfParameters < 1 ||
                countOfParameters > 2
            ) {
                await Context.Actor.PrintMessageAsync(PrintCommandStructure());

                return;
            }

            //Flow: It's an action of accept, deny, or cancel.
            var firstParameterAsString = GetParameterAsString(
                Context,
                0
            ).Result;

            UnturnedUser? userFrom = _unturnedUserDirectory.FindUser(
                Context.Actor.Id, UserSearchMode.FindById
            );

            if(userFrom is null) return;

            if(
                IsActionAccept(firstParameterAsString) ||
                IsActionDeny(firstParameterAsString) ||
                IsActionCancel(firstParameterAsString)
            ) {
                var secondParameter = countOfParameters > 1 ? GetParameterAsString(
                    Context,
                    1
                ).Result : null;

                await HandleAction(firstParameterAsString, secondParameter, userFrom);

                return;
            }

            //Flow: It's a match request.
            var firstParameterAsInt = GetParameterAsInt(
                Context,
                0
            ).Result;

            if(
                isRequestToAMatch(
                    firstParameterAsInt,
                    userFrom
                )
            ) {
                if(firstParameterAsInt < 0) return;

                await HandleMatchRequest(
                    firstParameterAsInt,
                    userFrom
                );

                return;
            } 

            //Flow: It's a request.
            UnturnedUser? userByFirstParameter = FindPlayerByPlayerName(
                firstParameterAsString,
                userFrom
            ).Result;

            if(userByFirstParameter is not null) {
                await _teleport.Request(
                    userFrom,
                    userByFirstParameter
                );

                return;
            }

            //Flow: Other flow paths were unreachable.
            await Context.Actor.PrintMessageAsync(PrintCommandStructure());
        }
    }
}
