using OpenMod.API.Ioc;
using System.Threading.Tasks;

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
    }
}

