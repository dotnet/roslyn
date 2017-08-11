// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public sealed class XmlDocClassificationTypes
        {
            [DebuggerStepThrough]
            public Tuple<string, string> AttributeName(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentAttributeName);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> AttributeQuotes(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentAttributeQuotes);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> AttributeValue(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentAttributeValue);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> CDataSection(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentCDataSection);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> Comment(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentComment);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> Delimiter(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentDelimiter);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> EntityReference(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentEntityReference);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> Name(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentName);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> ProcessingInstruction(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentProcessingInstruction);
            }

            [DebuggerStepThrough]
            public Tuple<string, string> Text(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.XmlDocCommentText);
            }
        }
    }
}
