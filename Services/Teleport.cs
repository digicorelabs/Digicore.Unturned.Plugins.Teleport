using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;
using Digicore.Unturned.Plugins.Teleport.API;

namespace Digicore.Unturned.Plugins.Teleport.Services
{

    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class Teleport : ITeleport
    {
        private readonly ILedger _ledger;

        public Teleport(
            ILedger ledger
        )
        {
            _ledger = ledger;
        }

        public Task Accept(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        )
        {
            if (userFrom is null || userTo is null) return Task.CompletedTask;

            // IF PLAYER IS NULL THEN ACCEPT THE LAST TELEPORT REQUEST

            // /tpa a
            // ACCEPTS THE LATEST TPA

            // /tpa a jay
            // ACCEPTS THE TPA REQUEST PARTIAL MATCH
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
            UnturnedUser userFrom,
            UnturnedUser? userTo
        )
        {
            // Request requires a user to teleport to.
            if (userTo is not null) {
                await userFrom.PrintMessageAsync($"Teleport request sent to {userTo.DisplayName}.");

                await userTo.PrintMessageAsync($"Teleport requested by {userFrom.DisplayName}.");
                await userTo.PrintMessageAsync("tp (accept|deny)");

                // REQUEST LOGIC
                //_ledger.Request(userTo.Id, userFrom.Id);
            }
        }
    }
}
