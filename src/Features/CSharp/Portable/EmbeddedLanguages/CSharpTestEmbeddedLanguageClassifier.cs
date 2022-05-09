// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Net.Mime;
using System.Text;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    [ExportEmbeddedLanguageClassifierInternal(
        PredefinedEmbeddedLanguageClassifierNames.CSharpTest, LanguageNames.CSharp, supportsUnannotatedAPIs: false,
        PredefinedEmbeddedLanguageClassifierNames.CSharpTest), Shared]
    internal class CSharpTestEmbeddedLanguageClassifier : IEmbeddedLanguageClassifier
    {
        private readonly EmbeddedLanguageInfo _info;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpTestEmbeddedLanguageClassifier()
        {
            _info = CSharpEmbeddedLanguagesProvider.Info;
        }

        public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
        {
            var cancellationToken = context.CancellationToken;

            var token = context.SyntaxToken;
            var semanticModel = context.SemanticModel;
            var compilation = semanticModel.Compilation;

            if (token.Kind() is not (SyntaxKind.StringLiteralToken or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken))
                return;

            var virtualCharsWithTestCharacters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            if (virtualCharsWithTestCharacters.IsDefaultOrEmpty)
                return;

            cancellationToken.ThrowIfCancellationRequested();
            // Simpler to only support literals with non-complex escapes.
            if (virtualCharsWithTestCharacters.Any(static vc => vc.Utf16SequenceLength != 1))
                return;

            var virtualCharsWithoutTestCharacters = StripTestCharacters(virtualCharsWithTestCharacters);
            cancellationToken.ThrowIfCancellationRequested();

            var encoding = semanticModel.SyntaxTree.Encoding;
            var testFileSourceText = new VirtualCharSequenceSourceText(virtualCharsWithoutTestCharacters, encoding);

            var testFileTree = SyntaxFactory.ParseSyntaxTree(testFileSourceText, semanticModel.SyntaxTree.Options, cancellationToken: cancellationToken);
            var compilationWithTestFile = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(testFileTree);
            var semanticModeWithTestFile = compilationWithTestFile.GetSemanticModel(testFileTree);

            var start = virtualCharsWithoutTestCharacters[0].Span.Start;
            context.AddClassification(
                ClassificationTypeNames.TestCode,
                TextSpan.FromBounds(
                    start,
                    virtualCharsWithoutTestCharacters.Last().Span.End));

            var testFileClassifiedSpans = Classifier.GetClassifiedSpans(
                context.WorkspaceServices,
                project: null,
                semanticModeWithTestFile,
                new TextSpan(0, virtualCharsWithoutTestCharacters.Length),
                ClassificationOptions.Default,
                cancellationToken);

            foreach (var testClassifiedSpan in testFileClassifiedSpans)
            {
                context.AddClassification(
                    testClassifiedSpan.ClassificationType,
                    new TextSpan(start + testClassifiedSpan.TextSpan.Start, testClassifiedSpan.TextSpan.Length));
            }

            //context.AddClassification(
            //    ClassificationTypeNames.StaticSymbol,
            //    TextSpan.FromBounds(
            //        virtualCharsWithoutTestCharacters[0].Span.Start,
            //        virtualCharsWithoutTestCharacters.Last().Span.End));

            //context.AddClassification(
            //    ClassificationTypeNames.Keyword,
            //    TextSpan.FromBounds(start + 1, start + 8));
        }

        private VirtualCharSequence StripTestCharacters(VirtualCharSequence virtualChars)
        {
            return virtualChars;
        }

        private class VirtualCharSequenceSourceText : SourceText
        {
            private readonly VirtualCharSequence _virtualChars;

            public override Encoding? Encoding { get; }

            public VirtualCharSequenceSourceText(VirtualCharSequence virtualChars, Encoding? encoding)
            {
                _virtualChars = virtualChars;
                Encoding = encoding;
            }

            public override int Length => _virtualChars.Length;

            public override char this[int position]
            {
                // This cast is safe because we disallowed virtual chars whose Value doesn't fit in a char in
                // RegisterClassifications.
                get => (char)_virtualChars[position].Value;
            }

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                for (int i = sourceIndex, n = sourceIndex + count; i < n; i++)
                    destination[destinationIndex + i] = this[i];
            }
        }
    }
}
