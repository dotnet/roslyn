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

            switch (mode)
            {
                case Mode.Usage:
                    Usage();
                    return true;
                case Mode.Verify:
                    return Verify(sourcesPath);
                case Mode.Consumes:
                    return Consumes(sourcesPath);
                default:
                    throw new Exception("Unrecognized mode");
            }
        }

        /// <summary>
        /// Verify our repo is consistent and this tool has enough information to make changes to the state.
        ///
        /// TOOD: should this run before every command?
        /// </summary>
        private static bool Verify(string sourcesPath)
        {
            var fileNames = Data
                .StaticList
                .Concat(Data.FloatingList)
                .Select(x => new FileName(sourcesPath, x));
            var allGood = false;
            foreach (var fileName in fileNames)
            {
                if (!File.Exists(fileName.FullPath))
                {
                    Console.WriteLine($"Project file {fileName} does not exist");
                    allGood = false;
                }
            }

            if (!allGood)
            {
                return false;
            }

            return ProjectJsonUtil.VerifyTracked(sourcesPath, fileNames);
        }

        private static bool Consumes(string sourcesPath)
        {
            var map = new Dictionary<string, NuGetReference>(StringComparer.Ordinal);
            foreach (var fileName in Data.GetFloatingFileNames(sourcesPath))
            {
                foreach (var nugetRef in ProjectJsonUtil.GetDependencies(fileName.FullPath))
                {
                    map[nugetRef.Name] = nugetRef;
                }
            }

            foreach (var nugetRef in map.Values.OrderBy(x => x.Name))
            {
                Console.WriteLine($@"""{nugetRef.Name}"" : ""{nugetRef.Version}""");
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
