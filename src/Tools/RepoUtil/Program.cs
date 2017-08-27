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
            internal string RepoUtilDataPath { get; set; }
            internal string SourcesDirectory { get; set; }
            internal string GenerateDirectory { get; set; }
            internal string[] RemainingArgs { get; set; }
        }

        private delegate ICommand CreateCommand(RepoConfig repoConfig, string sourcesDir, string generateDir);

        internal static int Main(string[] args)
        {
            int result = 1;
            try
            {
                if (Run(args))
                {
                    result = 0;
                }
            }
            catch (ConflictingPackagesException ex)
            {
                Console.WriteLine(ex.Message);
                foreach (var package in ex.ConflictingPackages)
                {
                    Console.WriteLine(package.PackageName);
                    Console.WriteLine($"\t{package.Conflict.NuGetPackage.Version} - {package.Conflict.FileName}");
                    Console.WriteLine($"\t{package.Original.NuGetPackage.Version} - {package.Original.FileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something unexpected happened.");
                Console.WriteLine(ex.ToString());
            }

            return result;
        }

        private static bool Run(string[] args)
        {
            if (!TryParseCommandLine(args, out var parsedArgs, out var func))
            {
                Usage();
                return false;
            }

            var repoConfig = RepoConfig.ReadFrom(parsedArgs.RepoUtilDataPath);
            var command = func(repoConfig, parsedArgs.SourcesDirectory, parsedArgs.GenerateDirectory);
            if (command.Run(Console.Out, parsedArgs.RemainingArgs))
            {
                return true;
            }
            else
            {
                Console.WriteLine($"RepoUtil config read from: {parsedArgs.RepoUtilDataPath}");
                return false;
            }
        }

        private static bool TryParseCommandLine(string[] args, out ParsedArgs parsedArgs, out CreateCommand func)
        {
            func = null;
            parsedArgs = new ParsedArgs();

            // Setup the default values
            var binariesPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppContext.BaseDirectory))));

            var index = 0;
            if (!TryParseCommon(args, ref index, parsedArgs))
            {
                return false;
            }

            if (!TryParseCommand(args, ref index, out func))
            {
                return false;
            }

            parsedArgs.SourcesDirectory = parsedArgs.SourcesDirectory ?? GetDirectoryName(AppContext.BaseDirectory, 5);
            parsedArgs.GenerateDirectory = parsedArgs.GenerateDirectory ?? parsedArgs.SourcesDirectory;
            parsedArgs.RepoUtilDataPath = parsedArgs.RepoUtilDataPath ?? Path.Combine(parsedArgs.SourcesDirectory, @"build\config\RepoUtilData.json");
            parsedArgs.RemainingArgs = index >= args.Length
                ? Array.Empty<string>()
                : args.Skip(index).ToArray();
            return true;
        }

        private static string GetDirectoryName(string path, int depth)
        {
            for (var i = 0; i < depth; i++)
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }

        private static bool TryParseCommon(string[] args, ref int index, ParsedArgs parsedArgs)
        {
            while (index < args.Length)
            {
                var arg = args[index];
                if (arg[0] != '-')
                {
                    return true;
                }

                index++;
                switch (arg.ToLower())
                {
                    case "-sourcespath":
                        {
                            if (index < args.Length)
                            {
                                parsedArgs.SourcesDirectory = args[index];
                                index++;
                            }
                            else
                            {
                                Console.WriteLine($"The -sourcesPath switch needs a value");
                                return false;
                            }
                            break;
                        }
                    case "-generatepath":
                        {
                            if (index < args.Length)
                            {
                                parsedArgs.GenerateDirectory = args[index];
                                index++;
                            }
                            else
                            {
                                Console.WriteLine($"The -generatePath switch needs a value");
                                return false;
                            }
                            break;
                        }
                    case "-config":
                        {
                            if (index < args.Length)
                            {
                                parsedArgs.RepoUtilDataPath = args[index];
                                index++;
                            }
                            else
                            {
                                Console.WriteLine($"The -config switch needs a value");
                                return false;
                            }
                            break;
                        }
                    default:
                        Console.Write($"Option {arg} is unrecognized");
                        return false;
                }
            }

            return true;
        }

        private static bool TryParseCommand(string[] args, ref int index, out CreateCommand func)
        {
            func = null;

            if (index >= args.Length)
            {
                Console.WriteLine("Need a command to run");
                return false;
            }

            var name = args[index];
            switch (name)
            {
                case "view":
                    func = (c, s, g) => new ViewCommand(c, s);
                    break;
                case "consumes":
                    func = (c, s, g) => new ConsumesCommand(RepoData.Create(c, s, ignoreConflicts: false));
                    break;
                case "change":
                    func = (c, s, g) => new ChangeCommand(RepoData.Create(c, s, ignoreConflicts: true), g);
                    break;
                case "produces":
                    func = (c, s, g) => new ProducesCommand(c, s);
                    break;
                default:
                    Console.Write($"Command {name} is not recognized");
                    return false;
            }

            index++;
            return true;
        }

        private static void Usage()
        {
            Console.WriteLine("RepoUtil [-sourcesPath <sources path>] [-generatePath <generate path>] [-config <config path>] [verify|view|consumes|change|produces]");
        }
    }
}
