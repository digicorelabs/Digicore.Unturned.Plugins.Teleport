using System.Threading.Tasks;
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
            UnturnedUser userFrom,
            UnturnedUser? userTo
        );
    }
}
