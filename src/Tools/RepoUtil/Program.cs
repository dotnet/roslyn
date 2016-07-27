using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal static class Program
    {
        private enum Mode
        {
            Usage,
            Verify,
            Consumes,
        }

        internal static readonly string[] ProjectJsonFileRelativeNames = Array.Empty<string>();

        internal static int Main(string[] args)
        {
            return Run(args) ? 0 : 1;
        }

        private static bool Run(string[] args)
        { 
            string sourcesPath;
            Mode mode;
            if (!TryParseCommandLine(args, out mode, out sourcesPath))
            {
                return false;
            }

            var repoData = RepoData.ReadFrom(Path.Combine(AppContext.BaseDirectory, "RepoData.json"));
            switch (mode)
            {
                case Mode.Usage:
                    Usage();
                    return true;
                case Mode.Verify:
                    return VerifyUtil.Go(sourcesPath, repoData);
                case Mode.Consumes:
                    return Consumes(sourcesPath);
                default:
                    throw new Exception("Unrecognized mode");
            }
        }

        private static bool Consumes(string sourcesPath)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<string>();
            var all = Data.FloatingList.Concat(Data.StaticList).Select(x => new FileName(sourcesPath, x));
            foreach (var fileName in all)
            {
                foreach (var nugetRef in ProjectJsonUtil.GetDependencies(fileName.FullPath))
                {
                    var key = $@"""{nugetRef.Name}"" : ""{nugetRef.Version}""";
                    if (set.Add(key))
                    {
                        list.Add(key);
                    }
                }
            }

            list.Sort();
            foreach (var key in list)
            {
                Console.WriteLine(key);
            }

            return true;
        }

        private static void Usage()
        {
            var text = @"
  -verify: check the state of the repo
  -consumes: output the conent consumed by this repo
";
            Console.Write(text);
        }

        private static bool TryParseCommandLine(string[] args, out Mode mode, out string sourcesPath)
        {
            var allGood = true;
            var binariesPath = Path.GetDirectoryName(Path.GetDirectoryName(AppContext.BaseDirectory));
            sourcesPath = Path.GetDirectoryName(binariesPath);
            mode = Mode.Usage;

            var index = 0;
            while (index < args.Length)
            {
                var arg = args[index];
                switch (arg.ToLower())
                {
                    case "-verify":
                        mode = Mode.Verify;
                        index++;
                        break;
                    case "-consumes":
                        mode = Mode.Consumes;
                        index++;
                        break;
                    default:
                        Console.Write($"Option {arg} is unrecognized");
                        allGood = false;
                        index++;
                        break;
                }
            }

            return allGood;
        }
    }
}
