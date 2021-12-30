using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;

namespace Digicore.Unturned.Plugins.Teleport.API
{
    [Service]
    public interface ITeleport
    {
        Task Accept(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        );

        Task Deny(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        );

        Task Cancel(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        );

        Task Request(
            UnturnedUser? userFrom,
            UnturnedUser? userTo
        );

        Task LedgerAdd(
            string id
        );

        Task LedgerRemove(
            string id
        );

        Task MatchAdd(
            string id,
            UnturnedUser match
        );

        Task MatchRemove(
            string id
        );

        class Player
        {
            public List<ITeleport.Player.Data>? requests;
            public List<UnturnedUser>? matches;

            public class Data
            {
                public UnturnedUser? user { get; set; }
                public DateTime timestamp { get; set; }
            }
        }
    }
}
