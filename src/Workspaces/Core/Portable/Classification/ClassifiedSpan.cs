// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        {
            this.ClassificationType = classificationType;
            this.TextSpan = textSpan;
        }

        public override int GetHashCode()
            => Hash.Combine(this.ClassificationType, this.TextSpan.GetHashCode());

        public override bool Equals(object? obj)
        {
            return obj is ClassifiedSpan &&
                Equals((ClassifiedSpan)obj);
        }

        public bool Equals(ClassifiedSpan other)
            => this.ClassificationType == other.ClassificationType && this.TextSpan == other.TextSpan;
    }
}
