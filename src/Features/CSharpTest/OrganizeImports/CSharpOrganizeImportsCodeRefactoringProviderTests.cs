// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.OrganizeImports;

using VerifyCS = CSharpCodeRefactoringVerifier<OrganizeImportsCodeRefactoringProvider>;

[UseExportProvider, Trait(Traits.Feature, Traits.Features.CodeActionsOrganizeImports)]
public sealed class CSharpOrganizeImportsCodeRefactoringProviderTests
{
    [Fact]
    public async Task Test1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                [||]using System.Collections;
                using System;

                """,
            FixedCode = """
                using System;
                using System.Collections;

                """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithinNamespace()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    [||]using System.Collections;
                    using System;
                }
                """,
            FixedCode = """
                namespace N
                {
                    using System;
                    using System.Collections;
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWhenAlreadySorted()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                [||]using System;
                using System.Collections;
                """,
            FixedCode = """
                using System;
                using System.Collections;
                """,
        }.RunAsync();
    }
}
