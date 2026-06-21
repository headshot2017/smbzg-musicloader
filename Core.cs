using MelonLoader;
using HarmonyLib;
using UnityEngine;

[assembly: MelonInfo(typeof(MusicLoader.Core), "MusicLoader", "1.1.0", "Headshotnoby/headshot2017", null)]
[assembly: MelonGame("Jonathan Miller aka Zethros", "SMBZ-G")]

namespace MusicLoader
{
    public class Core : MelonMod
    {
        public static BattleCache.MusicEnum[] originalMusicArray;
        public static List<CustomMusicEntry> customMusic = new List<CustomMusicEntry>();

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }

        public override void OnLateInitializeMelon()
        {
            originalMusicArray = BattleCache.MusicArray;

            GameObject obj = new GameObject("MusicLoader");
            GameObject.DontDestroyOnLoad(obj);
            obj.AddComponent<MusicLoaderComponent>();
        }

        [HarmonyPatch(typeof(BattleCache), "Song_GetDisplayName", new Type[] { typeof(BattleCache.MusicEnum) })]
        private static class DisplayNamePatch
        {
            private static bool Prefix(ref string __result, BattleCache.MusicEnum song)
            {
                foreach (CustomMusicEntry mus in customMusic)
                {
                    if (mus.id != song) continue;
                    __result = mus.music.name;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(BattleCache), "Music_GetData", new Type[] { typeof(BattleCache.MusicEnum) })]
        private static class GetDataPatch
        {
            private static bool Prefix(ref BattleMusicData __result, BattleCache.MusicEnum song)
            {
                foreach (CustomMusicEntry mus in customMusic)
                {
                    if (mus.id != song) continue;
                    __result = mus.music;
                    return false;
                }

                return true;
            }
        }
    }
}