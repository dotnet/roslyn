using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    internal static class ExceptionHelper
    {
        internal static string GetMessageFromResult(IEnumerable<Diagnostic> diagnostics, string directory)
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

        internal static string GetMessageFromResult(string output, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("PeVerify failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("':");
            sb.AppendLine(output);
            return sb.ToString();
        }

        internal static string GetMessageFromException(Exception executionException, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Execution failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("'.");
            sb.Append("Exception: " + executionException);
            return sb.ToString();
        }

        internal static string GetMessageFromResult(string expectedOutput, string actualOutput, string exePath)
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
