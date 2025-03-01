// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Copilot;
using Microsoft.CodeAnalysis.CSharp.UseNameofInAttribute;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseNameofInAttributeDiagnosticAnalyzer,
    CSharpCopilotNotImplementedMethodFixProvider>;

public class CSharpCopilotNotImplementedMethodFixProviderTests
{
    [Fact]
    public async Task TestNotImplementedMethodFix_WhenQuotaExceeded()
    {
        var mockOptionsService = new Mock<ICopilotOptionsService>(MockBehavior.Strict);
        mockOptionsService
            .Setup(service => service.IsImplementNotImplementedExceptionEnabledAsync())
            .ReturnsAsync(true);

        var mockCopilotService = new Mock<ICopilotCodeAnalysisService>(MockBehavior.Strict);
        mockCopilotService
            .Setup(service => service.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockCopilotService
            .Setup(service => service.ImplementNotImplementedMethodAsync(
                It.IsAny<Document>(),
                It.IsAny<TextSpan?>(),
                It.IsAny<SyntaxNode>(),
                It.IsAny<ISymbol>(),
                It.IsAny<SemanticModel>(),
                It.IsAny<ImmutableArray<ReferencedSymbol>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<(Dictionary<string, string>?, bool)>((null, true)));

        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    [|throw new NotImplementedException();|]
                }
            }
            """;

        var fixedCode = """
            using System;

            class C
            {
                void M()
                {
                    /* Error: Copilot not available. */
                    throw new NotImplementedException();
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ExpectedDiagnostics =
            {
                VerifyCS.Diagnostic().WithSpan(7, 9, 7, 45)
            },
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }
}
