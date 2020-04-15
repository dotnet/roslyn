// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A SyntaxAnnotation is used to annotate syntax elements with additional information. 
    /// 
    /// Since syntax elements are immutable, annotating them requires creating new instances of them
    /// with the annotations attached.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class SyntaxAnnotation : IObjectWritable, IEquatable<SyntaxAnnotation?>
    {
        static SyntaxAnnotation()
        {
            ObjectBinder.RegisterTypeReader(typeof(SyntaxAnnotation), r => new SyntaxAnnotation(r));
        }

        /// <summary>
        /// A predefined syntax annotation that indicates whether the syntax element has elastic trivia.
        /// </summary>
        public static SyntaxAnnotation ElasticAnnotation { get; } = new SyntaxAnnotation();

        // use a value identity instead of object identity so a deserialized instance matches the original instance.
        private readonly long _id;
        private static long s_nextId;

        // use a value identity instead of object identity so a deserialized instance matches the original instance.
        public string? Kind { get; }
        public string? Data { get; }

        public SyntaxAnnotation()
        {
            _id = System.Threading.Interlocked.Increment(ref s_nextId);
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

        private SyntaxAnnotation(ObjectReader reader)
        {
            _id = reader.ReadInt64();
            this.Kind = reader.ReadString();
            this.Data = reader.ReadString();
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteInt64(_id);
            writer.WriteString(this.Kind);
            writer.WriteString(this.Data);
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("Annotation: Kind='{0}' Data='{1}'", this.Kind ?? "", this.Data ?? "");
        }

        public bool Equals(SyntaxAnnotation? other)
        {
            return other is object && _id == other._id;
        }

        public static bool operator ==(SyntaxAnnotation? left, SyntaxAnnotation? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(SyntaxAnnotation left, SyntaxAnnotation right) =>
            !(left == right);

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as SyntaxAnnotation);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}
