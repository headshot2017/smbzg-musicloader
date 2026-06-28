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

    private static readonly Dictionary<string, FormatInfo> formats = new()
    {
        {"wav", new FormatInfo(AudioType.WAV, true) },
        {"ogg", new FormatInfo(AudioType.OGGVORBIS, true) },
        {"mp3", new FormatInfo(AudioType.MPEG, true) },
        {"it",  new FormatInfo(AudioType.IT, false) },
        {"s3m", new FormatInfo(AudioType.S3M, false) },
        {"xm",  new FormatInfo(AudioType.XM, false) },
        {"mod", new FormatInfo(AudioType.MOD, false) },
    };

    Dictionary<string, Coroutine> musicCoros;


    void Start()
    {
        musicCoros = [];
        StartCoroutine(Load());
    }

    IEnumerator LoadMusicFolder(string musicPath)
    {
        string musicName = Path.GetFileName(musicPath);

        UnityWebRequest www = null;
        DownloadHandlerAudioClip handler = null;

        BattleMusicData data = ScriptableObject.CreateInstance<BattleMusicData>();
        data.name = musicName;

        foreach (var fmt in formats)
        {
            if (!data.StartupBackgroundMusic && File.Exists($"{musicPath}/start.{fmt.Key}"))
            {
                www = UnityWebRequestMultimedia.GetAudioClip($"file:///{musicPath}/start.{fmt.Key}", fmt.Value.type);
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
                        MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Failed to load \"{musicPath}/start.{fmt.Key}\". result={www.result}");
                        Debug.Log($"MusicLoader: Failed to load \"{musicPath}/start.{fmt.Key}\". result={www.result}");
                    }
                }
            }

            if (!data.BackgroundMusic && File.Exists($"{musicPath}/loop.{fmt.Key}"))
            {
                www = UnityWebRequestMultimedia.GetAudioClip($"file:///{musicPath}/loop.{fmt.Key}", fmt.Value.type);
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
                        MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Failed to load \"{musicPath}/loop.{fmt.Key}\". result={www.result}");
                        Debug.Log($"MusicLoader: Failed to load \"{musicPath}/loop.{fmt.Key}\". result={www.result}");
                    }
                }
            }
        }

        if (!data.BackgroundMusic)
            yield break;

        CustomMusicEntry entry = new CustomMusicEntry();
        entry.music = data;
        entry.id = (BattleCache.MusicEnum)(1000 + MusicLoader.Core.customMusic.Count);

        MusicLoader.Core.customMusic.Add(entry);

        while (!musicCoros.ContainsKey(musicPath))
            yield return null;
        musicCoros.Remove(musicPath);

        MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Added music \"{musicName}\"");
        Debug.Log($"MusicLoader: Added music \"{musicName}\"");
    }

    IEnumerator LoadMusicFile(string musicPath)
    {
        string musicName = Path.GetFileNameWithoutExtension(musicPath);
        string musicExt = Path.GetExtension(musicPath).Substring(1); // remove dot

        UnityWebRequest www = null;
        DownloadHandlerAudioClip handler = null;
        bool allowStream = false;

        if (formats.ContainsKey(musicExt))
        {
            FormatInfo fmt = formats[musicExt];
            www = UnityWebRequestMultimedia.GetAudioClip($"file:///{musicPath}", fmt.type);
            handler = new DownloadHandlerAudioClip(string.Empty, fmt.type);
            allowStream = fmt.stream;
        }

        if (www == null)
        {
            MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Failed to load \"{musicName}.{musicExt}\". www == null");
            Debug.Log($"MusicLoader: Failed to load \"{musicName}.{musicExt}\". www == null");

            while (!musicCoros.ContainsKey(musicPath))
                yield return null;
            musicCoros.Remove(musicPath);

            yield break;
        }

        handler.streamAudio = allowStream;
        www.downloadHandler = handler;
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Failed to load \"{musicName}.{musicExt}\". result={www.result}");
            Debug.Log($"MusicLoader: Failed to load \"{musicName}.{musicExt}\". result={www.result}");

            while (!musicCoros.ContainsKey(musicPath))
                yield return null;
            musicCoros.Remove(musicPath);

            yield break;
        }

        BattleMusicData data = ScriptableObject.CreateInstance<BattleMusicData>();
        data.name = musicName;
        data.StartupBackgroundMusic = null;
        data.BackgroundMusic = handler.audioClip;

        CustomMusicEntry entry = new CustomMusicEntry();
        entry.music = data;

        MusicLoader.Core.customMusic.Add(entry);

        while (!musicCoros.ContainsKey(musicPath))
            yield return null;
        musicCoros.Remove(musicPath);

        MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Added music \"{musicName}\"");
        Debug.Log($"MusicLoader: Added music \"{musicName}\"");
    }

    IEnumerator Load()
    {
        string path = $"{Application.streamingAssetsPath}/Music".Replace('\\', '/');
        if (!Directory.Exists($"{Application.streamingAssetsPath}/Music"))
            Directory.CreateDirectory($"{Application.streamingAssetsPath}/Music");

        MusicLoader.Core.customMusic.Clear();
        BattleCache.MusicArray = MusicLoader.Core.originalMusicArray;

        // get StreamingAssets/Music/Music Name/(loop and-or start)
        foreach (string _musicName in Directory.GetDirectories(path))
        {
            string musicPath = _musicName.Replace('\\', '/');
            musicCoros[musicPath] = StartCoroutine(LoadMusicFolder(musicPath));
        }

        // get StreamingAssets/Music/Music Name
        foreach (string _musicName in Directory.GetFiles(path))
        {
            string musicPath = _musicName.Replace('\\', '/');
            musicCoros[musicPath] = StartCoroutine(LoadMusicFile(musicPath));
        }

        while (musicCoros.Count > 0)
            yield return null;

        MusicLoader.Core.customMusic.Sort((s1, s2) => s1.music.name.CompareTo(s2.music.name));
        for (int i=0; i<MusicLoader.Core.customMusic.Count; i++)
        {
            CustomMusicEntry music = MusicLoader.Core.customMusic[i];
            music.id = (BattleCache.MusicEnum)(1000 + i);
        }

        List<BattleCache.MusicEnum> FinalArray = MusicLoader.Core.originalMusicArray.ToList();
        foreach (CustomMusicEntry entry in MusicLoader.Core.customMusic)
            FinalArray.Add(entry.id);
        BattleCache.MusicArray = FinalArray.ToArray();

        MelonLoader.Melon<MusicLoader.Core>.Logger.Msg($"Loading finished with {MusicLoader.Core.customMusic.Count} custom tracks");
        Debug.Log($"MusicLoader: Loading finished with {MusicLoader.Core.customMusic.Count} custom tracks");

        Destroy(gameObject);
        yield return null;
    }
}
