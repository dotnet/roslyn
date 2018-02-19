// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Encapsulates exit code and output/error streams of a process.
    /// </summary>
    public sealed class ProcessResult
    {
        public int ExitCode { get; }
        public string Output { get; }
        public string Errors { get; }

        public ProcessResult(int exitCode, string output, string errors)
        {
            ExitCode = exitCode;
            Output = output;
            Errors = errors;
        }

        public override string ToString()
        {
            return "EXIT CODE: " +
                   this.ExitCode +
                   Environment.NewLine +
                   "OUTPUT STREAM:" +
                   Environment.NewLine +
                   this.Output +
                   Environment.NewLine +
                   "ERRORS:" +
                   Environment.NewLine +
                   this.Errors;
        }

        public bool ContainsErrors
        {
            get { return this.ExitCode != 0 || !string.IsNullOrEmpty(this.Errors); }
        }
    }
}
