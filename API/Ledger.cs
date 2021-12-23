using System;
using System.Threading.Tasks;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;

namespace Digicore.Unturned.Plugins.Teleport.API
{
    [Service]
    public interface ILedger
    {
        Task Add(
            string id
        );

        Task Remove(
            string id
        );

        Task Request(
            string id,
            ILedger.Data data
        );

        class Data
        {
            public UnturnedUser? from { get; set; }
            public UnturnedUser? to { get; set; }
            public DateTime timestamp { get; set; }
        }
    }
}
