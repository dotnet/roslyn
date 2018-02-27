// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public PunctuationClassificationTypes Punctuation { get; } = new PunctuationClassificationTypes();
        public OperatorClassificationTypes Operator { get; } = new OperatorClassificationTypes();
        public XmlDocClassificationTypes XmlDoc { get; } = new XmlDocClassificationTypes();

        private static FormattedClassification New(string text, string typeName) => new FormattedClassification(text, typeName);

        [DebuggerStepThrough]
        public FormattedClassification Struct(string text) => New(text, ClassificationTypeNames.StructName);

        [DebuggerStepThrough]
        public FormattedClassification Enum(string text) => New(text, ClassificationTypeNames.EnumName);

        [DebuggerStepThrough]
        public FormattedClassification Interface(string text) => New(text, ClassificationTypeNames.InterfaceName);

        [DebuggerStepThrough]
        public FormattedClassification Class(string text) => New(text, ClassificationTypeNames.ClassName);

        [DebuggerStepThrough]
        public FormattedClassification Delegate(string text) => New(text, ClassificationTypeNames.DelegateName);

        [DebuggerStepThrough]
        public FormattedClassification TypeParameter(string text) => New(text, ClassificationTypeNames.TypeParameterName);

        [DebuggerStepThrough]
        public FormattedClassification Field(string text) => New(text, ClassificationTypeNames.FieldName);

        [DebuggerStepThrough]
        public FormattedClassification EnumMember(string text) => New(text, ClassificationTypeNames.EnumMemberName);

        [DebuggerStepThrough]
        public FormattedClassification Constant(string text) => New(text, ClassificationTypeNames.ConstantName);

        [DebuggerStepThrough]
        public FormattedClassification Local(string text) => New(text, ClassificationTypeNames.LocalName);

        [DebuggerStepThrough]
        public FormattedClassification Parameter(string text) => New(text, ClassificationTypeNames.ParameterName);

        [DebuggerStepThrough]
        public FormattedClassification Method(string text) => New(text, ClassificationTypeNames.MethodName);

        [DebuggerStepThrough]
        public FormattedClassification ExtensionMethod(string text) => New(text, ClassificationTypeNames.ExtensionMethodName);

        [DebuggerStepThrough]
        public FormattedClassification Property(string text) => New(text, ClassificationTypeNames.PropertyName);

        [DebuggerStepThrough]
        public FormattedClassification Event(string text) => New(text, ClassificationTypeNames.EventName);

        [DebuggerStepThrough]
        public FormattedClassification String(string text) => New(text, ClassificationTypeNames.StringLiteral);

        [DebuggerStepThrough]
        public FormattedClassification Verbatim(string text) => New(text, ClassificationTypeNames.VerbatimStringLiteral);

        [DebuggerStepThrough]
        public FormattedClassification Keyword(string text) => New(text, ClassificationTypeNames.Keyword);

        [DebuggerStepThrough]
        public FormattedClassification WhiteSpace(string text) => New(text, ClassificationTypeNames.WhiteSpace);

        [DebuggerStepThrough]
        public FormattedClassification Text(string text) => New(text, ClassificationTypeNames.Text);

        [DebuggerStepThrough]
        public FormattedClassification NumericLiteral(string text) => New(text, ClassificationTypeNames.NumericLiteral);

        [DebuggerStepThrough]
        public FormattedClassification PPKeyword(string text) => New(text, ClassificationTypeNames.PreprocessorKeyword);

        [DebuggerStepThrough]
        public FormattedClassification PPText(string text) => New(text, ClassificationTypeNames.PreprocessorText);

        [DebuggerStepThrough]
        public FormattedClassification Identifier(string text) => New(text, ClassificationTypeNames.Identifier);

        [DebuggerStepThrough]
        public FormattedClassification Inactive(string text) => New(text, ClassificationTypeNames.ExcludedCode);

        [DebuggerStepThrough]
        public FormattedClassification Comment(string text) => New(text, ClassificationTypeNames.Comment);

        [DebuggerStepThrough]
        public FormattedClassification Number(string text) => New(text, ClassificationTypeNames.NumericLiteral);

        public FormattedClassification LineContinuation { get; } = New("_", ClassificationTypeNames.Punctuation);

        [DebuggerStepThrough]
        public FormattedClassification Module(string text) => New(text, ClassificationTypeNames.ModuleName);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlName(string text) => New(text, ClassificationTypeNames.XmlLiteralName);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlText(string text) => New(text, ClassificationTypeNames.XmlLiteralText);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlProcessingInstruction(string text) => New(text, ClassificationTypeNames.XmlLiteralProcessingInstruction);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlEmbeddedExpression(string text) => New(text, ClassificationTypeNames.XmlLiteralEmbeddedExpression);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlDelimiter(string text) => New(text, ClassificationTypeNames.XmlLiteralDelimiter);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlComment(string text) => New(text, ClassificationTypeNames.XmlLiteralComment);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlCDataSection(string text) => New(text, ClassificationTypeNames.XmlLiteralCDataSection);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlAttributeValue(string text) => New(text, ClassificationTypeNames.XmlLiteralAttributeValue);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlAttributeQuotes(string text) => New(text, ClassificationTypeNames.XmlLiteralAttributeQuotes);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlAttributeName(string text) => New(text, ClassificationTypeNames.XmlLiteralAttributeName);

        [DebuggerStepThrough]
        public FormattedClassification VBXmlEntityReference(string text) => New(text, ClassificationTypeNames.XmlLiteralEntityReference);
    }
}
