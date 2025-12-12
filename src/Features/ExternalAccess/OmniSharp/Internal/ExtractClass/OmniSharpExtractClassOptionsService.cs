// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ExtractClass;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal.ExtractClass;

[ExportWorkspaceService(typeof(IExtractClassOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OmniSharpExtractClassOptionsService(
    IOmniSharpExtractClassOptionsService omniSharpExtractClassOptionsService) : IExtractClassOptionsService
{
    private readonly IOmniSharpExtractClassOptionsService _omniSharpExtractClassOptionsService = omniSharpExtractClassOptionsService;

    public ExtractClassOptions? GetExtractClassOptions(
        Document document,
        INamedTypeSymbol originalType,
        ImmutableArray<ISymbol> selectedMembers,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        var result = _omniSharpExtractClassOptionsService.GetExtractClassOptions(document, originalType, selectedMembers);
        return result == null
            ? null
            : new ExtractClassOptions(
                result.FileName,
                result.TypeName,
                result.SameFile,
                result.MemberAnalysisResults.SelectAsArray(m => new ExtractClassMemberAnalysisResult(m.Member, m.MakeAbstract)));
    }
}
