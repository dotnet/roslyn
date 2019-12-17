// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// Represents an ID of a vertex or edge.
    /// </summary>
    /// <typeparam name="T">Used to distinguish what type of object this ID applies to. This is not actually used, but simply helps
    /// to ensure type safety since it's not uncommon to hold onto just an ID somewhere.</typeparam>
    internal struct Id<T> : IEquatable<Id<T>>, ISerializableId where T : Element
    {
        /// <summary>
        /// The next numberic ID that will be used for an object. Accessed only with Interlocked.Increment.
        /// </summary>
        private static int s_globalId = 0;

        public Id(int id)
        {
            NumericId = id;
        }

        public int NumericId { get; }

        public static Id<T> Create()
        {
            var id = Interlocked.Increment(ref s_globalId);
            return new Id<T>(id);
        }

        public override bool Equals(object? obj)
        {
            return obj is Id<T> other && Equals(other);
        }

        public bool Equals(Id<T> other)
        {
            return other.NumericId == NumericId;
        }

        public override int GetHashCode()
        {
            return NumericId.GetHashCode();
        }

        public static bool operator ==(Id<T> left, Id<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Id<T> left, Id<T> right)
        {
            return !(left == right);
        }
    }

    interface ISerializableId
    {
        public int NumericId { get; }
    }

    internal static class IdExtensions
    {
        /// <summary>
        /// "Casts" a strongly type ID representing a derived type to a base type.
        /// </summary>
        public static Id<TOut> As<TIn, TOut>(this Id<TIn> id) where TOut : Element where TIn : TOut
        {
            return new Id<TOut>(id.NumericId);
        }

        /// <summary>
        /// Fetches a strongly-typed <see cref="Id{T}"/> for a given element.
        /// </summary>
        public static Id<T> GetId<T>(this T element) where T : Element
        {
            return new Id<T>(element.Id.NumericId);
        }
    }
}
