//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System;
//using System.Diagnostics;
//using Microsoft.CodeAnalysis.Classification;

//namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
//{
//    public partial class ClassificationBuilder
//    {
//        private readonly OperatorClassificationTypes _operator = new OperatorClassificationTypes();
//        private readonly PunctuationClassificationTypes _punctuation = new PunctuationClassificationTypes();
//        private readonly XmlDocClassificationTypes _xmlDoc = new XmlDocClassificationTypes();
//        private readonly RegexClassificationTypes _regex = new RegexClassificationTypes();

//        [DebuggerStepThrough]
//        public Tuple<string, string> Struct(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.StructName);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Enum(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.EnumName);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Interface(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.InterfaceName);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Class(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.ClassName);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Delegate(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.DelegateName);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> TypeParameter(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.TypeParameterName);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> String(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.StringLiteral);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Verbatim(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.VerbatimStringLiteral);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Keyword(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.Keyword);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> WhiteSpace(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.WhiteSpace);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Text(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.Text);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> NumericLiteral(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.NumericLiteral);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> PPKeyword(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.PreprocessorKeyword);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> PPText(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.PreprocessorText);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Identifier(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.Identifier);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Inactive(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.ExcludedCode);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Comment(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.Comment);
//        }

//        [DebuggerStepThrough]
//        public Tuple<string, string> Number(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.NumericLiteral);
//        }

//        public Tuple<string, string> LineContinuation
//        {
//            get
//            {
//                return Tuple.Create("_", ClassificationTypeNames.Punctuation);
//            }
//        }

//        public Tuple<string, string> Module(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.ModuleName);
//        }

//        public Tuple<string, string> VBXmlName(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralName);
//        }

//        public Tuple<string, string> VBXmlText(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralText);
//        }

//        public Tuple<string, string> VBXmlProcessingInstruction(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralProcessingInstruction);
//        }

//        public Tuple<string, string> VBXmlEmbeddedExpression(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralEmbeddedExpression);
//        }

//        public Tuple<string, string> VBXmlDelimiter(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralDelimiter);
//        }

//        public Tuple<string, string> VBXmlComment(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralComment);
//        }

//        public Tuple<string, string> VBXmlCDataSection(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralCDataSection);
//        }

//        public Tuple<string, string> VBXmlAttributeValue(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralAttributeValue);
//        }

//        public Tuple<string, string> VBXmlAttributeQuotes(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralAttributeQuotes);
//        }

//        public Tuple<string, string> VBXmlAttributeName(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralAttributeName);
//        }

//        public Tuple<string, string> VBXmlEntityReference(string value)
//        {
//            return Tuple.Create(value, ClassificationTypeNames.XmlLiteralEntityReference);
//        }

//        public PunctuationClassificationTypes Punctuation => _punctuation;

//        public OperatorClassificationTypes Operator => _operator;

//        public XmlDocClassificationTypes XmlDoc => _xmlDoc;

//        public RegexClassificationTypes Regex => _regex;
//    }
//}
