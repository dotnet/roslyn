// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents an ID of a vertex or edge.
    /// </summary>
    /// <typeparam name="T">Used to distinguish what type of object this ID applies to. This is dropped in serialization, but simply helps
    /// to ensure type safety in the code so we don't cross IDs of different types.</typeparam>
    internal readonly record struct Id<T>(int NumericId) : ISerializableId where T : Element;

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
