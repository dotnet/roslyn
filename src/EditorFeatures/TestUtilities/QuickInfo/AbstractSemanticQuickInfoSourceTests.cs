// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.LanguageServices;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
{
    public abstract class AbstractSemanticQuickInfoSourceTests
    {
        protected readonly ClassificationBuilder ClassificationBuilder;

        protected AbstractSemanticQuickInfoSourceTests()
        {
            this.ClassificationBuilder = new ClassificationBuilder();
        }

        protected ClassificationBuilder.PunctuationClassificationTypes Punctuation => ClassificationBuilder.Punctuation;
        protected ClassificationBuilder.OperatorClassificationTypes Operators => ClassificationBuilder.Operator;
        protected ClassificationBuilder.XmlDocClassificationTypes XmlDoc => ClassificationBuilder.XmlDoc;

        [DebuggerStepThrough]
        protected FormattedClassification Struct(string text) => ClassificationBuilder.Struct(text);

        [DebuggerStepThrough]
        protected FormattedClassification Enum(string text) => ClassificationBuilder.Enum(text);

        [DebuggerStepThrough]
        protected FormattedClassification Interface(string text) => ClassificationBuilder.Interface(text);

        [DebuggerStepThrough]
        protected FormattedClassification Class(string text) => ClassificationBuilder.Class(text);

        [DebuggerStepThrough]
        protected FormattedClassification Delegate(string text) => ClassificationBuilder.Delegate(text);

        [DebuggerStepThrough]
        protected FormattedClassification TypeParameter(string text) => ClassificationBuilder.TypeParameter(text);

        [DebuggerStepThrough]
        protected FormattedClassification String(string text) => ClassificationBuilder.String(text);

        [DebuggerStepThrough]
        protected FormattedClassification Verbatim(string text) => ClassificationBuilder.Verbatim(text);

        [DebuggerStepThrough]
        protected FormattedClassification Keyword(string text) => ClassificationBuilder.Keyword(text);

        [DebuggerStepThrough]
        protected FormattedClassification WhiteSpace(string text) => ClassificationBuilder.WhiteSpace(text);

        [DebuggerStepThrough]
        protected FormattedClassification Text(string text) => ClassificationBuilder.Text(text);

        [DebuggerStepThrough]
        protected FormattedClassification NumericLiteral(string text) => ClassificationBuilder.NumericLiteral(text);

        [DebuggerStepThrough]
        protected FormattedClassification PPKeyword(string text) => ClassificationBuilder.PPKeyword(text);

        [DebuggerStepThrough]
        protected FormattedClassification PPText(string text) => ClassificationBuilder.PPText(text);

        [DebuggerStepThrough]
        protected FormattedClassification Identifier(string text) => ClassificationBuilder.Identifier(text);

        [DebuggerStepThrough]
        protected FormattedClassification Inactive(string text) => ClassificationBuilder.Inactive(text);

        [DebuggerStepThrough]
        protected FormattedClassification Comment(string text) => ClassificationBuilder.Comment(text);

        [DebuggerStepThrough]
        protected FormattedClassification Number(string text) => ClassificationBuilder.Number(text);

        protected string Lines(params string[] lines)
        {
            return string.Join("\r\n", lines);
        }

        protected FormattedClassification[] ExpectedClassifications(
            params FormattedClassification[] expectedClassifications)
        {
            return expectedClassifications;
        }

        protected FormattedClassification[] NoClassifications()
        {
            return null;
        }

        private static void AssertTextAndClassifications(string expectedText, FormattedClassification[] expectedClassifications, IDeferredQuickInfoContent actualContent)
        {
            var actualClassifications = ((ClassifiableDeferredContent)actualContent).ClassifiableContent;

            ClassificationTestHelper.VerifyTextAndClassifications(expectedText, expectedClassifications, actualClassifications);
        }

        protected void WaitForDocumentationComment(object content)
        {
            if (content is QuickInfoDisplayDeferredContent deferredContent)
            {
                if (deferredContent.Documentation is DocumentationCommentDeferredContent docCommentDeferredContent)
                {
                    docCommentDeferredContent.WaitForDocumentationCommentTask_ForTestingPurposesOnly();
                }
            }
        }

        internal Action<object> SymbolGlyph(Glyph expectedGlyph)
        {
            return content =>
            {
                var actualIcon = (SymbolGlyphDeferredContent)((QuickInfoDisplayDeferredContent)content).SymbolGlyph;
                Assert.Equal(expectedGlyph, actualIcon.Glyph);
            };
        }

        protected Action<object> MainDescription(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return content =>
            {
                switch (content)
                {
                    case QuickInfoDisplayDeferredContent qiContent:
                        {
                            AssertTextAndClassifications(expectedText, expectedClassifications, (ClassifiableDeferredContent)qiContent.MainDescription);
                        }
                        break;

                    case ClassifiableDeferredContent classifiable:
                        {
                            var actualContent = classifiable.ClassifiableContent;
                            ClassificationTestHelper.VerifyTextAndClassifications(expectedText, expectedClassifications, actualContent);
                        }
                        break;
                }
            };
        }

        protected Action<object> Documentation(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return content =>
            {
                var documentationCommentContent = ((QuickInfoDisplayDeferredContent)content).Documentation;
                switch (documentationCommentContent)
                {
                    case DocumentationCommentDeferredContent docComment:
                        {
                            Assert.Equal(expectedText, docComment.DocumentationComment);
                        }
                        break;

                    case ClassifiableDeferredContent classifiable:
                        {
                            var actualContent = classifiable.ClassifiableContent;
                            Assert.Equal(expectedText, actualContent.GetFullText());
                            ClassificationTestHelper.VerifyTextAndClassifications(expectedText, expectedClassifications, actualContent);
                        }
                        break;
                }
            };
        }

        protected Action<object> TypeParameterMap(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return content =>
            {
                AssertTextAndClassifications(expectedText, expectedClassifications, ((QuickInfoDisplayDeferredContent)content).TypeParameterMap);
            };
        }

        protected Action<object> AnonymousTypes(
            string expectedText,
            FormattedClassification[] expectedClassifications = null)
        {
            return content =>
            {
                AssertTextAndClassifications(expectedText, expectedClassifications, ((QuickInfoDisplayDeferredContent)content).AnonymousTypes);
            };
        }


        protected Action<object> NoTypeParameterMap
        {
            get
            {
                return content =>
                {
                    AssertTextAndClassifications("", NoClassifications(), ((QuickInfoDisplayDeferredContent)content).TypeParameterMap);
                };
            }
        }

        protected Action<object> Usage(string expectedText, bool expectsWarningGlyph = false)
        {
            return content =>
            {
                var quickInfoContent = (QuickInfoDisplayDeferredContent)content;
                Assert.Equal(expectedText, ((ClassifiableDeferredContent)quickInfoContent.UsageText).ClassifiableContent.GetFullText());
                var warningGlyph = quickInfoContent.WarningGlyph as SymbolGlyphDeferredContent;
                Assert.Equal(expectsWarningGlyph, warningGlyph != null && warningGlyph.Glyph == Glyph.CompletionWarning);
            };
        }

        protected Action<object> Exceptions(string expectedText)
        {
            return content =>
            {
                AssertTextAndClassifications(expectedText, expectedClassifications: null, actualContent: ((QuickInfoDisplayDeferredContent)content).ExceptionText);
            };
        }

        protected static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
        {
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        protected abstract Task TestAsync(string markup, params Action<object>[] expectedResults);
    }
}
