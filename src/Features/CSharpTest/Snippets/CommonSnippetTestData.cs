// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

public static class CommonSnippetTestData
{
    public static TheoryData<string> IntegerTypes => [
        "byte",
        "sbyte",
        "short",
        "ushort",
        "int",
        "uint",
        "long",
        "ulong",
        "nint",
        "nuint",
    ];

    public static TheoryData<string> NotIntegerTypesWithoutLengthOrCountProperty => [
        "object",
        "System.DateTime",
        "System.Action",
    ];

    public static TheoryData<string> AllAccessibilityModifiers => [
        "public",
        "private",
        "protected",
        "internal",
        "private protected",
        "protected internal",
    ];

    public static TheoryData<string> CommonEnumerableTypes => [
        "string",
        "System.Collections.Generic.List<int>",
        "int[]",
        "System.Collections.Generic.IEnumerable<int>",
        "System.Collections.ArrayList",
        "System.Collections.IEnumerable",
    ];
}
