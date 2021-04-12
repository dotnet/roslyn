﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class EventFieldDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EventFieldDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new EventFieldDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEventFieldWithComments()
        {
            const string code = @"
class C
{
    {|span:// Goo
    // Bar|}
    $$event EventHandler E;
}";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Goo ...", autoCollapse: true));
        }
    }
}
