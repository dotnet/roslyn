// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateDefaultConstructors
{
    public class GenerateConstructorForAbstractClassTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateDefaultConstructorsCodeRefactoringProvider();

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    protected B(int x)
    {
    }
}",
@"abstract class C : B
{
    protected C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    protected B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPublicConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    public B(int x)
    {
    }
}",
@"abstract class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    public B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPublicConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    public B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    public B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromInternalConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    internal B(int x)
    {
    }
}",
@"abstract class C : B
{
    internal C(int x) : base(x)
    {
    }
}

abstract class B
{
    internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromInternalConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    internal B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedInternalConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    protected internal B(int x)
    {
    }
}",
@"abstract class C : B
{
    protected internal C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedInternalConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    protected internal B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPrivateProtectedConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    private protected B(int x)
    {
    }
}",
@"abstract class C : B
{
    private protected C(int x) : base(x)
    {
    }
}

abstract class B
{
    private protected B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPrivateProtectedConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    private protected internal B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    private protected internal B(int x)
    {
    }
}");
        }
    }
}
