// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Serializable]
    public class EmitException : Exception
    {
        public IEnumerable<Diagnostic> Diagnostics { get; }

        protected EmitException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public EmitException(IEnumerable<Diagnostic> diagnostics, string directory)
            : base(GetMessageFromResult(diagnostics, directory))
        {
            this.Diagnostics = diagnostics;
        }

        private static string GetMessageFromResult(IEnumerable<Diagnostic> diagnostics, string directory)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Emit Failed, binaries saved to: ");
            sb.AppendLine(directory);
            foreach (var d in diagnostics)
            {
                sb.AppendLine(d.ToString());
            }
            return sb.ToString();
        }
    }

    [Serializable]
    public class PeVerifyException : Exception
    {
        protected PeVerifyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public PeVerifyException(string output, string exePath) : base(GetMessageFromResult(output, exePath)) { }

        private static string GetMessageFromResult(string output, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("PeVerify failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("':");
            sb.AppendLine(output);
            return sb.ToString();
        }
    }

    [Serializable]
    public class ExecutionException : Exception
    {
        public ExecutionException(string expectedOutput, string actualOutput, string exePath) : base(GetMessageFromResult(expectedOutput, actualOutput, exePath)) { }

        public ExecutionException(Exception innerException, string exePath) : base(GetMessageFromException(innerException, exePath), innerException) { }

        private static string GetMessageFromException(Exception executionException, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Execution failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("'.");
            sb.Append("Exception: " + executionException);
            return sb.ToString();
        }

        private static string GetMessageFromResult(string expectedOutput, string actualOutput, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Execution failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("'.");
            if (expectedOutput != null)
            {
                sb.Append("Expected: ");
                sb.AppendLine(expectedOutput);
                sb.Append("Actual:   ");
                sb.AppendLine(actualOutput);
            }
            else
            {
                sb.Append("Output: ");
                sb.AppendLine(actualOutput);
            }
            return sb.ToString();
        }
    }
}
