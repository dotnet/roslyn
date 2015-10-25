// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RunTests
{
    internal class Options
    {
        public bool UseHtml { get; set; }

        public bool Test64 { get; set; }

        public string Trait { get; set; }

        public string NoTrait { get; set; }

        public string[] Assemblies { get; set; }

        public string XunitPath { get; set; }

        internal static Options Parse(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return null;
            }

            var opt = new Options {XunitPath = args[0], UseHtml = true};

            int index = 1;
                    
            var comp = StringComparer.OrdinalIgnoreCase;
            while (index < args.Length)
            {
                var current = args[index];
                if (comp.Equals(current, "-test64"))
                {
                    opt.Test64 = true;
                    index++;
                }
                else if (comp.Equals(current, "-xml"))
                {
                    opt.UseHtml = false;
                    index++;
                }
                else if (current.StartsWith("-trait:", StringComparison.OrdinalIgnoreCase))
                {
                    opt.Trait = current.Substring(7);
                    index++;
                }
                else if (current.StartsWith("-notrait:", StringComparison.OrdinalIgnoreCase))
                {
                    opt.NoTrait = current.Substring(9);
                    index++;
                }
                else
                {
                    break;
                }
            }

            opt.XunitPath = opt.Test64
                ? Path.Combine(opt.XunitPath, "xunit.console.exe")
                : Path.Combine(opt.XunitPath, "xunit.console.x86.exe");


            opt.Assemblies = args.Skip(index).ToArray();

            if (opt.Assemblies.Length != 0) return opt;

            PrintUsage();
            return null;
        }        

        private static void PrintUsage()
        {
            Console.WriteLine("runtests [xunit-console-runner] [-test64] [-xml] [-trait:listOfTraisToInclude] [-notrait:listOfTraitsToExclude] [assembly1] [assembly2] [...]");
        }

    }

}