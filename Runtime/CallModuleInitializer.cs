using UnityEngine;

namespace USerialization.Unity
{
    public class CallModuleInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Call()
        {
            ModuleInitializer.Initialize();
        }
    }
}
