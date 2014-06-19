// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A SynatxAnnotation is used to annotate syntax elements with additional information. 
    /// 
    /// Since syntax elements are immutable, annotating them requires creating new instances of them
    /// with the annotations attached.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class SyntaxAnnotation : IObjectWritable, IObjectReadable, IEquatable<SyntaxAnnotation>
    {
        /// <summary>
        /// A predefined syntax annotation that indicates whether the syntax element has elastic trivia.
        /// </summary>
        public static readonly SyntaxAnnotation ElasticAnnotation = new SyntaxAnnotation();

        // use a value identity instead of object identity so a deserialized instance matches the original instance.
        private readonly long id;
        private static long nextId;

        // use a value identity instead of object identity so a deserialized instance matches the original instance.
        public string Kind { get; private set; }
        public string Data { get; private set; }

        public SyntaxAnnotation()
        {
            this.id = System.Threading.Interlocked.Increment(ref nextId);
        }

        public SyntaxAnnotation(string kind)
            : this()
        {
            this.Kind = kind;
        }

        public SyntaxAnnotation(string kind, string data)
            : this(kind)
        {
            this.Data = data;
        }

        private SyntaxAnnotation(ObjectReader reader)
        {
            this.id = reader.ReadInt64();
            this.Kind = reader.ReadString();
            this.Data = reader.ReadString();
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteInt64(this.id);
            writer.WriteString(this.Kind);
            writer.WriteString(this.Data);
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            return r => new SyntaxAnnotation(r);
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("Annotation: Kind='{0}' Data='{1}'", this.Kind ?? "", this.Data ?? "");
        }

        public bool Equals(SyntaxAnnotation other)
        {
            return (object)other != null && this.id == other.id;
        }

        public static bool operator ==(SyntaxAnnotation left, SyntaxAnnotation right)
        {
            if ((object)left == (object)right)
            {
                return true;
            }

            if ((object)left == null || (object)right == null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(SyntaxAnnotation left, SyntaxAnnotation right)
        {
            if ((object)left == (object)right)
            {
                return false;
            }

            if ((object)left == null || (object)right == null)
            {
                return true;
            }

            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as SyntaxAnnotation);
        }

        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }
    }
}