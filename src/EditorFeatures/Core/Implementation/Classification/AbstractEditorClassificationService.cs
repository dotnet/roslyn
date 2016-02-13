// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal abstract class AbstractEditorClassificationService : IEditorClassificationService
    {
        public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);
        public abstract ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetLanguageService<IClassificationService>();

            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();
            var classifiers = classificationService.GetDefaultSyntaxClassifiers();

            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds);

            return classificationService.AddSemanticClassificationsAsync(document, textSpan, getNodeClassifiers, getTokenClassifiers, result, cancellationToken);
        }

        public async Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetLanguageService<IClassificationService>();
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            classificationService.AddSyntacticClassifications(syntaxTree, textSpan, result, cancellationToken);
        }
    }
}
