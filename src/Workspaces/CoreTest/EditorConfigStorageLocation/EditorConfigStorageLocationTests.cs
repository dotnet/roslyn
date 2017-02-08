// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditorConfig.StorageLocation
{
    public class EditorConfigStorageLocationTests
    {
        [Fact]
        public static void TestEmptyDictionaryReturnFalse()
        {
            var editorConfigStorageLocation = new EditorConfigStorageLocation();
            var result = editorConfigStorageLocation.TryParseReadonlyDictionary(new Dictionary<string, object>(), typeof(NamingStylePreferences), out var @object);
            Assert.False(result, "Expected TryParseReadonlyDictionary to return 'false' for empty dictionary");
        }

        [Fact]
        public static void TestObjectTypeThrowsNotSupportedException()
        {
            var editorConfigStorageLocation = new EditorConfigStorageLocation();
            Assert.Throws<NotSupportedException>(() =>
            {
                editorConfigStorageLocation.TryParseReadonlyDictionary(new Dictionary<string, object>(), typeof(object), out var @object);
            });
        }
    }
}
