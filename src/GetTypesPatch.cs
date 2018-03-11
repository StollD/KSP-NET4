using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;

namespace KSPNET4
{
    [HarmonyPatch(typeof(Assembly))]
    [HarmonyPatch("GetTypes")]
    [HarmonyPatch(new Type[0])]
    public class GetTypesPatch
    {
        private static Dictionary<String, Type[]> _types = new Dictionary<String, Type[]>();
        
        public static Boolean Prefix(ref Assembly __instance, ref Type[] __result)
        {
            if (_types.ContainsKey(__instance.FullName))
            {
                __result = _types[__instance.FullName];
                return false;
            }

            return true;
        }

        public static void Postfix(ref Assembly __instance, ref Type[] __result)
        {
            if (!_types.ContainsKey(__instance.FullName))
            {
                _types.Add(__instance.FullName, __result);
            }
        }
    }
}