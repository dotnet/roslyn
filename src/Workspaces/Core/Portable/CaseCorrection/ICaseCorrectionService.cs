// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CaseCorrection
{
    internal interface ICaseCorrectionService : ILanguageService
    {
        /// <summary>
        /// Case corrects all names found in the spans in the provided document.
        /// </summary>
        Task<Document> CaseCorrectAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken);

        /// <summary>
        /// Case corrects only things that don't require semantic information
        /// </summary>
        SyntaxNode CaseCorrect(SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken);
    }
}
