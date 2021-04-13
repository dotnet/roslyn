// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents an ID of a vertex or edge.
    /// </summary>
    /// <typeparam name="T">Used to distinguish what type of object this ID applies to. This is dropped in serialization, but simply helps
    /// to ensure type safety in the code so we don't cross IDs of different types.</typeparam>
    internal struct Id<T> : IEquatable<Id<T>>, ISerializableId where T : Element
    {
        public Id(int id)
        {
            NumericId = id;
        }

        public int NumericId { get; }

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

        public override string ToString()
        {
            return $"{NumericId}";
        }
    }

    internal interface ISerializableId
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
