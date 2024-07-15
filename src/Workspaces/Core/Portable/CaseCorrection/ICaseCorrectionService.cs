// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CaseCorrection;

internal interface ICaseCorrectionService : ILanguageService
{
    /// <summary>
    /// Case corrects all names found in the spans in the provided document.
    /// </summary>
    Task<Document> CaseCorrectAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken);

    /// <summary>
    /// Case corrects only things that don't require semantic information
    /// </summary>
    SyntaxNode CaseCorrect(SyntaxNode root, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken);
}
