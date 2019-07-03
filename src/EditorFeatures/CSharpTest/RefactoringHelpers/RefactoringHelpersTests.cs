// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.RefactoringHelpers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RefactoringHelpers
{
    public partial class RefactoringHelpersTests : RefactoringHelpersTestBase<CSharpTestWorkspaceFixture>
    {
        public RefactoringHelpersTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        [Fact]
        public async Task Test1()
        {
            var testText = @"
class C
{
    void M()
    {
        [||]{|result:C LocalFunction(C c)
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        public async Task Test2()
        {
            var testText = @"
class C
{
    void M()
    {
        C LocalFunction(C c)
        [|{
            return null;
        }|]
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

    }
}
