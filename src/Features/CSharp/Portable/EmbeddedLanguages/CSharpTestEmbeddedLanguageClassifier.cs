// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
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

            var virtualCharsWithMarkup = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            if (virtualCharsWithMarkup.IsDefaultOrEmpty)
                return;

            cancellationToken.ThrowIfCancellationRequested();
            // Simpler to only support literals with non-complex escapes.
            if (virtualCharsWithMarkup.Any(static vc => vc.Utf16SequenceLength != 1))
                return;

            var virtualCharsWithoutMarkup = StringMarkupCharacters(context, virtualCharsWithMarkup);

            cancellationToken.ThrowIfCancellationRequested();

            var encoding = semanticModel.SyntaxTree.Encoding;
            var testFileSourceText = new VirtualCharSequenceSourceText(virtualCharsWithoutMarkup, encoding);

            var testFileTree = SyntaxFactory.ParseSyntaxTree(testFileSourceText, semanticModel.SyntaxTree.Options, cancellationToken: cancellationToken);
            var compilationWithTestFile = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(testFileTree);
            var semanticModeWithTestFile = compilationWithTestFile.GetSemanticModel(testFileTree);

            var start = virtualCharsWithoutMarkup[0].Span.Start;
            context.AddClassification(
                ClassificationTypeNames.TestCode,
                TextSpan.FromBounds(
                    start,
                    virtualCharsWithoutMarkup.Last().Span.End));

            var testFileClassifiedSpans = Classifier.GetClassifiedSpans(
                context.WorkspaceServices,
                project: null,
                semanticModeWithTestFile,
                new TextSpan(0, virtualCharsWithoutMarkup.Length),
                ClassificationOptions.Default,
                cancellationToken);

            foreach (var testClassifiedSpan in testFileClassifiedSpans)
                AddClassifications(context, virtualCharsWithoutMarkup, testClassifiedSpan);
        }

        private void AddClassifications(
            EmbeddedLanguageClassificationContext context,
            VirtualCharSequence virtualChars,
            ClassifiedSpan classifiedSpan)
        {
            if (classifiedSpan.TextSpan.IsEmpty)
                return;

            var classificationType = classifiedSpan.ClassificationType;
            var startIndexInclusive = classifiedSpan.TextSpan.Start;
            var endIndexExclusive = classifiedSpan.TextSpan.End;

            //var firstVirtualChar = virtualChars[startIndexInclusive];
            //var lastVirtualChar = virtualChars[endIndexExclusive - 1];

            var currentStartIndexInclusive = startIndexInclusive;
            while (currentStartIndexInclusive < endIndexExclusive)
            {
                var currentEndIndexExclusive = currentStartIndexInclusive + 1;

                while (currentEndIndexExclusive < endIndexExclusive &&
                       virtualChars[currentEndIndexExclusive - 1].Span.End == virtualChars[currentEndIndexExclusive].Span.Start)
                {
                    currentEndIndexExclusive++;
                }

                context.AddClassification(
                    classificationType,
                    FromBounds(virtualChars[currentStartIndexInclusive], virtualChars[currentEndIndexExclusive - 1]));
                currentStartIndexInclusive = currentEndIndexExclusive;
            }
        }

        private static VirtualCharSequence StringMarkupCharacters(
            EmbeddedLanguageClassificationContext context, VirtualCharSequence virtualChars)
        {
            var builder = ImmutableSegmentedList.CreateBuilder<VirtualChar>();

            for (int i = 0, n = virtualChars.Length; i < n;)
            {
                var vc = virtualChars[i];
                if (i != n - 1)
                {
                    var next = virtualChars[i + 1];

                    // This cast is safe because we disallowed virtual chars whose Value doesn't fit in a char in
                    // RegisterClassifications.
                    switch (((char)vc.Value, (char)next.Value))
                    {
                        case ('$', '$'):
                        case ('[', '|'):
                        case ('|', ']'):
                        case ('|', '}'):
                            context.AddClassification(ClassificationTypeNames.RegexQuantifier, FromBounds(vc, next));
                            i += 2;
                            continue;

                        case ('{', '|'):
                            var seekPoint = i;
                            while (seekPoint < n)
                            {
                                var seekChar = virtualChars[seekPoint];
                                if (seekChar.Value == ':')
                                {
                                    context.AddClassification(ClassificationTypeNames.RegexQuantifier, FromBounds(vc, seekChar));
                                    i = seekPoint + 1;
                                    continue;
                                }

                                seekPoint++;
                            }

                            // didn't find the colon.  don't classify these specially.
                            break;
                    }
                }

                // Nothing special, add character as is.
                builder.Add(vc);
                i++;
            }

            return VirtualCharSequence.Create(builder.ToImmutable());
        }

        private static TextSpan FromBounds(VirtualChar vc1, VirtualChar vc2)
        {
            return TextSpan.FromBounds(vc1.Span.Start, vc2.Span.End);
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
