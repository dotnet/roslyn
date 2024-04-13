// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseInterpolatedString;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseInterpolatedString;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpUseInterpolatedStringCodeFixProvider>;

public sealed class CSharpUseInterpolatedStringTests
{
    [Fact]
    public async Task CantHandleMultipleFormatItems()
    {
        var initial =
            """
            using System.Threading.Tasks;

            class Program
            {
                private string FormattedString => [|string.Format("{0} + {1} - {2}", "first", "second")|];
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;

            class Program
            {
                private string FormattedString => [|string.Format("{0} + {1} - {2}", "first", "second")|];
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(initial, expected);
    }
}
