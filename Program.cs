using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CpkTmxTool
{
    class Program
    {
        private static int Search(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        private static string getName(byte[] tmx)
        {
            int end = Search(tmx, new byte[] { 0x00 });
            byte[] name = tmx[0..end];
            return Encoding.ASCII.GetString(name);
        }

        public static Dictionary<string, int> getTmxNames(string cpk)
        {
            Dictionary<string, int> tmxNames = new Dictionary<string, int>();
            byte[] cpkBytes = File.ReadAllBytes(cpk);
            byte[] pattern = Encoding.ASCII.GetBytes("TMX0");
            int offset = 0;
            int found = 0;
            while (found != -1)
            {
                // Start search after "TMX0"
                found = Search(cpkBytes[offset..cpkBytes.Length], pattern);
                offset = found + offset + 4;
                if (found != -1)
                {
                    string tmxName = getName(cpkBytes[(offset - 268)..cpkBytes.Length]);
                    int index = 2;
                    while (tmxNames.ContainsKey(tmxName))
                    {
                        tmxName = $"{Path.GetFileNameWithoutExtension(tmxName)}({index}).tmx";
                        index += 1;
                    }
                    tmxNames.Add(tmxName, offset - 12);
                }
            }
            return tmxNames;
        }


        private static int findTmx(string cpk, string tmxName)
        {
            // Get all tmx names instead to prevent replacing similar names
            if (File.Exists(cpk))
            {
                Dictionary<string, int> tmxNames = getTmxNames(cpk);
                if (tmxNames.ContainsKey(tmxName))
                    return tmxNames[tmxName];
            }
            return -1;
        }

        private static string outputCpk;

        public static void replaceTmx(string cpk, string tmx, bool overwrite)
        {
            string tmxPattern = Path.GetFileName(tmx);
            int offset = findTmx(cpk, tmxPattern);
            string outputPath = cpk;
            if (!overwrite)
            {
                if (Path.GetDirectoryName(cpk) != null && Path.GetDirectoryName(cpk) != "")
                    outputPath = $@"{Path.GetDirectoryName(cpk)}\NEW_{Path.GetFileName(cpk)}";
                else
                    outputPath = $@"NEW_{Path.GetFileName(cpk)}";
            }
            if (OutputFilePath != null)
            {
                if (Path.GetExtension(OutputFilePath).ToLower() != ".cpk")
                {
                    outputPath = $@"{OutputFilePath}\{Path.GetFileName(cpk)}";
                    Directory.CreateDirectory(OutputFilePath);
                }
                else
                    outputPath = OutputFilePath;
            }
            if (offset > -1)
            {
                outputCpk = outputPath;
                byte[] tmxBytes = File.ReadAllBytes(tmx);
                int repTmxLen = tmxBytes.Length;
                int ogTmxLen = BitConverter.ToInt32(File.ReadAllBytes(cpk), offset + 4);
                byte[] cpkBytes = File.ReadAllBytes(cpk);
                byte[] newcpk = new byte[cpkBytes.Length + (repTmxLen - ogTmxLen)];
                cpkBytes[0..offset].CopyTo(newcpk, 0);
                tmxBytes.CopyTo(newcpk, offset);
                cpkBytes[(offset + ogTmxLen)..cpkBytes.Length].CopyTo(newcpk, offset + repTmxLen);
                BitConverter.GetBytes(repTmxLen).CopyTo(newcpk, offset - 4);
                File.WriteAllBytes(outputPath, newcpk);
                Console.WriteLine($"Replaced {tmx} in {cpk}");
            }
            else
                Console.WriteLine($"[Error] {tmx} not found in {cpk}");
        }


        public static byte[] extractTmx(string cpk, string tmx)
        {
            string tmxPattern = Path.GetFileName(tmx);
            int offset = findTmx(cpk, tmxPattern);
            if (offset > -1)
            {
                byte[] cpkBytes = File.ReadAllBytes(cpk);
                int tmxLen = BitConverter.ToInt32(cpkBytes, offset + 4);
                return cpkBytes[offset..(offset + tmxLen)];
            }
            return null;
        }

        private static void extractAll(string cpkFile)
        {
            // Extract all
            byte[] cpkBytes = File.ReadAllBytes(cpkFile);
            if (Path.GetExtension(getName(cpkBytes)).ToLower() != ".cin")
            {
                Console.WriteLine("[Error] Invalid .cpk file.");
                return;
            }
            string outputFolder = Path.ChangeExtension(cpkFile, null);
            if (OutputFilePath != null)
            {
                Directory.CreateDirectory(OutputFilePath);
                outputFolder = $@"{OutputFilePath}\{Path.GetFileName(outputFolder)}";
            }
            Directory.CreateDirectory(outputFolder);
            Dictionary<string, int> tmxNames = getTmxNames(cpkFile);
            foreach (string name in tmxNames.Keys)
            {
                byte[] tmx = extractTmx(cpkFile, name);
                File.WriteAllBytes($@"{outputFolder}\{name}", tmx);
                Console.WriteLine($"Extracted {name} from {cpkFile}");
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("[Error] No arguments specified");
                DisplayUsage();
                return;
            }
            if (!TryParseArguments(args))
                return;

            if (Extract)
            {
                FileAttributes attr = File.GetAttributes(InputFilePath);
                if (!attr.HasFlag(FileAttributes.Directory))
                {
                    if (Path.GetExtension(InputFilePath).ToLower() != ".cpk")
                    {
                        Console.WriteLine("[Error] Input file doesn't have .cpk extension");
                        return;
                    }
                    extractAll(InputFilePath);
                }
                else
                {
                    foreach (var cpkFile in Directory.EnumerateFiles(InputFilePath, "*.cpk", SearchOption.AllDirectories))
                    {
                        Console.WriteLine($"Extracting {cpkFile}");
                        extractAll(cpkFile);
                    }
                }
            }

            if (Replace)
            {
                FileAttributes attr = File.GetAttributes(InputFilePath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    Console.WriteLine("[Error] Input file for replacing is a folder instead of .cpk file");
                    return;
                }
                attr = File.GetAttributes(ReplaceFilePath);
                if (!attr.HasFlag(FileAttributes.Directory))
                {
                    if (Path.GetExtension(ReplaceFilePath).ToLower() != ".tmx")
                    {
                        Console.WriteLine("[Error] Replacement file doesn't have .tmx extension");
                        return;
                    }
                    replaceTmx(InputFilePath, ReplaceFilePath, false);
                }
                else
                {
                    int counter = 0;
                    foreach (var tmxFile in Directory.EnumerateFiles(ReplaceFilePath, "*.tmx", SearchOption.AllDirectories))
                    {
                        if (counter == 0)
                            replaceTmx(InputFilePath, tmxFile, false);
                        else if (outputCpk != null)
                            replaceTmx(outputCpk, tmxFile, true);
                        counter += 1;
                    }
                }
                

            }
        }

        private static string InputFilePath;
        private static string OutputFilePath;
        private static string ReplaceFilePath;
        private static bool IsActionAssigned;
        private static bool Extract;
        private static bool Replace;

        private static bool TryParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                bool isLast = i + 1 == args.Length;

                switch (args[i])
                {
                    // General
                    case "-Replace":
                        if (isLast)
                        {
                            Console.WriteLine("[Error] Missing argument for -Replace parameter");
                            return false;
                        }
                        if (!IsActionAssigned)
                            IsActionAssigned = true;
                        else
                        {
                            Console.WriteLine("[Error] Can only assign 1 action");
                            return false;
                        }
                        ReplaceFilePath = args[++i];
                        Replace = true;
                        break;

                    case "-Extract":
                        if (!IsActionAssigned)
                            IsActionAssigned = true;
                        else
                        {
                            Console.WriteLine("[Error] Can only assign 1 action");
                            return false;
                        }
                        Extract = true;
                        break;

                    case "-Out":
                        if (isLast)
                        {
                            Console.WriteLine("[Error] Missing argument for -Out parameter");
                            return false;
                        }

                        OutputFilePath = args[++i];
                        break;

                    case "-In":
                        if (isLast)
                        {
                            Console.WriteLine("[Error] Missing argument for -In parameter");
                            return false;
                        }

                        InputFilePath = args[++i];
                        break;

                }
            }

            if (InputFilePath == null)
            {
                Console.WriteLine($"[Error] No input file path declared");
                return false;
            }

            if (!File.Exists(InputFilePath) && !Directory.Exists(InputFilePath))
            {
                Console.WriteLine($"[Error] Specified input file doesn't exist! ({InputFilePath})");
                return false;
            }

            if (Replace && !File.Exists(ReplaceFilePath) && !Directory.Exists(ReplaceFilePath))
            {
                Console.WriteLine($"[Error] Specified replacement file doesn't exist! ({ReplaceFilePath})");
                return false;
            }

            if (!IsActionAssigned)
            {
                Console.WriteLine($"[Error] No action assigned");
                return false;
            }

            return true;
        }

        private static void DisplayUsage()
        {
            Console.WriteLine($"CpkTmxTool by Tekka (2021)");
            Console.WriteLine();
            Console.WriteLine("This tool is for extracting/replacing tmx files in the cpk files in Persona 3 FES");
            Console.WriteLine();
            Console.WriteLine("Parameter overview:");
            Console.WriteLine("     -In       [Required]     <path to file/folder>      Provides the input for extracting/replacing. Only takes in");
            Console.WriteLine("                                                         file path if replacing. If a folder path is specified for");
            Console.WriteLine("                                                         extracting, all .cpk files within will be extracted.");
            Console.WriteLine("     -Out      [Optional]     <path to file/folder>      Provides the output for extracting/replacing. Only takes in");
            Console.WriteLine("                                                         folder path if extracting.");
            Console.WriteLine("     -Extract  [Action]                                  Extracts tmx's from the cpk file(s) specified from -In.");
            Console.WriteLine("     -Replace  [Action]       <path to file/folder>      Replaces the tmx file/files in folder in the cpk file");
            Console.WriteLine("                                                         specified from -In.");
        }
    }
}
