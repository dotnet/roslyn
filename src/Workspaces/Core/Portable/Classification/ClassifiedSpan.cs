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
            => obj is ClassifiedSpanSlim && Equals((ClassifiedSpanSlim)obj);

        public bool Equals(ClassifiedSpanSlim other) 
            => Kind == other.Kind && TextSpan.Equals(other.TextSpan);

        public override int GetHashCode()
            => Hash.Combine(TextSpan.GetHashCode(), (int)Kind);

        public ClassifiedSpan ToClassifiedSpan()
            => new ClassifiedSpan(TextSpan, GetClassificationType(Kind));

        private string GetClassificationType(ClassificationTypeKind kind)
        {
            throw new NotImplementedException();
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
