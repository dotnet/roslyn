// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Extensions on the <see cref="IVerifier"/> interface.
    /// </summary>
    public static class IVerifierExtensions
    {
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
                var diffBuilder = new InlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildDiffModel(expected, actual, ignoreWhitespace: false);
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(
                    string.IsNullOrEmpty(message)
                        ? "Actual and expected values differ. Expected shown in baseline of diff:"
                        : message);

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

                    messageBuilder.AppendLine(line.Text);
                }

                verifier.Fail(messageBuilder.ToString());
            }
        }
    }
}
