// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Testing
{
    public interface IVerifier
    {
        /// <summary>
        /// Verify that a specified <paramref name="collection"/> is empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="collection">The collection.</param>
        void Empty<T>(string collectionName, IEnumerable<T> collection);

        /// <summary>
        /// Verify that two items are equal.
        /// </summary>
        /// <typeparam name="T">The type of item to compare.</typeparam>
        /// <param name="expected">The expected item.</param>
        /// <param name="actual">The actual item.</param>
        /// <param name="message">The message to report if the items are not equal, or <see langword="null"/> to use a default message.</param>
        void Equal<T>(T expected, T actual, string? message = null);

        /// <summary>
        /// Verify that a value is <see langword="true"/>.
        /// </summary>
        /// <param name="assert">The value.</param>
        /// <param name="message">The message to report if the value is not <see langword="true"/>, or <see langword="null"/> to use a default message.</param>
        void True(bool assert, string? message = null);

        /// <summary>
        /// Verify that a value is <see langword="false"/>.
        /// </summary>
        /// <param name="assert">The value.</param>
        /// <param name="message">The message to report if the value is not <see langword="false"/>, or <see langword="null"/> to use a default message.</param>
        void False(bool assert, string? message = null);

        /// <summary>
        /// Called to indicate validation has failed.
        /// </summary>
        /// <param name="message">The failure message to report, or <see langword="null"/> to use a default message.</param>
        void Fail(string? message = null);

        /// <summary>
        /// Verifies that a specific language is supported by this verifier.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <seealso cref="LanguageNames"/>
        void LanguageIsSupported(string language);

        /// <summary>
        /// Verify that a specified <paramref name="collection"/> is not empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="collection">The collection.</param>
        void NotEmpty<T>(string collectionName, IEnumerable<T> collection);

        /// <summary>
        /// Verify that two collections are equal.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="expected">The expected collection.</param>
        /// <param name="actual">The actual collection.</param>
        /// <param name="equalityComparer">The comparer to use for elements in the collection, or <see langword="null"/> to use the default comparer.</param>
        /// <param name="message">The message to report if the collections are not equal, or <see langword="null"/> to use a default message.</param>
        void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null);

        /// <summary>
        /// Creates a new verifier for validation within a specific context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A new <see cref="IVerifier"/> which includes the specified <paramref name="context"/> in failure messages.</returns>
        IVerifier PushContext(string context);
    }
}
