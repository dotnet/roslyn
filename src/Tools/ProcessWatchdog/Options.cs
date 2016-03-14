// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CommandLine;

namespace ProcessWatchdog
{
    /// <summary>
    /// Command line options for the ProcessWatchdog tool.
    /// </summary>
    internal class Options
    {
        [Option(
            't',
            "timeout",
            HelpText = "Timeout value in the form hh:mm[:ss].",
            Required = true)]
        public string Timeout { get; set; }

        [Option(
            'c',
            "command-line",
            HelpText = "Command line for the executable to be run.",
            Required = true)]
        public string CommandLine { get; set; }
    }
}