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
            'e',
            "executable",
            HelpText = "The executable to run.",
            Required = true)]
        public string Executable { get; set; }

        [Option(
            'a',
            "arguments",
            HelpText = "Command line arguments to pass to the executable, enclosed in quotes if necessary.")]
        public string Arguments { get; set; }

        [Option(
            'o',
            "output-directory",
            HelpText = "Directory to which process dumps and screen shots will be written.",
            Default = ".")]
        public string OutputDirectory { get; set; }

        [Option(
            'p',
            "polling-interval",
            HelpText = "Polling interval in milliseconds",
            Default = 1000)]
        public int PollingInterval { get; set; }
    }
}