// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public static partial class FormattedClassifications
    {
        public static class XmlDoc
        {
            [DebuggerStepThrough]
            public static FormattedClassification AttributeName(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentAttributeName);

            [DebuggerStepThrough]
            public static FormattedClassification AttributeQuotes(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentAttributeQuotes);

            [DebuggerStepThrough]
            public static FormattedClassification AttributeValue(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentAttributeValue);

            [DebuggerStepThrough]
            public static FormattedClassification CDataSection(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentCDataSection);

            [DebuggerStepThrough]
            public static FormattedClassification Comment(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentComment);

            [DebuggerStepThrough]
            public static FormattedClassification Delimiter(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentDelimiter);

            [DebuggerStepThrough]
            public static FormattedClassification EntityReference(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentEntityReference);

            [DebuggerStepThrough]
            public static FormattedClassification Name(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentName);

            [DebuggerStepThrough]
            public static FormattedClassification ProcessingInstruction(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentProcessingInstruction);

            [DebuggerStepThrough]
            public static FormattedClassification Text(string text)
                => New(text, ClassificationTypeNames.XmlDocCommentText);
        }
    }
}
