// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.InlineDeclaration;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDeclaration
{
    public class CSharpInlineDeclarationTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpInlineDeclarationDiagnosticAnalyzer(),
                new CSharpInlineDeclarationCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task InlineVariable1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|int|] i;
        if (int.TryParse(v, out i))
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task InlineVariablePreferVar1()
        {
            await TestAsync(
@"class C
{
    void M(string v)
    {
        [|int|] i;
        if (int.TryParse(v, out i))
        {
        } 
    }
}",
@"class C
{
    void M(string v)
    {
        if (int.TryParse(v, out var i))
        {
        } 
    }
}", options: UseImplicitTypeTests.ImplicitTypeEverywhere());
        }
    }
}