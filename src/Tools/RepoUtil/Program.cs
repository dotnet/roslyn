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
            Change,
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

            var repoConfig = RepoConfig.ReadFrom(Path.Combine(AppContext.BaseDirectory, "RepoData.json"));
            switch (mode)
            {
                case Mode.Usage:
                    Usage();
                    return true;
                case Mode.Verify:
                    return VerifyUtil.Go(repoConfig, sourcesPath);
                case Mode.Consumes:
                    {
                        Console.WriteLine(ConsumesUtil.Go(repoConfig, sourcesPath));
                        return true;
                    }
                case Mode.Change:
                    {
                        var repoData = RepoData.Create(repoConfig, sourcesPath);
                        var util = new ChangeUtil(repoData);
                        util.ChangeAll();
                        return true;
                    }
                default:
                    throw new Exception("Unrecognized mode");
            }
        }

        private static void Usage()
        {
            var text = @"
  -verify: check the state of the repo
  -consumes: output the conent consumed by this repo
  -change: change the dependencies.
";
            Console.Write(text);
        }

        private static bool TryParseCommandLine(string[] args, out Mode mode, out string sourcesPath)
        {
            var allGood = true;
            var binariesPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppContext.BaseDirectory)));
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
                    case "-change":
                        mode = Mode.Change;
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
