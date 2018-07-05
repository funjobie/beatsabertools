# beatsabertools
Level generator for VR-game Beat Saber

**What this is NOT**

A replacement for handcrafted Beat Saber levels/charts

**What this is**

A tool for creating custom songs for Beat Saber with a barely tolerable quality for listening to music while moving your body.
You need to mod Beat Saber using this project: https://github.com/xyonico/BeatSaberSongLoader/releases

This tool comes "as-is" with no support or warrenty (see LICENSE). If something doesn't work, feel free to report it as an issue, but the chance that I will do something about it is slim as I'm working on other projects. If you need it fixed and are comfortable programming C#/.NET, you are welcome to fork this project and modify it however you like or submit pull requests :)

**How to use it**

- Start BeatSaberSongGenerator.exe
- Find the song file and cover image for the song you like to import (Only mp3 files are currently supported)
- Click "Generate"

alternatively you can create generate multiple levels at once.
- Find a conver image (all songs will get this image, you can replace it later if wanted)
- Click "Batch process..." button and select multiple audio files

The generated song and corresponding levels are stored in the same directory as the audio file within a directory of the same name. That directy must then be placed in the "CustomSongs" folder in the Beat Saber directory.

**Acknowledgements**

Like many other software projects I do depend on the great work of others. A special shout out to these knowledge sources and code projects:

- File format specifications by Reaxt: https://steamcommunity.com/sharedfiles/filedetails/?id=1377190061
- NWaves (signal processing library) by ar1st0crat: https://github.com/ar1st0crat/NWaves
- NAudio: https://github.com/naudio/NAudio
- Json.NET from NewtonSoft: https://www.newtonsoft.com/json
- TagLib# (https://github.com/mono/taglib-sharp)

Additionally this project utilized open source tools for the mp3 to ogg conversion, many thanks to the creators of:
lame (http://lame.sourceforge.net/) compiled version "lame3.100" by http://www.rarewares.org/mp3-lame-bundle.php#lame-current
oggenc (https://xiph.org/downloads/) compiled version "oggenc2.88-1.3.5-generic" by http://www.rarewares.org/ogg-oggenc.php