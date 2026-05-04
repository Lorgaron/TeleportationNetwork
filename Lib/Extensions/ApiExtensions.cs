using Vintagestory.API.Common;

namespace TeleportationNetwork.Lib.Extensions
{
    public static class ApiExtensions
    {
        public static string GetWorldId(this ICoreAPI api) => api?.World?.SavegameIdentifier ?? "null";
    }
}
