// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A SyntaxAnnotation is used to annotate syntax elements with additional information. 
    /// 
    /// Since syntax elements are immutable, annotating them requires creating new instances of them
    /// with the annotations attached.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class SyntaxAnnotation : IEquatable<SyntaxAnnotation?>
    {
        /// <summary>
        /// A predefined syntax annotation that indicates whether the syntax element has elastic trivia.
        /// </summary>
        public static SyntaxAnnotation ElasticAnnotation { get; } = new SyntaxAnnotation();

        public string? Kind { get; }
        public string? Data { get; }

        public SyntaxAnnotation()
        {
        }

        public SyntaxAnnotation(string? kind)
            : this()
        {
            this.Kind = kind;
        }

        public SyntaxAnnotation(string? kind, string? data)
            : this(kind)
        {
            this.Data = data;
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("Annotation: Kind='{0}' Data='{1}'", this.Kind ?? "", this.Data ?? "");
        }

        public bool Equals(SyntaxAnnotation? other)
        {
            return Equals((object?)other);
        }

        public static bool operator ==(SyntaxAnnotation? left, SyntaxAnnotation? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(SyntaxAnnotation? left, SyntaxAnnotation? right) =>
            !(left == right);

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
