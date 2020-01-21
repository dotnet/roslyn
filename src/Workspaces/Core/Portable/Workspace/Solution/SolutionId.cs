// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An identifier that can be used to refer to the same Solution across versions. 
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public sealed class SolutionId : IEquatable<SolutionId>, IObjectWritable
    {
        /// <summary>
        /// The unique id of the solution.
        /// </summary>
        public Guid Id { get; }

        private readonly string _debugName;

        private SolutionId(Guid id, string debugName)
        {
            this.Id = id;
            _debugName = debugName;
        }

        /// <summary>
        /// Create a new Solution Id
        /// </summary>
        /// <param name="debugName">An optional name to make this id easier to recognize while debugging.</param>
        public static SolutionId CreateNewId(string debugName = null)
        {
            return CreateFromSerialized(Guid.NewGuid(), debugName);
        }

        public static SolutionId CreateFromSerialized(Guid id, string debugName = null)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            debugName ??= "unsaved";

            return new SolutionId(id, debugName);
        }

        internal string DebugName => _debugName;

        private string GetDebuggerDisplay()
        {
            return string.Format("({0}, #{1} - {2})", GetType().Name, this.Id, _debugName);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as SolutionId);
        }

        public bool Equals(SolutionId other)
        {
            return
                !ReferenceEquals(other, null) &&
                this.Id == other.Id;
        }

        public static bool operator ==(SolutionId left, SolutionId right)
        {
            return EqualityComparer<SolutionId>.Default.Equals(left, right);
        }

        public static bool operator !=(SolutionId left, SolutionId right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteGuid(Id);
            writer.WriteString(DebugName);
        }

        internal static SolutionId ReadFrom(ObjectReader reader)
        {
            var guid = reader.ReadGuid();
            var debugName = reader.ReadString();

            return CreateFromSerialized(guid, debugName);
        }
    }
}
