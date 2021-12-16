using System;

namespace Digicore.Unturned.Plugins.Teleport.API
{
    [Service]
    public interface ITeleport
    {
        Task send();
        Task accept();
        Task cancel();
    }
}
