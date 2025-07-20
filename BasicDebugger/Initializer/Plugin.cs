namespace BasicDebugger.Initializer
{
    [BepInEx.BepInPlugin("basic.debugger", "Basic Debugger", "0.0.1")]
    internal class Plugin : BepInEx.BaseUnityPlugin
    {
        void Awake()
        {
            // Using this is much easier and less detectable then a HarmonyPatch

            gameObject.AddComponent<Debugger>();

            UnityEngine.Debug.Log("Basic Debugger loaded!");
        }
    }
}
