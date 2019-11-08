// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
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

        public override bool Equals(object? obj)
        {
            return obj is ClassifiedSpan &&
                Equals((ClassifiedSpan)obj);
        }

        public bool Equals(ClassifiedSpan other)
        {
            return this is { ClassificationType: other.ClassificationType, TextSpan: other.TextSpan };
        }
    }
}
