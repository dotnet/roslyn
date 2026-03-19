// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing;

[Trait(Traits.Feature, Traits.Features.Organizing)]
public sealed class OrganizeModifiersTests : AbstractOrganizerTests
{
    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestTypes1(string typeKind)
        => CheckAsync($$"""
            static public {{typeKind}} C {
            }
            """, $$"""
            public static {{typeKind}} C {
            }
            """);

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestTypes2(string typeKind)
        => CheckAsync($$"""
            public static {{typeKind}} D {
            }
            """, $$"""
            public static {{typeKind}} D {
            }
            """);

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestTypes3(string typeKind)
        => CheckAsync($$"""
            public static partial {{typeKind}} E {
            }
            """, $$"""
            public static partial {{typeKind}} E {
            }
            """);

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestTypes4(string typeKind)
        => CheckAsync($$"""
            static public partial {{typeKind}} F {
            }
            """, $$"""
            public static partial {{typeKind}} F {
            }
            """);

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestTypes5(string typeKind)
        => CheckAsync($$"""
            unsafe public static {{typeKind}} F {
            }
            """, $$"""
            public static unsafe {{typeKind}} F {
            }
            """);
}
