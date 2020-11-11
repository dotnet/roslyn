// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
