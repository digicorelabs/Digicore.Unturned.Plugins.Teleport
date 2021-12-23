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

        private UnturnedUser? FindPlayerByPlayerName(string? playerName)
        {
            if(playerName == null) return null;

            UnturnedUser? match = null;

            List<UnturnedUser> matches = new List<UnturnedUser>();

            var nameToMatchOn = playerName.ToLower();
            var users = _unturnedUserDirectory.GetOnlineUsers();

            foreach (var user in users)
            {
                var username = user.DisplayName.ToLower();

                if(username == nameToMatchOn) match = user;
                if(username.Contains(nameToMatchOn)) matches.Add(user);
            }

            if(match != null) {
                return match;
            } else if(matches.Count > 1) {
                // TODO: SUPPORT MULTIPLE MATCHES.
            } else if(matches.Count == 1) {
                // Did you mean this one match?
                return matches[0];
            }

            return null;
        }

        private bool IsActionAccept(string action)
        {
            return action == ACTION_ACCEPT || action == ACTION_SHORTCUT_ACCEPT;
        }

        private bool IsActionDeny(string action)
        {
            return action == ACTION_DENY || action == ACTION_SHORTCUT_DENY;
        }

        private bool IsActionCancel(string action)
        {
            return action == ACTION_CANCEL || action == ACTION_SHORTCUT_CANCEL;
        }

        private bool IsAnAction(string action)
        {
            return IsActionAccept(action) || IsActionDeny(action) || IsActionCancel(action);
        }

        private string PrintCommandStructure()
        {
            return "tp (player|accept|deny|cancel)";
        }

        protected async override UniTask OnExecuteAsync()
        {
            if(Context.Parameters.Length < 1 || Context.Parameters.Length > 2) await Context.Actor.PrintMessageAsync(PrintCommandStructure());

            var firstParameter = await Context.Parameters.GetAsync<string>(0);

            UnturnedUser? userByFirstParameter = FindPlayerByPlayerName(firstParameter);
            UnturnedUser? userFrom = _unturnedUserDirectory.FindUser(Context.Actor.Id, UserSearchMode.FindById);

            // Check if the first parameter is a player.
            if(userByFirstParameter != null) {
                await _teleport.Request(
                    userFrom,
                    userByFirstParameter
                );
            // First parameter is not a player, maybe a command?
            } else if(IsAnAction(firstParameter)) {
                /**
                * Optional second parameter can be a player.
                * If the second parameter is a player, then the action correspond to target player.
                **/

                var secondParameter = await Context.Parameters.GetAsync<string>(1) ?? null;

                UnturnedUser? userBySecondParameter =  FindPlayerByPlayerName(secondParameter);

                if(
                    IsActionAccept(firstParameter)
                ) {
                    await _teleport.Accept(
                        userFrom,
                        userBySecondParameter
                    );
                } else if(
                    IsActionDeny(firstParameter)
                ) {
                    await _teleport.Deny(
                        userFrom,
                        userBySecondParameter
                    );
                } else if(
                    IsActionCancel(firstParameter)
                ) {
                    await _teleport.Cancel(
                        userFrom,
                        userBySecondParameter
                    );
                }
            } else {
                await Context.Actor.PrintMessageAsync(PrintCommandStructure());
            }
        }
    }
}
