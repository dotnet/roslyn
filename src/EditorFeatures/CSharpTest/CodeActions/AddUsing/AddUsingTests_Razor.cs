// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
public sealed partial class AddUsingTests_Razor : AbstractAddUsingTests
{
    [Theory, CombinatorialData]
    public Task TestAddIntoHiddenRegionWithModernSpanMapper(TestHost host)
        => TestAsync(
            """
            #line hidden
            using System.Collections.Generic;
            #line default

            class Program
            {
                void Main()
                {
                    [|DateTime|] d;
                }
            }
            """,
            """
            #line hidden
            using System;
            using System.Collections.Generic;
            #line default

            class Program
            {
                void Main()
                {
                    DateTime d;
                }
            }
            """, host);

    private protected override IDocumentServiceProvider GetDocumentServiceProvider()
    {
        return new TestDocumentServiceProvider(supportsMappingImportDirectives: true);
    }
}
