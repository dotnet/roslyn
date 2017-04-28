// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeFieldReadonly
{
    public class MakeFieldReadonlyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpMakeFieldReadonlyDiagnosticAnalyzer(), new CSharpMakeFieldReadonlyCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsPublic()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        public int [|_foo|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsInternal()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        internal int [|_foo|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsReadonly()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private readonly int [|_foo|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldNotAssigned()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private readonly int _foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldNotAssigned_Struct()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    struct MyStruct
    {
        private int [|_foo|];
    }
}",
@"namespace ConsoleApplication1
{
    struct MyStruct
    {
        private readonly int _foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInline()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|] = 0;
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private readonly int _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_AllCanBeReadonly()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|] = 0, _bar = 0;
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int _bar = 0;
        private readonly int _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_OneIsAssignedInMethod()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int _foo = 0, [|_bar|] = 0;
        Foo()
        {
            _foo = 0;
        }
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int _foo = 0;
        private readonly int _bar = 0;
        Foo()
        {
            _foo = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_NoInitializer()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|], _bar = 0;
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int _bar = 0;
        private readonly int _foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
        MyClass()
        {
            _foo = 0;
        }
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private readonly int _foo;
        MyClass()
        {
            _foo = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldReturnedInProperty()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
        int Foo
        {
            get { return _foo; }
        }
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private readonly int _foo;
        int Foo
        {
            get { return _foo; }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
        int Foo
        {
            get { return _foo; }
            set { _foo = value; }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
        int Foo()
        {
            _foo = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethodWithCompoundOperator()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|] = 0;
        int Foo(int value)
        {
            _foo += value;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldUsedWithPostfixIncrement()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|] = 0;
        int Foo(int value)
        {
            _foo++;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldUsedWithPrefixDecrement()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|] = 0;
        int Foo(int value)
        {
            --_foo;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task AssignedInPartialClass()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    partial class MyClass
    {
        private int [|_foo|];
    }
    partial class MyClass
    {
        void SetFoo()
        {
            _foo = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
        void Foo()
        {
            Bar(_foo);
        }
        void Bar(int foo)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsOutParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
        void Foo()
        {
            int.TryParse(""123"", out _foo);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsRefParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int [|_foo|];
        void Foo()
        {
            Bar(ref _foo);
        }
        void Bar(ref int foo)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task StaticFieldAssignedInStaticCtor()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private static int [|_foo|];
        static MyClass()
        {
            _foo = 0;
        }
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private static readonly int _foo;
        static MyClass()
        {
            _foo = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task StaticFieldAssignedInNonStaticCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private static int [|_foo|];
        MyClass()
        {
            _foo = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FixAll()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private int {|FixAllInDocument:_foo|} = 0, _bar = 0;
        private int _fizz = 0;
    }
}",
@"namespace ConsoleApplication1
{
    class MyClass
    {
        private readonly int _bar = 0;
        private readonly int _foo = 0;
        private readonly int _fizz = 0;
    }
}");
        }
    }
}