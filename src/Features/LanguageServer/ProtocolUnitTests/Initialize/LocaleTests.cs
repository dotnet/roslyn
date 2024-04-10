// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class LocaleTests(ITestOutputHelper? testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition => base.Composition
            .AddParts(typeof(LocaleTestHandler));

    [Theory, CombinatorialData]
    public async Task TestUsesLspLocale(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            Locale = "ja"
        });

        var result = await testLspServer.ExecuteRequestAsync<Request, Response>(LocaleTestHandler.MethodName, new Request(), CancellationToken.None);
        Assert.Equal("ja-JP", result!.HandlerCulture);
    }

    [Theory, CombinatorialData]
    public async Task TestUsesLspLocalePerServer(bool mutatingLspWorkspace)
    {
        await using var testLspServerOne = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            Locale = "ja"
        });

        await using var testLspServerTwo = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            Locale = "zh"
        });

        var resultOne = await testLspServerOne.ExecuteRequestAsync<Request, Response>(LocaleTestHandler.MethodName, new Request(), CancellationToken.None);
        var resultTwo = await testLspServerTwo.ExecuteRequestAsync<Request, Response>(LocaleTestHandler.MethodName, new Request(), CancellationToken.None);
        Assert.Equal("ja-JP", resultOne!.HandlerCulture);
        Assert.Equal("zh-CN", resultTwo!.HandlerCulture);
    }

    [Theory, CombinatorialData]
    public async Task TestUsesDefaultLocaleIfNotProvided(bool mutatingLspWorkspace)
    {
        var currentCulture = CultureInfo.CurrentUICulture.Name;
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            Locale = null
        });

        var result = await testLspServer.ExecuteRequestAsync<Request, Response>(LocaleTestHandler.MethodName, new Request(), CancellationToken.None);
        Assert.Equal(currentCulture, result!.HandlerCulture);
    }

    [Theory, CombinatorialData]
    public async Task TestUsesDefaultLocaleIfInvalidLocaleProvided(bool mutatingLspWorkspace)
    {
        var currentCulture = CultureInfo.CurrentUICulture.Name;
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            Locale = "this is invalid"
        });

        var result = await testLspServer.ExecuteRequestAsync<Request, Response>(LocaleTestHandler.MethodName, new Request(), CancellationToken.None);
        Assert.Equal(currentCulture, result!.HandlerCulture);
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(LocaleTestHandler)), PartNotDiscoverable, Shared]
    [Method(MethodName)]
    internal class LocaleTestHandler : ILspServiceRequestHandler<Request, Response>
    {
        public const string MethodName = nameof(LocaleTestHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LocaleTestHandler()
        {
        }

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public Task<Response> HandleRequestAsync(Request request, RequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Response(CultureInfo.CurrentUICulture.Name));
        }
    }

    internal record Request();

    internal record Response(string HandlerCulture);
}
