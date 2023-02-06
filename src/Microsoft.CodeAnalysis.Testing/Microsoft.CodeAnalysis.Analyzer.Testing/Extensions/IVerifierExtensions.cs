// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Extensions on the <see cref="IVerifier"/> interface.
    /// </summary>
    public static class IVerifierExtensions
    {
        private static readonly IChunker s_lineChunker = new LineChunker();
        private static readonly IChunker s_lineEndingsPreservingChunker = new LineEndingsPreservingChunker();
        private static readonly InlineDiffBuilder s_diffBuilder = new InlineDiffBuilder(new Differ());

        /// <summary>
        /// Asserts that two strings are equal, and prints a diff between the two if they are not.
        /// </summary>
        /// <param name="verifier">The verifier instance.</param>
        /// <param name="expected">The expected string. This is presented as the "baseline/before" side in the diff.</param>
        /// <param name="actual">The actual string. This is presented as the changed or "after" side in the diff.</param>
        /// <param name="message">The message to precede the diff, if the values are not equal.</param>
        public static void EqualOrDiff(this IVerifier verifier, string expected, string actual, string? message = null)
        {
            Requires.NotNull(verifier, nameof(verifier));

            if (expected != actual)
            {
                var diff = s_diffBuilder.BuildDiffModel(expected, actual, ignoreWhitespace: false, ignoreCase: false, s_lineChunker);
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(
                    string.IsNullOrEmpty(message)
                        ? "Actual and expected values differ. Expected shown in baseline of diff:"
                        : message);

                if (!diff.Lines.Any(line => line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted))
                {
                    // We have a failure only caused by line ending differences; recalculate with line endings visible
                    diff = s_diffBuilder.BuildDiffModel(expected, actual, ignoreWhitespace: false, ignoreCase: false, s_lineEndingsPreservingChunker);
                }

                foreach (var line in diff.Lines)
                {
                    switch (line.Type)
                    {
                        case ChangeType.Inserted:
                            messageBuilder.Append("+");
                            break;
                        case ChangeType.Deleted:
                            messageBuilder.Append("-");
                            break;
                        default:
                            messageBuilder.Append(" ");
                            break;
                    }

                    messageBuilder.AppendLine(line.Text.Replace("\r", "<CR>").Replace("\n", "<LF>"));
                }

                verifier.Fail(messageBuilder.ToString());
            }
        }
    }
}
