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
        void LedgerAdd(
            string id
        );

        void LedgerRemove(
            string id
        );

        void MatchAdd(
            string id,
            UnturnedUser match
        );

        void MatchRemove(
            string id
        );

        List<UnturnedUser>? GetMatches(
            string id
        );

        void Accept(
            UnturnedUser userFrom,
            UnturnedUser? userTo
        );

        void Deny(
            UnturnedUser userFrom,
            UnturnedUser? userTo
        );

        void Cancel(
            UnturnedUser userFrom,
            UnturnedUser? userTo
        );

        Task List(
            UnturnedUser user
        );

        Task Request(
            UnturnedUser userFrom,
            UnturnedUser userTo
        );

        class Player
        {
            public List<ITeleport.Player.Data>? requests;
            public List<UnturnedUser>? matches;
            public List<ITeleport.Player.Data>? pending;

            public class Data
            {
                public UnturnedUser? userFrom { get; set; }
                public UnturnedUser? userTo { get; set; }
                public DateTime timestamp { get; set; }
            }
        }
    }
}
