// © 2015 Mario Lelas
using UnityEngine;

namespace MLSpace
{

    /// <summary>
    /// general purpose void delegate
    /// </summary>
    public delegate void VoidFunc();

    /// <summary>
    /// miscellaneous methods
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// name said it all
        /// </summary>
        /// <param name="mask">layer mask</param>
        /// <param name="layer">layer</param>
        /// <returns>true or false</returns>
        public static bool DoesMaskContainsLayer(LayerMask mask, int layer)
        {
            return (mask == (mask | (1 << layer)));
        }
    }

}
