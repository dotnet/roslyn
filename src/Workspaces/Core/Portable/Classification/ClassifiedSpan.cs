// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification;

[DataContract]
public readonly struct ClassifiedSpan(TextSpan textSpan, string classificationType) : IEquatable<ClassifiedSpan>
{
    [DataMember(Order = 0)]
    public string ClassificationType { get; } = classificationType;
    [DataMember(Order = 1)]
    public TextSpan TextSpan { get; } = textSpan;

    public ClassifiedSpan(string classificationType, TextSpan textSpan)
        : this(textSpan, classificationType)
    {
    }

    public override int GetHashCode()
        => Hash.Combine(this.ClassificationType, this.TextSpan.GetHashCode());

    public override bool Equals(object? obj)
        => obj is ClassifiedSpan span && Equals(span);

    public bool Equals(ClassifiedSpan other)
        => this.ClassificationType == other.ClassificationType && this.TextSpan == other.TextSpan;
}
