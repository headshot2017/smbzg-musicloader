using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MusicLoaderComponent : MonoBehaviour
{
    class FormatInfo
    {
        public FormatInfo(AudioType _t, bool _s)
        {
            type = _t;
            stream = _s;
        }
        public AudioType type;
        public bool stream;
    }

    void Start()
    {
        StartCoroutine(Load());
    }

    IEnumerator Load()
    {
        string path = $"{Application.streamingAssetsPath}/Music".Replace('\\', '/');
        if (!Directory.Exists($"{Application.streamingAssetsPath}/Music"))
            Directory.CreateDirectory($"{Application.streamingAssetsPath}/Music");

        Dictionary<string, FormatInfo> formats = new Dictionary<string, FormatInfo>()
        {
            {"wav", new FormatInfo(AudioType.WAV, true) },
            {"ogg", new FormatInfo(AudioType.OGGVORBIS, true) },
            {"mp3", new FormatInfo(AudioType.MPEG, true) },
            {"it",  new FormatInfo(AudioType.IT, false) },
            {"s3m", new FormatInfo(AudioType.S3M, false) },
            {"xm",  new FormatInfo(AudioType.XM, false) },
            {"mod", new FormatInfo(AudioType.MOD, false) },
        };

        MusicLoader.Core.customMusic.Clear();
        BattleCache.MusicArray = MusicLoader.Core.originalMusicArray;

        // get StreamingAssets/Music/Music Name/(loop and-or start)
        foreach (string _musicName in Directory.GetDirectories(path))
        {
            string musicPath = _musicName.Replace('\\', '/');
            string musicName = Path.GetFileName(musicPath);

            UnityWebRequest www = null;
            DownloadHandlerAudioClip handler = null;

            BattleMusicData data = ScriptableObject.CreateInstance<BattleMusicData>();
            data.name = musicName;

            foreach (var fmt in formats)
            {
                if (!data.StartupBackgroundMusic && File.Exists($"{path}/{musicName}/start.{fmt.Key}"))
                {
                    www = UnityWebRequestMultimedia.GetAudioClip($"file:///{path}/{musicName}/start.{fmt.Key}", fmt.Value.type);
                    handler = new DownloadHandlerAudioClip(string.Empty, fmt.Value.type);

                    if (www != null)
                    {
                        handler.streamAudio = fmt.Value.stream;
                        www.downloadHandler = handler;
                        yield return www.SendWebRequest();
                        if (www.result == UnityWebRequest.Result.Success)
                            data.StartupBackgroundMusic = handler.audioClip;
                        else
                        {
                            MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Failed to load \"{musicName}/start.{fmt.Key}\". result={www.result}");
                            Debug.Log($"MusicLoader: Failed to load \"{musicName}/start.{fmt.Key}\". result={www.result}");
                        }
                    }
                }

                if (!data.BackgroundMusic && File.Exists($"{path}/{musicName}/loop.{fmt.Key}"))
                {
                    www = UnityWebRequestMultimedia.GetAudioClip($"file:///{path}/{musicName}/loop.{fmt.Key}", fmt.Value.type);
                    handler = new DownloadHandlerAudioClip(string.Empty, fmt.Value.type);

                    if (www != null)
                    {
                        handler.streamAudio = fmt.Value.stream;
                        www.downloadHandler = handler;
                        yield return www.SendWebRequest();
                        if (www.result == UnityWebRequest.Result.Success)
                            data.BackgroundMusic = handler.audioClip;
                        else
                        {
                            MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Failed to load \"{musicName}/loop.{fmt.Key}\". result={www.result}");
                            Debug.Log($"MusicLoader: Failed to load \"{musicName}/loop.{fmt.Key}\". result={www.result}");
                        }
                    }
                }
            }

            if (!data.BackgroundMusic) continue;

            CustomMusicEntry entry = new CustomMusicEntry();
            entry.music = data;
            entry.id = (BattleCache.MusicEnum)(1000 + MusicLoader.Core.customMusic.Count);

            MusicLoader.Core.customMusic.Add(entry);
            MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Added music \"{musicName}\"");
            Debug.Log($"MusicLoader: Added music \"{musicName}\"");
        }

        // get StreamingAssets/Music/Music Name
        foreach (string _musicName in Directory.GetFiles(path))
        {
            string musicPath = _musicName.Replace('\\', '/');
            string musicName = Path.GetFileNameWithoutExtension(musicPath);

            UnityWebRequest www = null;
            DownloadHandlerAudioClip handler = null;
            bool allowStream = false;

            foreach (var fmt in formats)
            {
                if (File.Exists($"{path}/{musicName}.{fmt.Key}"))
                {
                    www = UnityWebRequestMultimedia.GetAudioClip($"file:///{path}/{musicName}.{fmt.Key}", fmt.Value.type);
                    handler = new DownloadHandlerAudioClip(string.Empty, fmt.Value.type);
                    allowStream = fmt.Value.stream;
                    break;
                }
            }

            if (www != null)
            {
                handler.streamAudio = allowStream;
                www.downloadHandler = handler;
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Failed to load \"{musicName}\". result={www.result}");
                    Debug.Log($"MusicLoader: Failed to load \"{musicName}\". result={www.result}");
                    continue;
                }

                BattleMusicData data = ScriptableObject.CreateInstance<BattleMusicData>();
                data.name = musicName;
                data.StartupBackgroundMusic = null;
                data.BackgroundMusic = handler.audioClip;

                CustomMusicEntry entry = new CustomMusicEntry();
                entry.music = data;
                entry.id = (BattleCache.MusicEnum)(1000 + MusicLoader.Core.customMusic.Count);

                MusicLoader.Core.customMusic.Add(entry);
                MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Added music \"{musicName}\"");
                Debug.Log($"MusicLoader: Added music \"{musicName}\"");
            }
        }

        List<BattleCache.MusicEnum> FinalArray = MusicLoader.Core.originalMusicArray.ToList();
        foreach (CustomMusicEntry entry in MusicLoader.Core.customMusic)
            FinalArray.Add(entry.id);
        BattleCache.MusicArray = FinalArray.ToArray();

        MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Loading finished with {MusicLoader.Core.customMusic.Count} custom music");
        Debug.Log($"MusicLoader: Loading finished with {MusicLoader.Core.customMusic.Count} custom music");

        Destroy(gameObject);
    }
}
