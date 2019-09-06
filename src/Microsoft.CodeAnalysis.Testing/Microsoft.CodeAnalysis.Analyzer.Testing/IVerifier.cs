// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Testing
{
    public interface IVerifier
    {
        void Empty<T>(string collectionName, IEnumerable<T> collection);

        void Equal<T>(T expected, T actual, string? message = null);

        void True(bool assert, string? message = null);

        void False(bool assert, string? message = null);

        void Fail(string? message = null);

        void LanguageIsSupported(string language);

        void NotEmpty<T>(string collectionName, IEnumerable<T> collection);

        void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null);

        IVerifier PushContext(string context);
    }
}
