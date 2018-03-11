using Harmony;
using UnityEngine;

namespace KSPNET4
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class InitHarmony : MonoBehaviour
    {
        void Awake()
        {
            HarmonyInstance instance = HarmonyInstance.Create("KSP-NET4");
            instance.PatchAll(typeof(InitHarmony).Assembly);
        }
    }
}