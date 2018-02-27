// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public sealed class XmlDocClassificationTypes
        {
            [DebuggerStepThrough]
            public FormattedClassification AttributeName(string text) => New(text, ClassificationTypeNames.XmlDocCommentAttributeName);

            [DebuggerStepThrough]
            public FormattedClassification AttributeQuotes(string text) => New(text, ClassificationTypeNames.XmlDocCommentAttributeQuotes);

            [DebuggerStepThrough]
            public FormattedClassification AttributeValue(string text) => New(text, ClassificationTypeNames.XmlDocCommentAttributeValue);

            [DebuggerStepThrough]
            public FormattedClassification CDataSection(string text) => New(text, ClassificationTypeNames.XmlDocCommentCDataSection);

            [DebuggerStepThrough]
            public FormattedClassification Comment(string text) => New(text, ClassificationTypeNames.XmlDocCommentComment);

            [DebuggerStepThrough]
            public FormattedClassification Delimiter(string text) => New(text, ClassificationTypeNames.XmlDocCommentDelimiter);

            [DebuggerStepThrough]
            public FormattedClassification EntityReference(string text) => New(text, ClassificationTypeNames.XmlDocCommentEntityReference);

            [DebuggerStepThrough]
            public FormattedClassification Name(string text) => New(text, ClassificationTypeNames.XmlDocCommentName);

            [DebuggerStepThrough]
            public FormattedClassification ProcessingInstruction(string text) => New(text, ClassificationTypeNames.XmlDocCommentProcessingInstruction);

            [DebuggerStepThrough]
            public FormattedClassification Text(string text) => New(text, ClassificationTypeNames.XmlDocCommentText);
        }
    }
}
