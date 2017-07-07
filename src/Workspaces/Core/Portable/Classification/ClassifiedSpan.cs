// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal enum ClassificationTypeKind
    {
        Keyword,
        Identifier,
        Operator,
        Punctuation,

        NumericLiteral,
        StringLiteral,
        VerbatimStringLiteral,

        ClassName,
        DelegateName,
        EnumName,
        InterfaceName,
        ModuleName,
        StructName,
        TypeParameterName,

        PreprocessorKeyword,
        PreprocessorText,

        Comment,
        ExcludedCode,

        XmlDocCommentAttributeName,
        XmlDocCommentAttributeQuotes,
        XmlDocCommentAttributeValue,
        XmlDocCommentCDataSection,
        XmlDocCommentComment,
        XmlDocCommentDelimiter,
        XmlDocCommentEntityReference,
        XmlDocCommentName,
        XmlDocCommentProcessingInstruction,
        XmlDocCommentText,

        XmlLiteralAttributeName,
        XmlLiteralAttributeQuotes,
        XmlLiteralAttributeValue,
        XmlLiteralCDataSection,
        XmlLiteralComment,
        XmlLiteralDelimiter,
        XmlLiteralEmbeddedExpression,
        XmlLiteralEntityReference,
        XmlLiteralName,
        XmlLiteralProcessingInstruction,
        XmlLiteralText,
    }

    internal struct ClassifiedSpanSlim : IEquatable<ClassifiedSpanSlim>
    {
        public readonly ClassificationTypeKind Kind;
        public readonly TextSpan TextSpan;

        public ClassifiedSpanSlim(ClassificationTypeKind kind, TextSpan textSpan)
        {
            Kind = kind;
            TextSpan = textSpan;
        }

        public ClassifiedSpanSlim(TextSpan textSpan, ClassificationTypeKind kind)
        {
            Kind = kind;
            TextSpan = textSpan;
        }

        public override bool Equals(object obj) 
            => obj is ClassifiedSpanSlim classifiedSpan && Equals(classifiedSpan);

        public bool Equals(ClassifiedSpanSlim other) 
            => Kind == other.Kind && TextSpan.Equals(other.TextSpan);

        public override int GetHashCode()
            => Hash.Combine(TextSpan.GetHashCode(), (int)Kind);

        public ClassifiedSpan ToClassifiedSpan()
            => new ClassifiedSpan(TextSpan, GetClassificationType(Kind));

        private string GetClassificationType(ClassificationTypeKind kind)
        {
            switch (kind)
            {
                case ClassificationTypeKind.Keyword: return ClassificationTypeNames.Keyword;
                case ClassificationTypeKind.Identifier: return ClassificationTypeNames.Identifier;
                case ClassificationTypeKind.Operator: return ClassificationTypeNames.Operator;
                case ClassificationTypeKind.Punctuation: return ClassificationTypeNames.Punctuation;
                case ClassificationTypeKind.NumericLiteral: return ClassificationTypeNames.NumericLiteral;
                case ClassificationTypeKind.StringLiteral: return ClassificationTypeNames.StringLiteral;
                case ClassificationTypeKind.VerbatimStringLiteral: return ClassificationTypeNames.VerbatimStringLiteral;
                case ClassificationTypeKind.ClassName: return ClassificationTypeNames.ClassName;
                case ClassificationTypeKind.DelegateName: return ClassificationTypeNames.DelegateName;
                case ClassificationTypeKind.EnumName: return ClassificationTypeNames.EnumName;
                case ClassificationTypeKind.InterfaceName: return ClassificationTypeNames.InterfaceName;
                case ClassificationTypeKind.ModuleName: return ClassificationTypeNames.ModuleName;
                case ClassificationTypeKind.StructName: return ClassificationTypeNames.StructName;
                case ClassificationTypeKind.TypeParameterName: return ClassificationTypeNames.TypeParameterName;
                case ClassificationTypeKind.PreprocessorKeyword: return ClassificationTypeNames.PreprocessorKeyword;
                case ClassificationTypeKind.PreprocessorText: return ClassificationTypeNames.PreprocessorText;
                case ClassificationTypeKind.Comment: return ClassificationTypeNames.Comment;
                case ClassificationTypeKind.ExcludedCode: return ClassificationTypeNames.ExcludedCode;
                case ClassificationTypeKind.XmlDocCommentAttributeName: return ClassificationTypeNames.XmlDocCommentAttributeName;
                case ClassificationTypeKind.XmlDocCommentAttributeQuotes: return ClassificationTypeNames.XmlDocCommentAttributeQuotes;
                case ClassificationTypeKind.XmlDocCommentAttributeValue: return ClassificationTypeNames.XmlDocCommentAttributeValue;
                case ClassificationTypeKind.XmlDocCommentCDataSection: return ClassificationTypeNames.XmlDocCommentCDataSection;
                case ClassificationTypeKind.XmlDocCommentComment: return ClassificationTypeNames.XmlDocCommentComment;
                case ClassificationTypeKind.XmlDocCommentDelimiter: return ClassificationTypeNames.XmlDocCommentDelimiter;
                case ClassificationTypeKind.XmlDocCommentEntityReference: return ClassificationTypeNames.XmlDocCommentEntityReference;
                case ClassificationTypeKind.XmlDocCommentName: return ClassificationTypeNames.XmlDocCommentName;
                case ClassificationTypeKind.XmlDocCommentProcessingInstruction: return ClassificationTypeNames.XmlDocCommentProcessingInstruction;
                case ClassificationTypeKind.XmlDocCommentText: return ClassificationTypeNames.XmlDocCommentText;
                case ClassificationTypeKind.XmlLiteralAttributeName: return ClassificationTypeNames.XmlLiteralAttributeName;
                case ClassificationTypeKind.XmlLiteralAttributeQuotes: return ClassificationTypeNames.XmlLiteralAttributeQuotes;
                case ClassificationTypeKind.XmlLiteralAttributeValue: return ClassificationTypeNames.XmlLiteralAttributeValue;
                case ClassificationTypeKind.XmlLiteralCDataSection: return ClassificationTypeNames.XmlLiteralCDataSection;
                case ClassificationTypeKind.XmlLiteralComment: return ClassificationTypeNames.XmlLiteralComment;
                case ClassificationTypeKind.XmlLiteralDelimiter: return ClassificationTypeNames.XmlLiteralDelimiter;
                case ClassificationTypeKind.XmlLiteralEmbeddedExpression: return ClassificationTypeNames.XmlLiteralEmbeddedExpression;
                case ClassificationTypeKind.XmlLiteralEntityReference: return ClassificationTypeNames.XmlLiteralEntityReference;
                case ClassificationTypeKind.XmlLiteralName: return ClassificationTypeNames.XmlLiteralName;
                case ClassificationTypeKind.XmlLiteralProcessingInstruction: return ClassificationTypeNames.XmlLiteralProcessingInstruction;
                case ClassificationTypeKind.XmlLiteralText: return ClassificationTypeNames.XmlLiteralText;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }

    public struct ClassifiedSpan : IEquatable<ClassifiedSpan>
    {
        public string ClassificationType { get; }
        public TextSpan TextSpan { get; }

        public ClassifiedSpan(string classificationType, TextSpan textSpan)
            : this(textSpan, classificationType)
        {
        }

        public ClassifiedSpan(TextSpan textSpan, string classificationType)
            : this()
        {
            this.ClassificationType = classificationType;
            this.TextSpan = textSpan;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.ClassificationType, this.TextSpan.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return obj is ClassifiedSpan &&
                Equals((ClassifiedSpan)obj);
        }

        public bool Equals(ClassifiedSpan other)
        {
            return this.ClassificationType == other.ClassificationType && this.TextSpan == other.TextSpan;
        }
    }
}
