using System.Threading.Tasks;
using Digicore.Unturned.Plugins.Teleport.API;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;

namespace Digicore.Unturned.Plugins.Teleport.Events
{
    public class UnturnedPlayerConnectedEventListener : IEventListener<UnturnedPlayerConnectedEvent>
    {
        private ITeleport _teleport;

        public UnturnedPlayerConnectedEventListener(
            ITeleport teleport
        ) {
            _teleport = teleport;
        }

        public async Task HandleEventAsync(object? sender, UnturnedPlayerConnectedEvent @event)
        {
            var id = @event.Player.SteamId.ToString();

            await Task.Run(() => {
                _teleport.LedgerAdd(id);
            });
        }
    }
}
