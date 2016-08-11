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
using Microsoft.CodeAnalysis.QuickInfo;
using System.Linq;

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
/*
            if (content is QuickInfoDisplayDeferredContent)
            {
                var docCommentDeferredContent = ((QuickInfoDisplayDeferredContent)content).Documentation as DocumentationCommentDeferredContent;
                if (docCommentDeferredContent != null)
                {
                    docCommentDeferredContent.WaitForDocumentationCommentTask_ForTestingPurposesOnly();
                }
            }
*/
        }

        internal Action<QuickInfoElement> ElementGlyph(string elementKind, Glyph expectedGlyph)
        {
            return (element) =>
            {
                var glyphElement = element.Elements.FirstOrDefault(e => e.Kind == elementKind);
                Assert.NotNull(glyphElement);
                var actualGlyph = element.Tags.GetGlyph();
                Assert.Equal(expectedGlyph, actualGlyph);
            };
        }

        internal Action<QuickInfoElement> SymbolGlyph(Glyph expectedGlyph)
        {
            return ElementGlyph(QuickInfoElementKinds.Symbol, expectedGlyph);
        }

        internal Action<QuickInfoElement> WarningGlyph(Glyph expectedGlyph)
        {
            return ElementGlyph(QuickInfoElementKinds.Warning, expectedGlyph);
        }

        internal Action<QuickInfoElement> ElementText(
            string elementKind,
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return (element) =>
            {
                var md = element.Elements.FirstOrDefault(e => e.Kind == elementKind);
                if (md != null)
                {
                    var actualText = md.RawText;
                    Assert.Equal(expectedText, actualText);
                }
                else
                {
                    Assert.Equal(expectedText, string.Empty);
                }

                /*
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
                */
            };
        }

        internal Action<QuickInfoElement> MainDescription(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return ElementText(QuickInfoElementKinds.Description, expectedText, expectedClassifications);
        }

        internal Action<QuickInfoElement> Documentation(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return ElementText(QuickInfoElementKinds.Documentation, expectedText, expectedClassifications);
        }

        internal Action<QuickInfoElement> TypeParameterMap(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return ElementText(QuickInfoElementKinds.TypeParameterMap, expectedText, expectedClassifications);

            /*
            return (content) =>
            {
                var actualContent = ((QuickInfoDisplayDeferredContent)content).TypeParameterMap.ClassifiableContent;

                // The type parameter map should have an additional line break at the beginning. We
                // create a copy here because we've captured expectedText and this delegate might be
                // executed more than once (e.g. with different parse options).

                // var expectedTextCopy = "\r\n" + expectedText;
                ClassificationTestHelper.Verify(expectedText, expectedClassifications, actualContent);
            };
            */
        }

        internal Action<QuickInfoElement> AnonymousTypes(
            string expectedText,
            Tuple<string, string>[] expectedClassifications = null)
        {
            return ElementText(QuickInfoElementKinds.AnonymousTypes, expectedText, expectedClassifications);

            /*
            return (content) =>
            {
                var actualContent = ((QuickInfoDisplayDeferredContent)content).AnonymousTypes.ClassifiableContent;

                // The type parameter map should have an additional line break at the beginning. We
                // create a copy here because we've captured expectedText and this delegate might be
                // executed more than once (e.g. with different parse options).

                // var expectedTextCopy = "\r\n" + expectedText;
                ClassificationTestHelper.Verify(expectedText, expectedClassifications, actualContent);
            };
            */
        }

        internal Action<QuickInfoElement> NoTypeParameterMap
        {
            get
            {
                return ElementText(QuickInfoElementKinds.TypeParameterMap, string.Empty);
                /*
                return (element) =>
                {

                    Assert.Equal(string.Empty, ((QuickInfoDisplayDeferredContent)content).TypeParameterMap.ClassifiableContent.GetFullText());
                };
                */
            }
        }

        internal Action<QuickInfoElement> Usage(string expectedText, bool expectsWarningGlyph = false)
        {
            return (element) =>
            {
                ElementText(QuickInfoElementKinds.Usage, expectedText)(element);
                if (expectsWarningGlyph)
                {
                    WarningGlyph(Glyph.CompletionWarning)(element);
                }
                else
                {
                    Assert.Null(element.Elements.FirstOrDefault(e => e.Kind == QuickInfoElementKinds.Warning));
                }
            };
        }

        internal Action<QuickInfoElement> Exceptions(string expectedText)
        {
            return ElementText(QuickInfoElementKinds.Exception, expectedText);
        }

        protected static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
        {
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        internal abstract Task TestAsync(string markup, params Action<QuickInfoElement>[] expectedResults);
    }
}