using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace TeleportationNetwork.Lib.Extensions
{
    public static class RandomExtensions
    {
        public static T GetItem<T>(this LCGRandom random, IList<T> values)
        {
            return values[random.NextInt(values.Count)];
        }
    }
}
