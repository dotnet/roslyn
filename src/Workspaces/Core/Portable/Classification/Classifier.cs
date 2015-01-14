// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    public static class Classifier
    {
        public static async Task<IEnumerable<ClassifiedSpan>> GetClassifiedSpansAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var semanticModel = await document.GetSemanticModelForSpanAsync(textSpan, cancellationToken).ConfigureAwait(false);
            return GetClassifiedSpans(semanticModel, textSpan, document.Project.Solution.Workspace, cancellationToken);
        }

        public static IEnumerable<ClassifiedSpan> GetClassifiedSpans(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Workspace workspace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var service = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IClassificationService>();

            var syntaxClassifiers = service.GetDefaultSyntaxClassifiers();

            var extensionManager = workspace.Services.GetService<IExtensionManager>();
            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(syntaxClassifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(syntaxClassifiers, c => c.SyntaxTokenKinds);

            var syntacticClassifications = new List<ClassifiedSpan>();
            var semanticClassifications = new List<ClassifiedSpan>();

            service.AddSyntacticClassifications(semanticModel.SyntaxTree, textSpan, syntacticClassifications, cancellationToken);
            service.AddSemanticClassifications(semanticModel, textSpan, workspace, getNodeClassifiers, getTokenClassifiers, semanticClassifications, cancellationToken);

            var allClassifications = new List<ClassifiedSpan>(semanticClassifications.Where(s => s.TextSpan.OverlapsWith(textSpan)));
            var semanticSet = semanticClassifications.Select(s => s.TextSpan).ToSet();

            allClassifications.AddRange(syntacticClassifications.Where(
                s => s.TextSpan.OverlapsWith(textSpan) && !semanticSet.Contains(s.TextSpan)));
            allClassifications.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);

            return allClassifications;
        }
    }
}
