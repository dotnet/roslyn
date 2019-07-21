// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateOverrides;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateOverrides
{
    public class GenerateOverridesTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateOverridesCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task Test1()
        {
            await TestWithPickMembersDialogAsync(
@"
class C
{
    [||]
}",
@"
class C
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}", new[] { "Equals", "GetHashCode", "ToString" });
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task TestAtEndOfFile()
        {
            await TestWithPickMembersDialogAsync(
@"
class C[||]",
@"
class C
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}
", new[] { "Equals", "GetHashCode", "ToString" });
        }

        [WorkItem(17698, "https://github.com/dotnet/roslyn/issues/17698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task TestRefReturns()
        {
            await TestWithPickMembersDialogAsync(
@"
using System;

class Base
{
    public virtual ref int X() => throw new NotImplementedException();

    public virtual ref int Y => throw new NotImplementedException();

    public virtual ref int this[int i] => throw new NotImplementedException();
}

class Derived : Base
{
     [||]
}",
@"
using System;

class Base
{
    public virtual ref int X() => throw new NotImplementedException();

    public virtual ref int Y => throw new NotImplementedException();

    public virtual ref int this[int i] => throw new NotImplementedException();
}

class Derived : Base
{
    public override ref int this[int i] => ref base[i];

    public override ref int Y => ref base.Y;

    public override ref int X()
    {
        return ref base.X();
    }
}", new[] { "X", "Y", "this[]" });
        }

        [WorkItem(21601, "https://github.com/dotnet/roslyn/issues/21601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task TestMissingInStaticClass1()
        {
            await TestMissingAsync(
@"
static class C
{
    [||]
}");
        }

        [WorkItem(21601, "https://github.com/dotnet/roslyn/issues/21601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task TestMissingInStaticClass2()
        {
            await TestMissingAsync(
@"
static class [||]C
{
    
}");
        }
    }
}
