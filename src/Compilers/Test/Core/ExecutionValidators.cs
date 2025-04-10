// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public static class ExecutionValidators
{
    [Flags]
    public enum NormalizeOutputFlags
    {
        None = 0b0000,

        /// <summary>
        /// Trim the output
        /// </summary>
        Trim = 0b0001,

        /// <summary>
        /// Normalize all spaces into a single space representation
        /// </summary>
        Spaces = 0b0010,

        All = Trim | Spaces
    }

    public static Action<int, string, string> Create(string expectedOutput, NormalizeOutputFlags flags) =>
        Create(0, expectedOutput, "", flags);

    public static Action<int, string, string> Create(string expectedOutput, bool trim = true) =>
        Create(0, expectedOutput, "", GetFlags(trim, spaces: false));

    public static Action<int, string, string> Create(int expectedExitCode, string expectedOutput, string expectedErrorOutput, NormalizeOutputFlags flags) =>
        void (int exitCode, string output, string errorOutput) =>
        {
            Assert.Equal(0, exitCode);

            if (flags != NormalizeOutputFlags.None)
            {
                output = Normalize(output, flags);
                errorOutput = Normalize(errorOutput, flags);
                expectedOutput = Normalize(expectedOutput, flags);
                expectedErrorOutput = Normalize(expectedErrorOutput, flags);
            }

            Assert.Equal(expectedOutput, output);
            Assert.Equal(expectedErrorOutput, expectedErrorOutput);
        };

    public static Action<int, string, string>? TryCreate(int? expectedExitCode, string? expectedOutput, bool trim) =>
        TryCreate(expectedExitCode, expectedOutput, expectedErrorOutput: null, GetFlags(trim, spaces: false));

    public static Action<int, string, string>? TryCreate(int? expectedExitCode, string? expectedOutput, string? expectedErrorOutput, NormalizeOutputFlags flags)
    {
        if (expectedExitCode is null && expectedOutput is null && expectedErrorOutput is null)
        {
            return null;
        }

        return Create(expectedExitCode ?? 0, expectedOutput ?? "", expectedErrorOutput ?? "", flags);
    }

    public static NormalizeOutputFlags GetFlags(bool trim, bool spaces)
    {
        var flags = NormalizeOutputFlags.None;
        if (trim)
        {
            flags |= NormalizeOutputFlags.Trim;
        }

        if (spaces)
        {
            flags |= NormalizeOutputFlags.Spaces;
        }

        return flags;
    }

    public static string Normalize(string str, NormalizeOutputFlags flags)
    {
        if (0 != (flags & NormalizeOutputFlags.Trim))
        {
            str = str.Trim();
        }

        if (0 != (flags & NormalizeOutputFlags.Spaces))
        {
            var builder = new StringBuilder(str.Length);
            foreach (var c in str)
            {
                if (char.IsWhiteSpace(c))
                {
                    builder.Append(' ');
                }
                else
                {
                    builder.Append(c);
                }
            }

            str = builder.ToString();
        }

        return str;
    }

}
