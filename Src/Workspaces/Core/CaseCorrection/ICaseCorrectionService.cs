// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CaseCorrection
{
    internal interface ICaseCorrectionService : ILanguageService
    {
        /// <summary>
        /// Case corrects all names found in the spans in the provided document.
        /// </summary>
        Task<Document> CaseCorrectAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken);

        /// <summary>
        /// case corrects only things that doesn't require semantic information
        /// </summary>
        SyntaxNode CaseCorrect(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken);
    }
}