namespace BasicDebugger
{
    internal class Debugger : UnityEngine.MonoBehaviour
    {
        internal static System.Collections.Generic.List<byte> ExclusionList = new System.Collections.Generic.List<byte>();

        internal static string ConfigPath = System.IO.Path.Combine(BepInEx.Paths.PluginPath, "Exclusions.txt");

        internal static System.Collections.Generic.Dictionary<byte, string> EventTooltips = new System.Collections.Generic.Dictionary<byte, string>()
        {
            { 176, "Projectile spawned! (176)" },
            { 200, "RPC Called! (200)" },
            { 206, "Serialization data received! (201/206)" },
            { 202, "Something was instantiated! (202)" },
        };

        void Start()
        {
            Photon.Pun.PhotonNetwork.NetworkingClient.EventReceived += OnEvent;

            if (!System.IO.File.Exists(ConfigPath))
            {
                UnityEngine.Debug.LogError("Exclusion path not found. Automatically creating one!");
                System.IO.File.WriteAllText(ConfigPath, string.Empty);
            }

            string[] lines = System.IO.File.ReadAllLines(ConfigPath);

            foreach (string line in lines)
            {
                // Honestly dont think anyone is stupid enough to leave whitespaces but I dont need people complaining to me so whatever

                if (byte.TryParse(line.Trim(), out byte eventCode))
                {
                    ExclusionList.Add(eventCode);
                }
            }
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null (null)";

            System.Type type = value.GetType();

            if (type.IsArray && value is object[] array)
            {
                System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
                foreach (var item in array)
                {
                    parts.Add(FormatValue(item));
                }

                return $"[ {string.Join(", ", parts)} ] (object[])";
            }

            if (value is ExitGames.Client.Photon.Hashtable hashtable)
            {
                System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
                foreach (System.Collections.DictionaryEntry entry in hashtable)
                {
                    parts.Add($"{entry.Key}: {FormatValue(entry.Value)}");
                }

                return $"{{ {string.Join(", ", parts)} }} (Hashtable)";
            }

            return $"{value} ({type.Name})";
        }

        internal static void OnEvent(ExitGames.Client.Photon.EventData photonEvent)
        {
            // Was getting some errors so had to add this check lmao

            if (!Photon.Pun.PhotonNetwork.InRoom) return;

            // Remember having some issues with event code 0 being spammed, no idea if that was user error but adding the check anyway

            if (ExclusionList.Contains(photonEvent.Code) || photonEvent.Code == 0) return;

            string finalDebugMessage = "";

            Photon.Realtime.Player sender = Photon.Pun.PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(photonEvent.Sender, false);

            System.Collections.Generic.List<string> eventData = new System.Collections.Generic.List<string>();

            if (photonEvent.Code == 200 && photonEvent.CustomData is ExitGames.Client.Photon.Hashtable rpcData)
            {
                foreach (System.Collections.DictionaryEntry entry in rpcData)
                {
                    eventData.Add($"{entry.Key}: {FormatValue(entry.Value)}");
                }
            }
            else if (photonEvent[245] is object[] data)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    object value = data[i];
                    eventData.Add($"{(value != null ? $"{value} ({value.GetType().Name})" : "null (null)")}");
                    eventData.Add(FormatValue(value));
                }
            }

            finalDebugMessage = EventTooltips.TryGetValue(photonEvent.Code, out var tooltip)
                ? tooltip
                : $"Undocumented event called! ({photonEvent.Code})";

            finalDebugMessage = $"{finalDebugMessage}\nSender: {sender}\nData:\n{string.Join("\n", eventData)}";

            UnityEngine.Debug.Log(finalDebugMessage);
        }
    }
}
