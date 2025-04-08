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
    public static Action<int, string, string> Create(string expectedOutput, bool trim = true) =>
        Create(0, expectedOutput, "", trim);

    public static Action<int, string, string> Create(int expectedExitCode, string expectedOutput, string expectedErrorOutput, bool trim) =>
        void (int exitCode, string output, string errorOutput) =>
        {
            Assert.Equal(0, exitCode);

            if (trim)
            {
                output = output.Trim();
                errorOutput = errorOutput.Trim();
                expectedOutput = expectedOutput.Trim();
                expectedErrorOutput = expectedErrorOutput.Trim();
            }

            Assert.Equal(expectedOutput, output);
            Assert.Equal(expectedErrorOutput, expectedErrorOutput);
        };

    public static Action<int, string, string>? TryCreate(int? expectedExitCode, string? expectedOutput, bool trim) =>
        TryCreate(expectedExitCode, expectedOutput, expectedErrorOutput: null, trim);

    public static Action<int, string, string>? TryCreate(int? expectedExitCode, string? expectedOutput, string? expectedErrorOutput, bool trim)
    {
        if (expectedExitCode is null && expectedOutput is null && expectedErrorOutput is null)
        {
            return null;
        }

        return Create(expectedExitCode ?? 0, expectedOutput ?? "", expectedErrorOutput ?? "", trim);
    }

}
