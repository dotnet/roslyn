// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers.CSharpRemoveUnusedMembersDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers.CSharpRemoveUnusedMembersCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedMembers
{
    public class RemoveUnusedMembersTests
    {
        private async Task TestDiagnosticMissingAsync(string initialMarkup)
            => await VerifyCS.VerifyCodeFixAsync(initialMarkup, initialMarkup);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public void TestStandardProperties()
            => VerifyCS.VerifyStandardProperties();

        [Fact, WorkItem(31582, "https://github.com/dotnet/roslyn/issues/31582")]
        public async Task FieldReadViaSuppression()
        {
            await TestDiagnosticMissingAsync(@"
#nullable enable
class MyClass
{
    string? _field = null;
    public void M()
    {
        _field!.ToString();
    }
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateField(string accessibility)
        {
            await TestDiagnosticMissingAsync(
$@"class MyClass
{{
    {accessibility} int _goo;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateFieldWithConstantInitializer(string accessibility)
        {
            await TestDiagnosticMissingAsync(
$@"class MyClass
{{
    {accessibility} int _goo = 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateFieldWithNonConstantInitializer(string accessibility)
        {
            await TestDiagnosticMissingAsync(
$@"class MyClass
{{
    {accessibility} int _goo = _goo2;
    private static readonly int _goo2 = 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateMethod(string accessibility)
        {
            await TestDiagnosticMissingAsync(
$@"class MyClass
{{
    {accessibility} void M() {{ }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateProperty(string accessibility)
        {
            await TestDiagnosticMissingAsync(
$@"class MyClass
{{
    {accessibility} int P {{ get; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateIndexer(string accessibility)
        {
            var code = $@"class MyClass
{{
    {accessibility}
    int this {{ get {{ return 0; }} set {{ }} }}
}}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(4,9): error CS0548: 'MyClass.this[get]': property or indexer must have at least one accessor
                    DiagnosticResult.CompilerError("CS0548").WithSpan(4, 9, 4, 13).WithArguments("MyClass.this[get]"),
                    // Test0.cs(4,14): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(4, 14, 4, 15),
                    // Test0.cs(4,14): error CS1003: Syntax error, '[' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(4, 14, 4, 15).WithArguments("[", "{"),
                    // Test0.cs(4,16): error CS0246: The type or namespace name 'get' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(4, 16, 4, 19).WithArguments("get"),
                    // Test0.cs(4,20): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(4, 20, 4, 21),
                    // Test0.cs(4,20): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(4, 20, 4, 21).WithArguments(",", "{"),
                    // Test0.cs(4,32): error CS1003: Syntax error, ']' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(4, 32, 4, 33).WithArguments("]", "}"),
                    // Test0.cs(4,32): error CS1514: { expected
                    DiagnosticResult.CompilerError("CS1514").WithSpan(4, 32, 4, 33),
                    // Test0.cs(4,38): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(4, 38, 4, 39).WithArguments("{"),
                    // Test0.cs(4,38): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(4, 38, 4, 39).WithArguments("{"),
                    // Test0.cs(4,42): error CS1022: Type or namespace definition, or end-of-file expected
                    DiagnosticResult.CompilerError("CS1022").WithSpan(4, 42, 4, 43),
                    // Test0.cs(5,1): error CS1022: Type or namespace definition, or end-of-file expected
                    DiagnosticResult.CompilerError("CS1022").WithSpan(5, 1, 5, 2),
                },
                fixedSource: code);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateEvent(string accessibility)
        {
            await TestDiagnosticMissingAsync(
$@"using System;

class MyClass
{{
    {accessibility} event EventHandler RaiseCustomEvent;
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|_goo|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|M|]() => 0;
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|M|]<T>() => 0;
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass<T>
{
    private int [|M|]() => 0;
}",
@"class MyClass<T>
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUnused_NoArguments()
        {
            // We only flag constructors with arguments.
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private MyClass() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUnused_WithArguments()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private [|MyClass|](int i) { }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task StaticConstructorIsNotFlagged()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    static MyClass() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DestructorIsNotFlagged()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    ~MyClass() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private event System.EventHandler [|e|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private static void Main() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_02()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static async Task Main() => await Task.CompletedTask;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_03()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static async Task<int> Main() => await Task.FromResult(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_04()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static Task Main() => Task.CompletedTask;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31572, "https://github.com/dotnet/roslyn/issues/31572")]
        public async Task EntryPointMethodNotFlagged_05()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static int Main() => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnused_ReadOnly()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private readonly int [|_goo|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsUnused_ReadOnly()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|P|] { get; }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsUnused_ReadOnly()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private readonly event System.EventHandler {|CS0106:[|E|]|};
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnused_Static()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private static int [|_goo|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_Static()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private static void {|CS0547:{|CS0548:[|M|]|}|} { }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsUnused_Static()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private static int [|P|] { get { return 0; } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsUnused_Static()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private static int {|CS0106:[|this|]|}[int x] { get { return 0; } set { } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsUnused_Static()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private static event System.EventHandler [|e1|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_Extern()
        {
            await TestDiagnosticMissingAsync(
@"using System.Runtime.InteropServices;

class C
{
    [DllImport(""Assembly.dll"")]
    private static extern void M();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_Abstract()
        {
            var code = @"class C
{
    protected abstract void M();
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                // Test0.cs(3,29): error CS0513: 'C.M()' is abstract but it is contained in non-abstract class 'C'
                DiagnosticResult.CompilerError("CS0513").WithSpan(3, 29, 3, 30).WithArguments("C.M()", "C"),
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_InterfaceMethod()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    void M();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_ExplicitInterfaceImplementation()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    void M();
}

class C : I
{
    void I.M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsUnused_ExplicitInterfaceImplementation()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    int P { get; set; }
}

class C : I
{
    int I.P { get { return 0; } set { } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30965, "https://github.com/dotnet/roslyn/issues/30965")]
        public async Task EventIsUnused_ExplicitInterfaceImplementation()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    event System.Action E;
}

class C : I
{
    event System.Action I.E
    {
        add { }
        remove { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30894, "https://github.com/dotnet/roslyn/issues/30894")]
        public async Task WriteOnlyProperty_NotWritten()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    int [|P|] { set { } }
}",
@"class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30894, "https://github.com/dotnet/roslyn/issues/30894")]
        public async Task WriteOnlyProperty_Written()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    int P { set { } }
    public void M(int i) => P = i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnused_Const()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private const int [|_goo|] = 0;
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ExpressionBody()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockBody()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M() { return _goo; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ExpressionLambda()
        {
            var code = @"class MyClass
{
    private int _goo;
    public void M()
    {
        Func<int> getGoo = () => _goo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                // Test0.cs(6,9): error CS0246: The type or namespace name 'Func<>' could not be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS0246").WithSpan(6, 9, 6, 18).WithArguments("Func<>"),
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockLambda()
        {
            var code = @"class MyClass
{
    private int _goo;
    public void M()
    {
        Func<int> getGoo = () => { return _goo; }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(6,9): error CS0246: The type or namespace name 'Func<>' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(6, 9, 6, 18).WithArguments("Func<>"),
                    // Test0.cs(6,50): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(6, 50, 6, 50),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Delegate()
        {
            var code = @"class MyClass
{
    private int _goo;
    public void M()
    {
        Func<int> getGoo = delegate { return _goo; }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(6,9): error CS0246: The type or namespace name 'Func<>' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(6, 9, 6, 18).WithArguments("Func<>"),
                    // Test0.cs(6,53): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(6, 53, 6, 53),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ExpressionBodyLocalFunction()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M()
    {
        int LocalFunction() => _goo;
        return LocalFunction();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockBodyLocalFunction()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M()
    {
        int LocalFunction() { return _goo; }
        return LocalFunction();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Accessor()
        {
            var code = @"class MyClass
{
    private int _goo;
    public void Goo
    {
        get
        {
            return _goo;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(4,17): error CS0547: 'MyClass.Goo': property or indexer cannot have void type
                    DiagnosticResult.CompilerError("CS0547").WithSpan(4, 17, 4, 20).WithArguments("MyClass.Goo"),
                    // Test0.cs(8,13): error CS0127: Since 'MyClass.Goo.get' returns void, a return keyword must not be followed by an object expression
                    DiagnosticResult.CompilerError("CS0127").WithSpan(8, 13, 8, 19).WithArguments("MyClass.Goo.get"),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Deconstruction()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public void M(int x)
    {
        var y = (_goo, x);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_DifferentInstance()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M() => new MyClass()._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ObjectInitializer()
        {
            await TestDiagnosticMissingAsync(
@"
class C
{
    public int F;
}
class MyClass
{
    private int _goo;
    public C M() => new C() { F = _goo };
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ThisInstance()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M() => this._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Attribute()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private const string _goo = """";

    [System.Obsolete(_goo)]
    public void M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsInvoked()
        {
            var code = @"class MyClass
{
    private int M1 => 0
    public int M2() => M1();
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(3,24): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(3, 24, 3, 24),
                    // Test0.cs(4,24): error CS1955: Non-invocable member 'MyClass.M1' cannot be used like a method.
                    DiagnosticResult.CompilerError("CS1955").WithSpan(4, 24, 4, 26).WithArguments("MyClass.M1"),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsAddressTaken()
        {
            var code = @"class MyClass
{
    private int M1 => 0
    public void M2()
    {
        System.Func<int> m1 = M1;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(3,24): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(3, 24, 3, 24),
                    // Test0.cs(6,31): error CS0029: Cannot implicitly convert type 'int' to 'System.Func<int>'
                    DiagnosticResult.CompilerError("CS0029").WithSpan(6, 31, 6, 33).WithArguments("int", "System.Func<int>"),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsInvoked_ExplicitTypeArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int M1<T>() => 0;
    public int M2() => M1<int>();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsInvoked_ImplicitTypeArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private T M1<T>(T t) => t;
    public int M2() => M1(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NoTypeArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private int M1() => 0;
    public int M2() => M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NonConstructedType()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private int M1() => 0;
    public int M2(MyClass<T> m) => m.M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_ConstructedType()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private int M1() => 0;
    public int M2(MyClass<int> m) => m.M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_NoArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private MyClass() { }
    public static readonly MyClass Instance = new MyClass();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_WithArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private MyClass(int i) { }
    public static readonly MyClass Instance = new MyClass(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsRead()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int P => 0;
    public int M() => P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsRead()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M(int x) => this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsRead()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class MyClass
{
    private event EventHandler e;
    public EventHandler P => e;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsSubscribed()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class MyClass
{
    private event EventHandler e;
    public void M()
    {
        e += MyHandler;
    }

    static void MyHandler(object sender, EventArgs e)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsRaised()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class MyClass
{
    private event EventHandler _eventHandler;

    public void RaiseEvent(EventArgs e)
    {
        _eventHandler(this, e);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32488, "https://github.com/dotnet/roslyn/issues/32488")]
        public async Task FieldInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public string _goo2 = nameof(_goo);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33765, "https://github.com/dotnet/roslyn/issues/33765")]
        public async Task GenericFieldInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private T _goo;
    public string _goo2 = nameof(MyClass<int>._goo);
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")]
        public async Task MethodInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private void M() { }
    private string _goo = nameof(M);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33765, "https://github.com/dotnet/roslyn/issues/33765")]
        public async Task GenericMethodInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private void M() { }
    private string _goo2 = nameof(MyClass<int>.M);
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")]
        public async Task PropertyInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int P { get; }
    public string _goo = nameof(P);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicInvocation()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private void M(dynamic d) { }
    public void M2(dynamic d) => M(d);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicObjectCreation()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private MyClass(int i) { }
    public static MyClass Create(dynamic d) => new MyClass(d);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicIndexerAccess()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int[] _list;
    private int this[int index] => _list[index];
    public int M2(dynamic d) => this[d];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment()
        {
            await TestDiagnosticMissingAsync(
@"
/// <summary>
/// <see cref=""C._goo""/>
/// </summary>
class C
{
    private static int {|IDE0052:_goo|};
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment_02()
        {
            await TestDiagnosticMissingAsync(
@"
class C
{
    /// <summary>
    /// <see cref=""_goo""/>
    /// </summary>
    private static int {|IDE0052:_goo|};
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment_03()
        {
            await TestDiagnosticMissingAsync(
@"
class C
{
    /// <summary>
    /// <see cref=""_goo""/>
    /// </summary>
    public void M() { }

    private static int {|IDE0052:_goo|};
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33994, "https://github.com/dotnet/roslyn/issues/33994")]
        public async Task PropertyIsOnlyWritten()
        {
            var source =
@"class MyClass
{
    private int {|IDE0052:P|} { get; set; }
    public void M()
    {
        P = 0;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                ExpectedDiagnostics =
                {
                },
                FixedCode = source,
            }.RunAsync();
            //var testParameters = new TestParameters(retainNonFixableDiagnostics: true);
            //using var workspace = CreateWorkspaceFromOptions(source, testParameters);
            //var diagnostics = await GetDiagnosticsAsync(workspace, testParameters).ConfigureAwait(false);
            //diagnostics.Verify(Diagnostic("IDE0052", "P").WithLocation(3, 17));
            //var expectedMessage = string.Format(FeaturesResources.Private_property_0_can_be_converted_to_a_method_as_its_get_accessor_is_never_invoked, "MyClass.P");
            //Assert.Equal(expectedMessage, diagnostics.Single().GetMessage());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsOnlyWritten()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
    public void M(int x, int y)
    {
        this[x] = y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsOnlyWritten()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private event System.EventHandler e { add { } remove { } }
    public void M()
    {
        // CS0079: The event 'MyClass.e' can only appear on the left hand side of += or -=
        {|CS0079:e|} = null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyInitialized_NonConstant()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|} = M();
    public static int M() => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_Deconstruction()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M()
    {
        int x;
        (_goo, x) = (0, 0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_ObjectInitializer()
        {
            await TestDiagnosticMissingAsync(
@"
class MyClass
{
    private int {|IDE0052:_goo|};
    public MyClass M() => new MyClass() { _goo = 0 };
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_InProperty()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public int Goo
    {
        get { return 0; }
        set { _goo = value; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsReadAndWritten()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public void M()
    {
        _goo = 0;
        System.Console.WriteLine(_goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsReadAndWritten()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int P { get; set; }
    public void M()
    {
        P = 0;
        System.Console.WriteLine(P);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsReadAndWritten()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public void M(int x)
    {
        this[x] = 0;
        System.Console.WriteLine(this[x]);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsReadAndWritten_InProperty()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int Goo
    {
        get { return _goo; }
        set { _goo = value; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30397, "https://github.com/dotnet/roslyn/issues/30397")]
        public async Task FieldIsIncrementedAndValueUsed()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M1() => ++_goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30397, "https://github.com/dotnet/roslyn/issues/30397")]
        public async Task FieldIsIncrementedAndValueUsed_02()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M1() { return ++_goo; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueDropped()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1() => ++_goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueDropped_02()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1() { ++_goo; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsIncrementedAndValueUsed()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int P { get; set; }
    public int M1() => ++P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsIncrementedAndValueDropped()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:P|} { get; set; }
    public void M1() { ++P; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsIncrementedAndValueUsed()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M1(int x) => ++this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsIncrementedAndValueDropped()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
    public void M1(int x) => ++this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M1(int x) => _goo += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed_02()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M1(int x) { return _goo += x; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1(int x) => _goo += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped_02()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1(int x) { _goo += x; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsTargetOfCompoundAssignmentAndValueUsed()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int P { get; set; }
    public int M1(int x) => P += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsTargetOfCompoundAssignmentAndValueDropped()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:P|} { get; set; }
    public void M1(int x) { P += x; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsTargetOfCompoundAssignmentAndValueUsed()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M1(int x, int y) => this[x] += y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsTargetOfCompoundAssignmentAndValueDropped()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
    public void M1(int x, int y) => this[x] += y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfAssignmentAndParenthesized()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1(int x) => (_goo) = x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfAssignmentAndHasImplicitConversion()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public static implicit operator int(MyClass c) => 0;
    public void M1(MyClass c) => _goo = c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsArg()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1() => M2(_goo);
    public int M2(int i) => { i = 0; return i; }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(5,29): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(5, 29, 5, 30),
                    // Test0.cs(5,29): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 29, 5, 30).WithArguments("{"),
                    // Test0.cs(5,29): error CS1525: Invalid expression term '{'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(5, 29, 5, 30).WithArguments("{"),
                    // Test0.cs(5,33): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 33, 5, 34).WithArguments("="),
                    // Test0.cs(5,33): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 33, 5, 34).WithArguments("="),
                    // Test0.cs(5,46): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 46, 5, 47).WithArguments(";"),
                    // Test0.cs(5,46): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 46, 5, 47).WithArguments(";"),
                    // Test0.cs(6,1): error CS1022: Type or namespace definition, or end-of-file expected
                    DiagnosticResult.CompilerError("CS1022").WithSpan(6, 1, 6, 2),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsInArg()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1() => M2(_goo);
    public int M2(in int i) => { i = 0; return i; }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(5,32): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(5, 32, 5, 33),
                    // Test0.cs(5,32): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 32, 5, 33).WithArguments("{"),
                    // Test0.cs(5,32): error CS1525: Invalid expression term '{'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(5, 32, 5, 33).WithArguments("{"),
                    // Test0.cs(5,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 36, 5, 37).WithArguments("="),
                    // Test0.cs(5,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 36, 5, 37).WithArguments("="),
                    // Test0.cs(5,49): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 49, 5, 50).WithArguments(";"),
                    // Test0.cs(5,49): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(5, 49, 5, 50).WithArguments(";"),
                    // Test0.cs(6,1): error CS1022: Type or namespace definition, or end-of-file expected
                    DiagnosticResult.CompilerError("CS1022").WithSpan(6, 1, 6, 2),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRefArg()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M1() => M2(ref _goo);
    public int M2(ref int i) => i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOutArg()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public int M1() => M2(out _goo);
    public int M2(out int i) { i = 0; return i; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsArg()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int M() => 0;
    public int M1() => M2(M);
    public int M2(System.Func<int> m) => m();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsArg()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int P => 0;
    public int M1() => M2(P);
    public int M2(int p) => p;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsArg()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M1(int x) => M2(this[x]);
    public int M2(int p) => p;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsArg()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class MyClass
{
    private event EventHandler _e;
    public EventHandler M1() => M2(_e);
    public EventHandler M2(EventHandler e) => e;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MultipleFields_AllUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|_goo|] = 0, [|_bar|] = 0;
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MultipleFields_AllUnused_02()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|_goo|] = 0, [|_bar|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MultipleFields_SomeUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|_goo|] = 0, _bar = 0;
    public int M() => _bar;
}",
@"class MyClass
{
    private int _bar = 0;
    public int M() => _bar;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MultipleFields_SomeUnused_02()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int _goo = 0, [|_bar|] = 0;
    public int M() => _goo;
}",
@"class MyClass
{
    private int _goo = 0;
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_InNestedType()
        {
            var code = @"class MyClass
{
    private int _goo;

    class Derived : MyClass
    {
        public in M() => _goo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(7,16): error CS1519: Invalid token 'in' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(7, 16, 7, 18).WithArguments("in"),
                    // Test0.cs(7,16): error CS1519: Invalid token 'in' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(7, 16, 7, 18).WithArguments("in"),
                    // Test0.cs(7,19): error CS1520: Method must have a return type
                    DiagnosticResult.CompilerError("CS1520").WithSpan(7, 19, 7, 20),
                    // Test0.cs(7,26): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                    DiagnosticResult.CompilerError("CS0201").WithSpan(7, 26, 7, 30),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsInvoked_InNestedType()
        {
            var code = @"class MyClass
{
    private int M1() => 0;

    class Derived : MyClass
    {
        public in M2() => M1();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(7,16): error CS1519: Invalid token 'in' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(7, 16, 7, 18).WithArguments("in"),
                    // Test0.cs(7,16): error CS1519: Invalid token 'in' in class, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan(7, 16, 7, 18).WithArguments("in"),
                    // Test0.cs(7,19): error CS1520: Method must have a return type
                    DiagnosticResult.CompilerError("CS1520").WithSpan(7, 19, 7, 21),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldOfNestedTypeIsUnused()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    class NestedType
    {
        private int [|_goo|];
    }
}",
@"class MyClass
{
    class NestedType
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldOfNestedTypeIsRead()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    class NestedType
    {
        private int _goo;

        public int M() => _goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnused_PartialClass()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"partial class MyClass
{
    private int [|_goo|];
}",
@"partial class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_PartialClass()
        {
            await TestDiagnosticMissingAsync(
@"partial class MyClass
{
    private int _goo;
}
partial class MyClass
{
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_PartialClass_DifferentFile()
        {
            var source1 = @"partial class MyClass
{
    private int _goo;
}";
            var source2 = @"partial class MyClass
{
    public int M() => _goo;
}";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source1, source2 } },
                FixedState = { Sources = { source1, source2 } },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_PartialClass_DifferentFile()
        {
            var source1 = @"partial class MyClass
{
    private int {|IDE0052:_goo|};
}";
            var source2 = @"partial class MyClass
{
    public void M() { _goo = 0; }
}";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source1, source2 } },
                FixedState = { Sources = { source1, source2 } },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_InParens()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int _goo;
    public int M() => (_goo);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsWritten_InParens()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M() { (_goo) = 1; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsWritten_InParens_02()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int {|IDE0052:_goo|};
    public int M() => (_goo) = 1;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_InDeconstruction_InParens()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private int i;

    public void M()
    {
        var x = ((i, 0), 0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInTypeWithGeneratedCode()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    private int [|i|];

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int j;

    public void M()
    {
    }
}",
@"class C
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int j;

    public void M()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsGeneratedCode()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int i;

    public void M()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldUsedInGeneratedCode()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private int i;

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    public int M() => i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SyntaxError()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private int i;

    public int M() { return {|CS1525:=|} {|CS1525:;|} }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SemanticError()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private int i;

    // 'ii' is undefined.
    public int M() => {|CS0103:ii|};
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SemanticErrorInDifferentType()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    private int [|i|];
}

class C2
{
    // 'ii' is undefined.
    public int M() => {|CS0103:ii|};
}",
@"class C
{
}

class C2
{
    // 'ii' is undefined.
    public int M() => {|CS0103:ii|};
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task StructLayoutAttribute_ExplicitLayout()
        {
            await TestDiagnosticMissingAsync(
@"using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Explicit)]
class C
{
    [FieldOffset(0)]
    private int i;

    [FieldOffset(4)]
    private int i2;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task StructLayoutAttribute_SequentialLayout()
        {
            await TestDiagnosticMissingAsync(
@"using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
struct S
{
    private int i;
    private int i2;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesField()
        {
            await TestDiagnosticMissingAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{s}"")]
class C
{
    private string s;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesMethod()
        {
            await TestDiagnosticMissingAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{GetString()}"")]
class C
{
    private string GetString() => """";
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesProperty()
        {
            await TestDiagnosticMissingAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{MyString}"")]
class C
{
    private string MyString => """";
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnField_ReferencesField()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private string s;

    [System.Diagnostics.DebuggerDisplayAttribute(""{s}"")]
    public int M;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnProperty_ReferencesMethod()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private string GetString() => """";

    [System.Diagnostics.DebuggerDisplayAttribute(""{GetString()}"")]
    public int M => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnProperty_ReferencesProperty()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private string MyString { get { return """"; } }

    [System.Diagnostics.DebuggerDisplayAttribute(""{MyString}"")]
    public int M { get { return 0; } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnNestedTypeMember_ReferencesField()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private static string s;

    class Nested
    {
        [System.Diagnostics.DebuggerDisplayAttribute(""{C.s}"")]
        public int M;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30886, "https://github.com/dotnet/roslyn/issues/30886")]
        public async Task SerializableConstructor_TypeImplementsISerializable()
        {
            await TestDiagnosticMissingAsync(
@"using System.Runtime.Serialization;

class C : ISerializable
{
    public C()
    {
    }

    private C(SerializationInfo info, StreamingContext context)
    {
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30886, "https://github.com/dotnet/roslyn/issues/30886")]
        public async Task SerializableConstructor_BaseTypeImplementsISerializable()
        {
            await TestDiagnosticMissingAsync(
@"using System;
using System.Runtime.Serialization;

class C : Exception 
{
    public C()
    {
    }

    private C(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
    }
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData(@"[System.Runtime.Serialization.OnDeserializingAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnDeserializedAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnSerializingAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnSerializedAttribute]")]
        [InlineData(@"[System.Runtime.InteropServices.ComRegisterFunctionAttribute]")]
        [InlineData(@"[System.Runtime.InteropServices.ComUnregisterFunctionAttribute]")]
        public async Task MethodsWithSpecialAttributes(string attribute)
        {
            await TestDiagnosticMissingAsync(
$@"class C
{{
    {attribute}
    private void M()
    {{
    }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30887, "https://github.com/dotnet/roslyn/issues/30887")]
        public async Task ShouldSerializePropertyMethod()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private bool ShouldSerializeData()
    {
        return true;
    }

    public int Data { get; private set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(38491, "https://github.com/dotnet/roslyn/issues/38491")]
        public async Task ResetPropertyMethod()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    private void ResetData()
    {
        return;
    }

    public int Data { get; private set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30377, "https://github.com/dotnet/roslyn/issues/30377")]
        public async Task EventHandlerMethod()
        {
            await TestDiagnosticMissingAsync(
$@"using System;

class C
{{
    private void M(object o, EventArgs args)
    {{
    }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32727, "https://github.com/dotnet/roslyn/issues/32727")]
        public async Task NestedStructLayoutTypeWithReference()
        {
            await TestDiagnosticMissingAsync(
@"using System.Runtime.InteropServices;

class Program
{
    private const int MAX_PATH = 260;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessEntry32
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string szExeFile;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FixAllFields_Document()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|_goo|] = 0, [|_bar|];
    private int [|_x|] = 0, [|_y|], _z = 0;
    private string [|_fizz|] = null;

    public int Method() => _z;
}",
@"class MyClass
{
    private int _z = 0;

    public int Method() => _z;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FixAllMethods_Document()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|M1|]() => 0;
    private void [|M2|]() { }
    private static void [|M3|]() { }
    private class NestedClass
    {
        private void [|M4|]() { }
    }
}",
@"class MyClass
{
    private class NestedClass
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FixAllProperties_Document()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class MyClass
{
    private int [|P1|] => 0;
    private int [|P2|] { get; set; }
    private int [|P3|] { get { return 0; } set { } }
    private int [|this|][int i] { get { return 0; } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FixAllEvents_Document()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

class MyClass
{
    private event EventHandler [|E1|], E2 = null, [|E3|];
    private event EventHandler [|E4|], [|E5|] = null;
    private event EventHandler [|E|]
    {
        add { }
        remove { }
    }

    public void M()
    {
        EventHandler handler = E2;
    }
}",
@"using System;

class MyClass
{
    private event EventHandler E2 = null;

    public void M()
    {
        EventHandler handler = E2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FixAllMembers_Project()
        {
            var source1 = @"
using System;

partial class MyClass
{
    private int [|f1|], f2 = 0, [|f3|];
    private void [|M1|]() { }
    private int [|P1|] => 0;
    private int [|this|][int x] { get { return 0; } set { } }
    private event EventHandler [|e1|], [|e2|] = null;
}

class MyClass2
{
    private void [|M2|]() { }
}";
            var source2 = @"
partial class MyClass
{
    private void [|M3|]() { }
    public int M4() => f2;
}

static class MyClass3
{
    private static void [|M5|]() { }
}";
            var fixedSource1 = @"
using System;

partial class MyClass
{
    private int f2 = 0;
}

class MyClass2
{
}";
            var fixedSource2 = @"
partial class MyClass
{
    public int M4() => f2;
}

static class MyClass3
{
}";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source1, source2 } },
                FixedState = { Sources = { fixedSource1, fixedSource2 } },
                NumberOfFixAllInDocumentIterations = 2,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32702, "https://github.com/dotnet/roslyn/issues/32702")]
        public async Task UsedExtensionMethod_ReferencedFromPartialMethod()
        {
            var source1 = @"
static partial class B
{
    public static void Entry() => PartialMethod();
    static partial void PartialMethod();
}";
            var source2 = @"
static partial class B
{
    static partial void PartialMethod()
    {
        UsedMethod();
    }

    private static void UsedMethod() { }
}";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source1, source2 } },
                FixedState = { Sources = { source1, source2 } },
            }.RunAsync();
        }

        [Fact, WorkItem(32842, "https://github.com/dotnet/roslyn/issues/32842")]
        public async Task FieldIsRead_NullCoalesceAssignment()
        {
            await TestDiagnosticMissingAsync(@"
public class MyClass
{
    private MyClass _field;
    public MyClass Property => _field ??= new MyClass();
}");
        }

        [Fact, WorkItem(32842, "https://github.com/dotnet/roslyn/issues/32842")]
        public async Task FieldIsNotRead_NullCoalesceAssignment()
        {
            await TestDiagnosticMissingAsync(@"
public class MyClass
{
    private MyClass {|IDE0052:_field|};
    public void M() => _field ??= new MyClass();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(37213, "https://github.com/dotnet/roslyn/issues/37213")]
        public async Task UsedPrivateExtensionMethod()
        {
            await TestDiagnosticMissingAsync(
@"public static class B
{
    public static void PublicExtensionMethod(this string s) => s.PrivateExtensionMethod();
    private static void PrivateExtensionMethod(this string s) { }
}");
        }
    }
}
