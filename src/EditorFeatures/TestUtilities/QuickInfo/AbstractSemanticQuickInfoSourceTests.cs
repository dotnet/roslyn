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

        protected void WaitForDocumentationComment(object content)
        {
            if (content is QuickInfoDisplayDeferredContent)
            {
                var docCommentDeferredContent = ((QuickInfoDisplayDeferredContent)content).Documentation as DocumentationCommentDeferredContent;
                if (docCommentDeferredContent != null)
                {
                    docCommentDeferredContent.WaitForDocumentationCommentTask_ForTestingPurposesOnly();
                }
            }
        }

        internal Action<object> SymbolGlyph(Glyph expectedGlyph)
        {
            return (content) =>
            {
                var actualIcon = ((QuickInfoDisplayDeferredContent)content).SymbolGlyph;
                Assert.Equal(expectedGlyph, actualIcon.Glyph);
            };
        }

        protected Action<object> MainDescription(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return (content) =>
            {
                content.TypeSwitch(
                        (QuickInfoDisplayDeferredContent qiContent) =>
                        {
                            var actualContent = qiContent.MainDescription.ClassifiableContent;
                            ClassificationTestHelper.Verify(expectedText, expectedClassifications, actualContent);
                        },
                        (ClassifiableDeferredContent classifiable) =>
                        {
                            var actualContent = classifiable.ClassifiableContent;
                            ClassificationTestHelper.Verify(expectedText, expectedClassifications, actualContent);
                        });
            };
        }

        protected Action<object> Documentation(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return (content) =>
            {
                var documentationCommentContent = ((QuickInfoDisplayDeferredContent)content).Documentation;
                documentationCommentContent.TypeSwitch(
                    (DocumentationCommentDeferredContent docComment) =>
                    {
                        var documentationCommentBlock = (TextBlock)docComment.Create();
                        var actualText = documentationCommentBlock.Text;

                        Assert.Equal(expectedText, actualText);
                    },
                    (ClassifiableDeferredContent classifiable) =>
                    {
                        var actualContent = classifiable.ClassifiableContent;
                        Assert.Equal(expectedText, actualContent.GetFullText());
                        ClassificationTestHelper.Verify(expectedText, expectedClassifications, actualContent);
                    });
            };
        }

        protected Action<object> TypeParameterMap(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return (content) =>
            {
                var actualContent = ((QuickInfoDisplayDeferredContent)content).TypeParameterMap.ClassifiableContent;

                // The type parameter map should have an additional line break at the beginning. We
                // create a copy here because we've captured expectedText and this delegate might be
                // executed more than once (e.g. with different parse options).

                // var expectedTextCopy = "\r\n" + expectedText;
                ClassificationTestHelper.Verify(expectedText, expectedClassifications, actualContent);
            };
        }

        protected Action<object> AnonymousTypes(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return (content) =>
            {
                var actualContent = ((QuickInfoDisplayDeferredContent)content).AnonymousTypes.ClassifiableContent;

                // The type parameter map should have an additional line break at the beginning. We
                // create a copy here because we've captured expectedText and this delegate might be
                // executed more than once (e.g. with different parse options).

                // var expectedTextCopy = "\r\n" + expectedText;
                ClassificationTestHelper.Verify(expectedText, expectedClassifications, actualContent);
            };
        }

        protected Action<object> NoTypeParameterMap
        {
            get
            {
                return (content) =>
                {
                    Assert.Equal(string.Empty, ((QuickInfoDisplayDeferredContent)content).TypeParameterMap.ClassifiableContent.GetFullText());
                };
            }
        }

        protected Action<object> Usage(string expectedText, bool expectsWarningGlyph = false)
        {
            return (content) =>
            {
                var quickInfoContent = (QuickInfoDisplayDeferredContent)content;
                Assert.Equal(expectedText, quickInfoContent.UsageText.ClassifiableContent.GetFullText());
                Assert.Equal(expectsWarningGlyph, quickInfoContent.WarningGlyph != null && quickInfoContent.WarningGlyph.Glyph == Glyph.CompletionWarning);
            };
        }

        protected Action<object> Exceptions(string expectedText)
        {
            return (content) =>
            {
                var quickInfoContent = (QuickInfoDisplayDeferredContent)content;
                Assert.Equal(expectedText, quickInfoContent.ExceptionText.ClassifiableContent.GetFullText());
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