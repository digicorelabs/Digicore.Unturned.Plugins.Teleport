using System.Threading.Tasks;
using Digicore.Unturned.Plugins.Teleport.API;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;

namespace Digicore.Unturned.Plugins.Teleport.Events
{
    public class UnturnedPlayerDisconnectedEventListener : IEventListener<UnturnedPlayerDisconnectedEvent>
    {
        private readonly ILedger _ledger;

        public UnturnedPlayerDisconnectedEventListener(
            ILedger ledger
        )
        {
            _ledger = ledger;
        }

        public async Task HandleEventAsync(object? sender, UnturnedPlayerDisconnectedEvent @event)
        {
            var id = @event.Player.SteamId.ToString();

            await _ledger.Remove(id);
        }
    }
}
