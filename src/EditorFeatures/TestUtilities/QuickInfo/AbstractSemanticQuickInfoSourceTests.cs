// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.LanguageServices;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
{
    public abstract class AbstractSemanticQuickInfoSourceTests
    {
        protected AbstractSemanticQuickInfoSourceTests() { }

        protected FormattedClassification Text(string text)
            => FormattedClassifications.Text(text);

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
