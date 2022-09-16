// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing
{
    [Trait(Traits.Feature, Traits.Features.Organizing)]
    public class OrganizeModifiersTests : AbstractOrganizerTests
    {
        [Theory]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestTypes1(string typeKind)
        {
            var initial =
$@"static public {typeKind} C {{
}}";
            var final =
$@"public static {typeKind} C {{
}}";

            await CheckAsync(initial, final);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestTypes2(string typeKind)
        {
            var initial =
$@"public static {typeKind} D {{
}}";
            var final =
$@"public static {typeKind} D {{
}}";

            await CheckAsync(initial, final);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestTypes3(string typeKind)
        {
            var initial =
$@"public static partial {typeKind} E {{
}}";
            var final =
$@"public static partial {typeKind} E {{
}}";

            await CheckAsync(initial, final);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestTypes4(string typeKind)
        {
            var initial =
$@"static public partial {typeKind} F {{
}}";
            var final =
$@"public static partial {typeKind} F {{
}}";

            await CheckAsync(initial, final);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestTypes5(string typeKind)
        {
            var initial =
$@"unsafe public static {typeKind} F {{
}}";
            var final =
$@"public static unsafe {typeKind} F {{
}}";

            await CheckAsync(initial, final);
        }
    }
}
