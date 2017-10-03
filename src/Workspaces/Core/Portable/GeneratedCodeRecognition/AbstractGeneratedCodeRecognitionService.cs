// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GeneratedCodeRecognition
{
    internal abstract class AbstractGeneratedCodeRecognitionService : IGeneratedCodeRecognitionService
    {
        public bool IsGeneratedCode(Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return GeneratedCodeUtilities.IsGeneratedCode(
                syntaxTree, t => syntaxFacts.IsRegularComment(t) || syntaxFacts.IsDocumentationComment(t), cancellationToken);
        }
    }
}
