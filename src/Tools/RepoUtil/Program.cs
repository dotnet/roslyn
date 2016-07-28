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
        private sealed class ParsedArgs
        {
            internal Mode Mode { get; set; } = Mode.Usage;
            internal string RepoDataPath { get; set; }
            internal string SourcesPath { get; set; }
            internal string[] RemainingArgs { get; set; }
        }

        private enum Mode
        {
            Usage,
            Verify,
            Consumes,
            Change,
            Produces,
        }

        internal static readonly string[] ProjectJsonFileRelativeNames = Array.Empty<string>();

        internal static int Main(string[] args)
        {
            return Run(args) ? 0 : 1;
        }

        private static bool Run(string[] args)
        {
            ParsedArgs parsedArgs;
            if (!TryParseCommandLine(args, out parsedArgs))
            {
                return false;
            }

            var repoConfig = RepoConfig.ReadFrom(parsedArgs.RepoDataPath);
            switch (parsedArgs.Mode)
            {
                case Mode.Usage:
                    Usage();
                    return true;
                case Mode.Verify:
                    return VerifyUtil.Go(repoConfig, parsedArgs.SourcesPath);
                case Mode.Consumes:
                    {
                        Console.WriteLine(ConsumesUtil.Go(repoConfig, parsedArgs.SourcesPath));
                        return true;
                    }
                case Mode.Change:
                    {
                        var repoData = RepoData.Create(repoConfig, parsedArgs.SourcesPath);
                        var util = new ChangeUtil(repoData);
                        return util.Run(Console.Out, parsedArgs.RemainingArgs);
                    }
                case Mode.Produces:
                    {
                        var util = new ProduceUtil(repoConfig, parsedArgs.SourcesPath);
                        util.Go();
                        return true;
                    }
                default:
                    throw new Exception("Unrecognized mode");
            }
        }

        // TODO: don't use dashes here.
        private static bool TryParseCommandLine(string[] args, out ParsedArgs parsedArgs)
        {
            parsedArgs = new ParsedArgs();

            // Setup the default values
            var binariesPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppContext.BaseDirectory)));
            parsedArgs.SourcesPath = Path.GetDirectoryName(binariesPath);
            parsedArgs.Mode = Mode.Usage;
            parsedArgs.RepoDataPath = Path.Combine(AppContext.BaseDirectory, "RepoData.json");

            var allGood = true;
            var done = false;
            var index = 0;
            while (index < args.Length && !done)
            {
                var arg = args[index];
                switch (arg.ToLower())
                {
                    case "-sourcesPath":
                        {
                            if (index + 1 < args.Length)
                            {
                                parsedArgs.SourcesPath = args[index + 1];
                                index += 2;
                            }
                            else
                            {
                                Console.WriteLine($"The -sourcesPath switch needs a value");
                                index++;
                                allGood = false;
                            }
                            break;
                        }
                    case "verify":
                        parsedArgs.Mode = Mode.Verify;
                        index++;
                        done = true;
                        break;
                    case "consumes":
                        parsedArgs.Mode = Mode.Consumes;
                        index++;
                        done = true;
                        break;
                    case "change":
                        parsedArgs.Mode = Mode.Change;
                        index++;
                        done = true;
                        break;
                    case "produces":
                        parsedArgs.Mode = Mode.Produces;
                        index++;
                        done = true;
                        break;
                    default:
                        Console.Write($"Option {arg} is unrecognized");
                        allGood = false;
                        index++;
                        break;
                }
            }

            parsedArgs.RemainingArgs = index >= args.Length
                ? Array.Empty<string>()
                : args.Skip(index).ToArray();

            return allGood;
        }

        private static void Usage()
        {
            var text = @"
  verify: check the state of the repo
  consumes: output the conent consumed by this repo
  produces: output the content produced by this repo
  change: change the dependencies.
";
            Console.Write(text);
        }
    }
}
