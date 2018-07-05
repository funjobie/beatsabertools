using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Commons;

namespace BeatSaberSongGenerator.AudioProcessing
{
    /// <summary>
    /// Converts any audio supported by NAudio and converts to .ogg-format
    /// </summary>
    public class AudioToOggConverter
    {
        public void Convert(string inputFile, string outputFile)
        {
            string strCmdText;
            strCmdText = "/C lame.exe --decode <source> \"-\" | oggenc2.exe -q 5 \"-\" -o <destination>";
            strCmdText = strCmdText.Replace("<source>", "\"" + inputFile + "\"");
            strCmdText = strCmdText.Replace("<destination>", "\"" + outputFile + "\"");

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = strCmdText;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
    }
}
