// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
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
            CancellationToken cancellationToken = default)
        {
            var semanticModel = await document.GetSemanticModelForSpanAsync(textSpan, cancellationToken).ConfigureAwait(false);
            return GetClassifiedSpans(semanticModel, textSpan, document.Project.Solution.Workspace, cancellationToken);
        }

        public static IEnumerable<ClassifiedSpan> GetClassifiedSpans(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Workspace workspace,
            CancellationToken cancellationToken = default)
        {
            var service = workspace.Services.GetLanguageServices(semanticModel.Language).GetRequiredService<ISyntaxClassificationService>();

            var syntaxClassifiers = service.GetDefaultSyntaxClassifiers();

            var extensionManager = workspace.Services.GetRequiredService<IExtensionManager>();
            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(syntaxClassifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(syntaxClassifiers, c => c.SyntaxTokenKinds);

            var syntacticClassifications = ArrayBuilder<ClassifiedSpan>.GetInstance();
            var semanticClassifications = ArrayBuilder<ClassifiedSpan>.GetInstance();
            try
            {
                service.AddSyntacticClassifications(semanticModel.SyntaxTree, textSpan, syntacticClassifications, cancellationToken);
                service.AddSemanticClassifications(semanticModel, textSpan, workspace, getNodeClassifiers, getTokenClassifiers, semanticClassifications, cancellationToken);

                var allClassifications = new List<ClassifiedSpan>(semanticClassifications.Where(s => s.TextSpan.OverlapsWith(textSpan)));
                var semanticSet = semanticClassifications.Select(s => s.TextSpan).ToSet();

                allClassifications.AddRange(syntacticClassifications.Where(
                    s => s.TextSpan.OverlapsWith(textSpan) && !semanticSet.Contains(s.TextSpan)));
                allClassifications.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);

                return allClassifications;
            }
            finally
            {
                syntacticClassifications.Free();
                semanticClassifications.Free();
            }
        }

        internal static async Task<ImmutableArray<SymbolDisplayPart>> GetClassifiedSymbolDisplayPartsAsync(
            SemanticModel semanticModel, TextSpan textSpan, Workspace workspace,
            CancellationToken cancellationToken = default)
        {
            var classifiedSpans = GetClassifiedSpans(semanticModel, textSpan, workspace, cancellationToken);
            var sourceText = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return ConvertClassificationsToParts(sourceText, textSpan.Start, classifiedSpans);
        }

        internal static ImmutableArray<SymbolDisplayPart> ConvertClassificationsToParts(
            SourceText sourceText, int startPosition, IEnumerable<ClassifiedSpan> classifiedSpans)
        {
            var parts = ArrayBuilder<SymbolDisplayPart>.GetInstance();

            foreach (var span in classifiedSpans)
            {
                // If there is space between this span and the last one, then add a space.
                if (startPosition < span.TextSpan.Start)
                {
                    parts.AddRange(Space());
                }

                var kind = GetClassificationKind(span.ClassificationType);
                if (kind != null)
                {
                    parts.Add(new SymbolDisplayPart(kind.Value, null, sourceText.ToString(span.TextSpan)));

                    startPosition = span.TextSpan.End;
                }
            }

            return parts.ToImmutableAndFree();
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
                case ClassificationTypeNames.FieldName:
                    return SymbolDisplayPartKind.FieldName;
                case ClassificationTypeNames.EnumMemberName:
                    return SymbolDisplayPartKind.EnumMemberName;
                case ClassificationTypeNames.ConstantName:
                    return SymbolDisplayPartKind.ConstantName;
                case ClassificationTypeNames.LocalName:
                    return SymbolDisplayPartKind.LocalName;
                case ClassificationTypeNames.ParameterName:
                    return SymbolDisplayPartKind.ParameterName;
                case ClassificationTypeNames.ExtensionMethodName:
                    return SymbolDisplayPartKind.ExtensionMethodName;
                case ClassificationTypeNames.MethodName:
                    return SymbolDisplayPartKind.MethodName;
                case ClassificationTypeNames.PropertyName:
                    return SymbolDisplayPartKind.PropertyName;
                case ClassificationTypeNames.LabelName:
                    return SymbolDisplayPartKind.LabelName;
                case ClassificationTypeNames.NamespaceName:
                    return SymbolDisplayPartKind.NamespaceName;
                case ClassificationTypeNames.EventName:
                    return SymbolDisplayPartKind.EventName;
            }
        }
    }
}
