// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateDeconstructMethod;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateDeconstructMethod
{
    public class GenerateDeconstructMethodTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public GenerateDeconstructMethodTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new GenerateDeconstructMethodCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_Simple()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        (int x, int y) = [|this|];
    }
}",
@"using System;

class Class
{
    private void Deconstruct(out int x, out int y)
    {
        throw new NotImplementedException();
    }

    void Method()
    {
        (int x, int y) = this;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_Simple_Record()
        {
            await TestInRegularAndScriptAsync(
@"record R
{
    void Method()
    {
        (int x, int y) = [|this|];
    }
}",
@"using System;

record R
{
    private void Deconstruct(out int x, out int y)
    {
        throw new NotImplementedException();
    }

    void Method()
    {
        (int x, int y) = this;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_TypeParameters()
        {
            await TestInRegularAndScriptAsync(
@"class Class<T>
{
    void Method<U>()
    {
        (T x, U y) = [|this|];
    }
}",
@"using System;

class Class<T>
{
    private void Deconstruct(out T x, out object y)
    {
        throw new NotImplementedException();
    }

    void Method<U>()
    {
        (T x, U y) = this;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_OtherDeconstructMethods()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        (int x, int y) = [|this|];
    }
    void Deconstruct(out int x) => throw null;
    void Deconstruct(out int x, out int y, out int z) => throw null;
}",
@"using System;

class Class
{
    void Method()
    {
        (int x, int y) = this;
    }
    void Deconstruct(out int x) => throw null;
    void Deconstruct(out int x, out int y, out int z) => throw null;

    private void Deconstruct(out int x, out int y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_AlreadySuccessfull()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        (int x, int y) = [|this|];
    }
    void Deconstruct(out int x, out int y) => throw null;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_UndeterminedType()
        {
            await TestInRegularAndScript1Async(
@"class Class
{
    void Method()
    {
        (var x, var y) = [|this|];
    }
}",
@"using System;

class Class
{
    private void Deconstruct(out object x, out object y)
    {
        throw new NotImplementedException();
    }

    void Method()
    {
        (var x, var y) = this;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_UndeterminedType2()
        {
            await TestInRegularAndScript1Async(
@"class Class
{
    void Method()
    {
        var (x, y) = [|this|];
    }
}",
@"using System;

class Class
{
    private void Deconstruct(out object x, out object y)
    {
        throw new NotImplementedException();
    }

    void Method()
    {
        var (x, y) = this;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionDeclaration_BuiltinType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        (int x, int y) = [|1|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        int x, y;
        (x, y) = [|this|];
    }
}",
@"using System;

class Class
{
    private void Deconstruct(out int x, out int y)
    {
        throw new NotImplementedException();
    }

    void Method()
    {
        int x, y;
        (x, y) = this;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionAssignment_Nested()
        {
            // We only offer a fix for non-nested deconstruction, at the moment
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        int x, y, z;
        ((x, y), z) = ([|this|], 0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionAssignment_Array()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        int x, y;
        (x, y) = [|new[] { this }|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleDeconstructionForeach()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        foreach ((int x, int y) in new[] { [|this|] }) { }
    }
}",
@"using System;

class Class
{
    private void Deconstruct(out int x, out int y)
    {
        throw new NotImplementedException();
    }

    void Method()
    {
        foreach ((int x, int y) in new[] { this }) { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleDeconstructionForeach_AnotherType()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(D d)
    {
        foreach ((int x, int y) in new[] { [|d|] }) { }
    }
}
class D
{
}",
@"using System;

class Class
{
    void Method(D d)
    {
        foreach ((int x, int y) in new[] { d }) { }
    }
}
class D
{
    internal void Deconstruct(out int x, out int y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(32510, "https://github.com/dotnet/roslyn/issues/32510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDeconstructionAssignment_InvalidDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class C
{
    void Method()
    {
        var stuff = new Dictionary<string, string>();
        foreach ((key, value) in [|stuff|]) // Invalid variable declarator syntax
        {
        }
    }
}");
        }
    }
}
