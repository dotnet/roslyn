// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
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

        internal static async Task<List<SymbolDisplayPart>> GetClassifiedSymbolDisplayPartsAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
 
            return await GetClassifiedSymbolDisplayPartsAsync(
                semanticModel, textSpan,
                document.Project.Solution.Workspace,
                cancellationToken).ConfigureAwait(false);
        }
 
        internal static async Task<List<SymbolDisplayPart>> GetClassifiedSymbolDisplayPartsAsync(
            SemanticModel semanticModel, TextSpan textSpan, Workspace workspace, CancellationToken cancellationToken)
        {
            var classifiedSpans = GetClassifiedSpans(semanticModel, textSpan, workspace, cancellationToken);
            var sourceText = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return ConvertClassifications(sourceText, classifiedSpans);
        }
 
        private static List<SymbolDisplayPart> ConvertClassifications(
            SourceText sourceText, IEnumerable<ClassifiedSpan> classifiedSpans)
        {
            var parts = new List<SymbolDisplayPart>();
 
            ClassifiedSpan? lastSpan = null;
            foreach (var span in classifiedSpans)
            {
                // If there is space between this span and the last one, then add a space.
                if (lastSpan != null && lastSpan.Value.TextSpan.End != span.TextSpan.Start)
                {
                    parts.AddRange(Space());
                }
 
                var kind = GetClassificationKind(span.ClassificationType);
                if (kind != null)
                {
                    parts.Add(new SymbolDisplayPart(kind.Value, null, sourceText.ToString(span.TextSpan)));
 
                    lastSpan = span;
                }
            }
 
            return parts;
        }
 
        private static IEnumerable<SymbolDisplayPart> Space(int count = 1)
        {
            yield return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, new string(' ', count));
        }
 
        private static SymbolDisplayPartKind? GetClassificationKind(string type)
        {
            switch (type)
            {
                default:
                    return null;
                case ClassificationTypeNames.Identifier:
                    return SymbolDisplayPartKind.Text;
                case ClassificationTypeNames.Keyword:
                    return SymbolDisplayPartKind.Keyword;
                case ClassificationTypeNames.NumericLiteral:
                    return SymbolDisplayPartKind.NumericLiteral;
                case ClassificationTypeNames.StringLiteral:
                    return SymbolDisplayPartKind.StringLiteral;
                case ClassificationTypeNames.WhiteSpace:
                    return SymbolDisplayPartKind.Space;
                case ClassificationTypeNames.Operator:
                    return SymbolDisplayPartKind.Operator;
                case ClassificationTypeNames.Punctuation:
                    return SymbolDisplayPartKind.Punctuation;
                case ClassificationTypeNames.ClassName:
                    return SymbolDisplayPartKind.ClassName;
                case ClassificationTypeNames.StructName:
                    return SymbolDisplayPartKind.StructName;
                case ClassificationTypeNames.InterfaceName:
                    return SymbolDisplayPartKind.InterfaceName;
                case ClassificationTypeNames.DelegateName:
                    return SymbolDisplayPartKind.DelegateName;
                case ClassificationTypeNames.EnumName:
                    return SymbolDisplayPartKind.EnumName;
                case ClassificationTypeNames.TypeParameterName:
                    return SymbolDisplayPartKind.TypeParameterName;
                case ClassificationTypeNames.ModuleName:
                    return SymbolDisplayPartKind.ModuleName;
                case ClassificationTypeNames.VerbatimStringLiteral:
                    return SymbolDisplayPartKind.StringLiteral;
            }
        }
    }
}