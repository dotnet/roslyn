// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// A does-nothing version of the <see cref="IFindReferencesProgress"/>. Useful for
/// clients that have no need to report progress as they work.
/// </summary>
internal sealed class NoOpFindReferencesProgress : IFindReferencesProgress
{
    public static readonly IFindReferencesProgress Instance = new NoOpFindReferencesProgress();

    private NoOpFindReferencesProgress()
    {
    }

    public void ReportProgress(int current, int maximum)
    {
    }

    public void OnCompleted()
    {
    }

    public void OnStarted()
    {
    }

    public void OnDefinitionFound(ISymbol symbol)
    {
    }

    public void OnReferenceFound(ISymbol symbol, ReferenceLocation location)
    {
    }

    public void OnFindInDocumentStarted(Document document)
    {
    }

    public void OnFindInDocumentCompleted(Document document)
    {
    }
}
