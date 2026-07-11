# MusicLoader

Custom music loader for SMBZ-G.

Requires [MelonLoader](https://melonwiki.xyz/#/?id=requirements) and [SMBZModsMenu](https://github.com/headshot2017/smbzg-modsmenu)

## Custom music folder structure

Music goes in `SMBZ-G_Data/StreamingAssets/Music`

There are two ways you can add music:
* Place the music audio file directly in the Music folder
* Create a folder in the Music folder, and inside it, place "start" and "loop" audio files

```
└── [SMBZ-G_Data]
    └── [StreamingAssets]
        └── [Music]
            ├── [Music folder]
            │   ├── "loop" file from any supported audio format
            │   └── "start" file from any supported audio format
            └── Music file from any supported audio format
```

## Supported formats

* OGG
* MP3
* WAV
* Tracker music (.it, .s3m, .mod, .xm)
