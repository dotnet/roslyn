using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            string binariesPath;
            string sourcePath;
            bool test;
            if (!ParseCommandLineArguments(args, out binariesPath, out sourcePath, out test))
            {
                Console.WriteLine("signroslyn.exe [-test] [-binariesPath <path>]");
                Environment.Exit(1);
            }

            var signTool = SignToolFactory.Create(AppContext.BaseDirectory, binariesPath, sourcePath, test);
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
            out string sourcePath,
            out bool test)
        {
            binariesPath = Path.GetDirectoryName(Path.GetDirectoryName(AppContext.BaseDirectory));
            sourcePath = null;
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
                    default:
                        Console.WriteLine($"Unrecognized option {current}");
                        return false;
                }
            }

            sourcePath = Path.GetDirectoryName(Path.GetDirectoryName(binariesPath));
            return true;
        }
    }
}
