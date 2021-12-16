using System;
using OpenMod.API.Users;
using OpenMod.Unturned.Commands;

namespace Digicore.Unturned.Plugins.Teleport.Commands
{
    [Command("tp")]
    public class Command : UnturnedCommand
    {
        public Command(
            IServiceProvider serviceProvider
        ) : base(ServiceProvider) {
            
        }

        public async Task OnExecuteAsync()
        {

        }
    }
}
