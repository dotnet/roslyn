// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace BuildValidator
{
    internal class Options
    {
        private Options()
        { }

        public bool ConsoleOutput { get; private set; } = true;
        public bool Verbose { get; private set; }
        public bool IgnoreCompilerVersion { get; private set; }

        public static Options Create(string[] args)
        {
            var options = new Options();

            for (var i = 0; i < args.Length;)
            {
                var arg = args[i++];

                switch (arg)
                {
                    case "/verbose":
                        options.Verbose = true;
                        break;

                    case "/quiet":
                        options.ConsoleOutput = false;
                        break;

                    case "/ignorecompilerversion":
                        options.IgnoreCompilerVersion = true;
                        break;
                }
            }

            return options;
        }
    }
}
