// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal sealed class CopilotOnTheFlyDocsInfoWrapper
{
    private readonly OnTheFlyDocsInfo _onTheFlyDocsInfo;
    private readonly ImmutableArray<CopilotOnTheFlyDocsRelevantFileInfoWrapper?> _wrappedDeclarationCode;
    private readonly ImmutableArray<CopilotOnTheFlyDocsRelevantFileInfoWrapper?> _wrappedAdditionalContext;

    public CopilotOnTheFlyDocsInfoWrapper(OnTheFlyDocsInfo onTheFlyDocsInfo)
    {
        _onTheFlyDocsInfo = onTheFlyDocsInfo;
        _wrappedDeclarationCode = _onTheFlyDocsInfo.DeclarationCode.SelectAsArray(c => c is not null ? new CopilotOnTheFlyDocsRelevantFileInfoWrapper(c) : null);
        _wrappedAdditionalContext = _onTheFlyDocsInfo.AdditionalContext.SelectAsArray(c => c is not null ? new CopilotOnTheFlyDocsRelevantFileInfoWrapper(c) : null);

    }

    public string SymbolSignature => _onTheFlyDocsInfo.SymbolSignature;
    public ImmutableArray<CopilotOnTheFlyDocsRelevantFileInfoWrapper?> DeclarationCode => _wrappedDeclarationCode;
    public string Language => _onTheFlyDocsInfo.Language;
    public bool IsContentExcluded => _onTheFlyDocsInfo.IsContentExcluded;
    public ImmutableArray<CopilotOnTheFlyDocsRelevantFileInfoWrapper?> AdditionalContext => _wrappedAdditionalContext;
    public bool HasComments => _onTheFlyDocsInfo.HasComments;
}

