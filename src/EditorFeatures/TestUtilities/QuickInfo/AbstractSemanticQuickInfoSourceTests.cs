// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        [DebuggerStepThrough]
        protected Tuple<string, string> Struct(string value)
        {
            return ClassificationBuilder.Struct(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Enum(string value)
        {
            return ClassificationBuilder.Enum(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Interface(string value)
        {
            return ClassificationBuilder.Interface(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Class(string value)
        {
            return ClassificationBuilder.Class(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Delegate(string value)
        {
            return ClassificationBuilder.Delegate(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> TypeParameter(string value)
        {
            return ClassificationBuilder.TypeParameter(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> String(string value)
        {
            return ClassificationBuilder.String(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Verbatim(string value)
        {
            return ClassificationBuilder.Verbatim(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Keyword(string value)
        {
            return ClassificationBuilder.Keyword(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> WhiteSpace(string value)
        {
            return ClassificationBuilder.WhiteSpace(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Text(string value)
        {
            return ClassificationBuilder.Text(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> NumericLiteral(string value)
        {
            return ClassificationBuilder.NumericLiteral(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> PPKeyword(string value)
        {
            return ClassificationBuilder.PPKeyword(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> PPText(string value)
        {
            return ClassificationBuilder.PPText(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Identifier(string value)
        {
            return ClassificationBuilder.Identifier(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Inactive(string value)
        {
            return ClassificationBuilder.Inactive(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Comment(string value)
        {
            return ClassificationBuilder.Comment(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Number(string value)
        {
            return ClassificationBuilder.Number(value);
        }

        protected ClassificationBuilder.PunctuationClassificationTypes Punctuation
        {
            get { return ClassificationBuilder.Punctuation; }
        }

        protected ClassificationBuilder.OperatorClassificationTypes Operators
        {
            get { return ClassificationBuilder.Operator; }
        }

        protected ClassificationBuilder.XmlDocClassificationTypes XmlDoc
        {
            get { return ClassificationBuilder.XmlDoc; }
        }

        protected string Lines(params string[] lines)
        {
            return string.Join("\r\n", lines);
        }

        protected Tuple<string, string>[] ExpectedClassifications(
            params Tuple<string, string>[] expectedClassifications)
        {
            return expectedClassifications;
        }

        protected Tuple<string, string>[] NoClassifications()
        {
            return null;
        }

        private static void AssertTextAndClassifications(string expectedText, Tuple<string, string>[] expectedClassifications, IDeferredQuickInfoContent actualContent)
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
            Tuple<string, string>[] expectedClassifications = null)
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
            Tuple<string, string>[] expectedClassifications = null)
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
            Tuple<string, string>[] expectedClassifications = null)
        {
            return content =>
            {
                AssertTextAndClassifications(expectedText, expectedClassifications, ((QuickInfoDisplayDeferredContent)content).TypeParameterMap);
            };
        }

        protected Action<object> AnonymousTypes(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
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

        protected Action<object> Captures(string expectedText)
        {
            return content =>
            {
                AssertTextAndClassifications(expectedText, expectedClassifications: null, actualContent: ((QuickInfoDisplayDeferredContent)content).CapturesText);
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
