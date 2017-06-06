// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
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
}