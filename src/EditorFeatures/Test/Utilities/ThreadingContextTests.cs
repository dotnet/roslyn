// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

public sealed class ThreadingContextTests
{
    [WpfFact]
    public async Task DisposingExportProviderDisposesThreadingContext()
    {
        var composition = EditorTestCompositions.EditorFeatures.GetCompositionConfiguration();
        var factory = RuntimeComposition.CreateRuntimeComposition(composition).CreateExportProviderFactory(joinableTaskFactory: null);
        await using var exportProvider = factory.CreateExportProvider();
        var threadingContext = exportProvider.GetExportedValue<IThreadingContext>();

        Assert.False(threadingContext.DisposalToken.IsCancellationRequested);
        await exportProvider.DisposeAsync();
        Assert.True(threadingContext.DisposalToken.IsCancellationRequested);
    }
}
