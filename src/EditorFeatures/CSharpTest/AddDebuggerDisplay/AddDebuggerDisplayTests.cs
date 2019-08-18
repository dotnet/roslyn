// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddDebuggerDisplay
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddDebuggerDisplay)]
    public sealed class AddDebuggerDisplayTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
        {
            return new CSharpAddDebuggerDisplayCodeRefactoringProvider();
        }

        [Fact]
        public async Task OfferedOnClassWithOverriddenToString()
        {
            await TestInRegularAndScriptAsync(@"
[||]class C
{
    public override string ToString() => ""Foo"";
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{ToString(),nq}"")]
class C
{
    public override string ToString() => ""Foo"";
}");
        }
    }
}
