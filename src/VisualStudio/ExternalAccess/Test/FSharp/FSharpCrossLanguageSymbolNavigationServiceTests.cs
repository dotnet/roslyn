// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Composition.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

public sealed class FSharpCrossLanguageSymbolNavigationServiceTests
{
    [Fact]
    public async Task DefinitionSpanComesFromAnFSharpServiceThatHasTheMember()
    {
        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Project", LanguageNames.CSharp).AddDocument("Library.fs", "type Counter() = class end");
        SpanService.Span = new FSharpDocumentSpan(document, new TextSpan(5, 7));

        var span = await GetService(typeof(SpanService)).TryGetDefinitionSpanAsync("Library", "T:Counter", CancellationToken.None);

        Assert.Equal(new DocumentSpan(document, new TextSpan(5, 7)), span);
    }

    [Fact]
    public async Task NoDefinitionSpanFromAnFSharpServiceBuiltBeforeTheMember()
    {
        var span = await GetService(typeof(NavigationOnlyService)).TryGetDefinitionSpanAsync("Library", "T:Counter", CancellationToken.None);

        Assert.Null(span);
    }

    private static ICrossLanguageSymbolNavigationService GetService(Type fsharpService)
        => new ContainerConfiguration()
            .WithParts(typeof(FSharpCrossLanguageSymbolNavigationService), fsharpService)
            .CreateContainer()
            .GetExport<ICrossLanguageSymbolNavigationService>();

    [Export(typeof(IFSharpCrossLanguageSymbolNavigationService)), Shared]
    private sealed class NavigationOnlyService : IFSharpCrossLanguageSymbolNavigationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigationOnlyService()
        {
        }

        public Task<IFSharpNavigableLocation?> TryGetNavigableLocationAsync(string assemblyName, string documentationCommentId, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    [Export(typeof(IFSharpCrossLanguageSymbolNavigationService)), Shared]
    private sealed class SpanService : IFSharpCrossLanguageSymbolNavigationService2
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SpanService()
        {
        }

        public static FSharpDocumentSpan? Span { get; set; }

        public Task<IFSharpNavigableLocation?> TryGetNavigableLocationAsync(string assemblyName, string documentationCommentId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<FSharpDocumentSpan?> TryGetDefinitionSpanAsync(string assemblyName, string documentationCommentId, CancellationToken cancellationToken)
            => Task.FromResult(Span);
    }
}
