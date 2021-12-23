using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Digicore.Unturned.Plugins.Teleport.API;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;

namespace Digicore.Unturned.Plugins.Teleport.Events
{
    public class UnturnedPlayerConnectedEventListener : IEventListener<UnturnedPlayerConnectedEvent>
    {
        private readonly ILedger _ledger;
        private readonly ILogger<UnturnedPlayerConnectedEventListener> _logger;

        public UnturnedPlayerConnectedEventListener(
            ILedger ledger,
            ILogger<UnturnedPlayerConnectedEventListener> logger
        )
        {
            _logger = logger;
            _ledger = ledger;
        }

        public async Task HandleEventAsync(object? sender, UnturnedPlayerConnectedEvent @event)
        {
            var playerID = @event.Player.SteamPlayer.playerID.ToString();
            var SteamId = @event.Player.SteamId.ToString();

            _logger.LogInformation($"[DIGICORE/UnturnedPlayerConnectedEvent] playerID: {playerID}");
            _logger.LogInformation($"[DIGICORE/UnturnedPlayerConnectedEvent] SteamId: {SteamId}");

            var id = SteamId;

            await _ledger.Add(id);
        }
    }
}
