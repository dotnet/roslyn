// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignTool
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            string binariesPath;
            string settingsFile;
            string msbuildPath;
            bool test;

            if (!ParseCommandLineArguments(args, out binariesPath, out settingsFile, out msbuildPath, out test))
            {
                Console.WriteLine("SignTool.exe [-test] [-binariesPath <path>] [-settingsFile <path>] [-msbuildPath <path>]");
                Environment.Exit(1);
            }

            var signTool = SignToolFactory.Create(AppContext.BaseDirectory, binariesPath, settingsFile, msbuildPath, test);
            var batchData = ReadBatchSignInput(binariesPath);
            var util = new BatchSignUtil(signTool, batchData);
            util.Go();
        }

        internal static BatchSignInput ReadBatchSignInput(string rootBinaryPath)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "BatchSignData.json");
            using (var file = File.OpenText(filePath))
            {
                var serializer = new JsonSerializer();
                var fileJson = (Json.FileJson)serializer.Deserialize(file, typeof(Json.FileJson));
                var map = new Dictionary<string, SignInfo>();
                var allGood = true;
                foreach (var item in fileJson.SignList)
                {
                    var data = new SignInfo(certificate: item.Certificate, strongName: item.StrongName);
                    foreach (var name in item.FileList)
                    {
                        if (map.ContainsKey(name))
                        {
                            Console.WriteLine($"Duplicate file entry: {name}");
                            allGood = false;
                        }
                        else
                        {
                            map.Add(name, data);
                        }
                    }
                }

                if (!allGood)
                {
                    Environment.Exit(1);
                }

                return new BatchSignInput(rootBinaryPath, map, fileJson.ExcludeList);
            }
        }

        internal static bool ParseCommandLineArguments(
            string[] args,
            out string binariesPath,
            out string settingsFile,
            out string msbuildPath,
            out bool test)
        {
            binariesPath = Path.GetDirectoryName(Path.GetDirectoryName(AppContext.BaseDirectory));
            settingsFile = Path.Combine(Environment.CurrentDirectory, @"build\Targets\Settings.targets");
            msbuildPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"MSBuild\14.0\bin\MSBuild.exe");
            test = false;

            var i = 0;
            while (i < args.Length)
            {
                var current = args[i];
                switch (current.ToLower())
                {
                    case "-test":
                        test = true;
                        i++;
                        break;
                    case "-binariespath":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("-binariesPath needs an argument");
                            return false;
                        }

                        binariesPath = args[i + 1];
                        i += 2;
                        break;
                    case "-settingsfile":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("-settingsFile needs an argument");
                            return false;
                        }

                        settingsFile = args[i + 1];
                        i += 2;
                        break;
                    case "-msbuildpath":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("-msbuildPath needs an argument");
                            return false;
                        }

                        msbuildPath = args[i + 1];
                        i += 2;
                        break;
                    default:
                        Console.WriteLine($"Unrecognized option {current}");
                        return false;
                }
            }
            return true;
        }
    }
}
