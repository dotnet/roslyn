// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public void TestStandardProperties()
            => VerifyCS.VerifyStandardProperties();

        [Fact, WorkItem(31582, "https://github.com/dotnet/roslyn/issues/31582")]
        public async Task FieldReadViaSuppression()
        {
            var code = @"
#nullable enable
class MyClass
{
    string? _field = null;
    public void M()
    {
        _field!.ToString();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateField(string accessibility)
        {
            var code = $@"class MyClass
{{
    {accessibility} int _goo;
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateFieldWithConstantInitializer(string accessibility)
        {
            var code = $@"class MyClass
{{
    {accessibility} int _goo = 0;
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateFieldWithNonConstantInitializer(string accessibility)
        {
            var code = $@"class MyClass
{{
    {accessibility} int _goo = _goo2;
    private static readonly int _goo2 = 0;
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateMethod(string accessibility)
        {
            var code = $@"class MyClass
{{
    {accessibility} void M() {{ }}
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateProperty(string accessibility)
        {
            var code = $@"class MyClass
{{
    {accessibility} int P {{ get; }}
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
    int this[int arg] {{ get {{ return 0; }} set {{ }} }}
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateEvent(string accessibility)
        {
            var code = $@"using System;

class MyClass
{{
    {accessibility} event EventHandler RaiseCustomEvent;
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class MyClass
{
    private MyClass() { }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class MyClass
{
    static MyClass() { }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DestructorIsNotFlagged()
        {
            var code = @"class MyClass
{
    ~MyClass() { }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class MyClass
{
    private static void Main() { }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_02()
        {
            var code = @"using System.Threading.Tasks;

class MyClass
{
    private static async Task Main() => await Task.CompletedTask;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_03()
        {
            var code = @"using System.Threading.Tasks;

class MyClass
{
    private static async Task<int> Main() => await Task.FromResult(0);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_04()
        {
            var code = @"using System.Threading.Tasks;

class MyClass
{
    private static Task Main() => Task.CompletedTask;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31572, "https://github.com/dotnet/roslyn/issues/31572")]
        public async Task EntryPointMethodNotFlagged_05()
        {
            var code = @"using System.Threading.Tasks;

class MyClass
{
    private static int Main() => 0;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_06()
        {
            var code = @"
return 0;
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ExpectedDiagnostics =
                {
                    // error CS8805: Program using top-level statements must be an executable.
                    DiagnosticResult.CompilerError("CS8805"),
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_07()
        {
            var code = @"
return 0;
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code, code },
                },
                FixedState =
                {
                    Sources = { code, code },
                },
                ExpectedDiagnostics =
                {
                    // error CS8805: Program using top-level statements must be an executable.
                    DiagnosticResult.CompilerError("CS8805"),
                    // /0/Test1.cs(2,1): error CS8802: Only one compilation unit can have top-level statements.
                    DiagnosticResult.CompilerError("CS8802").WithSpan("/0/Test1.cs", 2, 1, 2, 7),
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_08()
        {
            var code = @"
return 0;
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var project = solution.GetRequiredProject(projectId);
                        var compilationOptions = project.CompilationOptions;
                        return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithOutputKind(OutputKind.ConsoleApplication));
                    },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_09()
        {
            var code = @"
return 0;
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code, code },
                },
                FixedState =
                {
                    Sources = { code, code },
                },
                ExpectedDiagnostics =
                {
                    // /0/Test1.cs(2,1): error CS8802: Only one compilation unit can have top-level statements.
                    DiagnosticResult.CompilerError("CS8802").WithSpan("/0/Test1.cs", 2, 1, 2, 7),
                },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var project = solution.GetRequiredProject(projectId);
                        var compilationOptions = project.CompilationOptions;
                        return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithOutputKind(OutputKind.ConsoleApplication));
                    },
                },
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
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
    // error CS0106: The modifier 'readonly' is not valid for this item
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
    private static void [|M|]() { }
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
    // error CS0106: The modifier 'static' is not valid for this item
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
            var code = @"using System.Runtime.InteropServices;

class C
{
    [DllImport(""Assembly.dll"")]
    private static extern void M();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_Abstract()
        {
            var code = @"abstract class C
{
    protected abstract void M();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_InterfaceMethod()
        {
            var code = @"interface I
{
    void M();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_ExplicitInterfaceImplementation()
        {
            var code = @"interface I
{
    void M();
}

class C : I
{
    void I.M() { }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsUnused_ExplicitInterfaceImplementation()
        {
            var code = @"interface I
{
    int P { get; set; }
}

class C : I
{
    int I.P { get { return 0; } set { } }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30965, "https://github.com/dotnet/roslyn/issues/30965")]
        public async Task EventIsUnused_ExplicitInterfaceImplementation()
        {
            var code = @"interface I
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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class C
{
    int P { set { } }
    public void M(int i) => P = i;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class MyClass
{
    private int _goo;
    public int M() => _goo;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockBody()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M() { return _goo; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ExpressionLambda()
        {
            var code = @"using System;
class MyClass
{
    private int _goo;
    public void M()
    {
        Func<int> getGoo = () => _goo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockLambda()
        {
            var code = @"using System;
class MyClass
{
    private int _goo;
    public void M()
    {
        Func<int> getGoo = () => { return _goo; };
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Delegate()
        {
            var code = @"using System;
class MyClass
{
    private int _goo;
    public void M()
    {
        Func<int> getGoo = delegate { return _goo; };
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ExpressionBodyLocalFunction()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M()
    {
        int LocalFunction() => _goo;
        return LocalFunction();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockBodyLocalFunction()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M()
    {
        int LocalFunction() { return _goo; }
        return LocalFunction();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Accessor()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int Goo
    {
        get
        {
            return _goo;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Deconstruction()
        {
            var code = @"class MyClass
{
    private int _goo;
    public void M(int x)
    {
        var y = (_goo, x);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_DifferentInstance()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M() => new MyClass()._goo;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ObjectInitializer()
        {
            var code = @"
class C
{
    public int F;
}
class MyClass
{
    private int _goo;
    public C M() => new C() { F = _goo };
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ThisInstance()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M() => this._goo;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Attribute()
        {
            var code = @"class MyClass
{
    private const string _goo = """";

    [System.Obsolete(_goo)]
    public void M() { }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsInvoked()
        {
            var code = @"class MyClass
{
    private int M1() => 0;
    public int M2() => M1();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsAddressTaken()
        {
            var code = @"class MyClass
{
    private int M1() => 0;
    public void M2()
    {
        System.Func<int> m1 = M1;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsInvoked_ExplicitTypeArguments()
        {
            var code = @"class MyClass
{
    private int M1<T>() => 0;
    public int M2() => M1<int>();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsInvoked_ImplicitTypeArguments()
        {
            var code = @"class MyClass
{
    private T M1<T>(T t) => t;
    public int M2() => M1(0);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NoTypeArguments()
        {
            var code = @"class MyClass<T>
{
    private int M1() => 0;
    public int M2() => M1();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NonConstructedType()
        {
            var code = @"class MyClass<T>
{
    private int M1() => 0;
    public int M2(MyClass<T> m) => m.M1();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_ConstructedType()
        {
            var code = @"class MyClass<T>
{
    private int M1() => 0;
    public int M2(MyClass<int> m) => m.M1();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_NoArguments()
        {
            var code = @"class MyClass
{
    private MyClass() { }
    public static readonly MyClass Instance = new MyClass();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_WithArguments()
        {
            var code = @"class MyClass
{
    private MyClass(int i) { }
    public static readonly MyClass Instance = new MyClass(0);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsRead()
        {
            var code = @"class MyClass
{
    private int P => 0;
    public int M() => P;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsRead()
        {
            var code = @"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M(int x) => this[x];
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsRead()
        {
            var code = @"using System;

class MyClass
{
    private event EventHandler e;
    public EventHandler P => e;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsSubscribed()
        {
            var code = @"using System;

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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsRaised()
        {
            var code = @"using System;

class MyClass
{
    private event EventHandler _eventHandler;

    public void RaiseEvent(EventArgs e)
    {
        _eventHandler(this, e);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32488, "https://github.com/dotnet/roslyn/issues/32488")]
        public async Task FieldInNameOf()
        {
            var code = @"class MyClass
{
    private int _goo;
    public string _goo2 = nameof(_goo);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33765, "https://github.com/dotnet/roslyn/issues/33765")]
        public async Task GenericFieldInNameOf()
        {
            var code = @"class MyClass<T>
{
    private T _goo;
    public string _goo2 = nameof(MyClass<int>._goo);
}
";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")]
        public async Task MethodInNameOf()
        {
            var code = @"class MyClass
{
    private void M() { }
    private string _goo = nameof(M);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33765, "https://github.com/dotnet/roslyn/issues/33765")]
        public async Task GenericMethodInNameOf()
        {
            var code = @"class MyClass<T>
{
    private void M() { }
    private string _goo2 = nameof(MyClass<int>.M);
}
";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")]
        public async Task PropertyInNameOf()
        {
            var code = @"class MyClass
{
    private int P { get; }
    public string _goo = nameof(P);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicInvocation()
        {
            var code = @"class MyClass
{
    private void M(dynamic d) { }
    public void M2(dynamic d) => M(d);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicObjectCreation()
        {
            var code = @"class MyClass
{
    private MyClass(int i) { }
    public static MyClass Create(dynamic d) => new MyClass(d);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicIndexerAccess()
        {
            var code = @"class MyClass
{
    private int[] _list;
    private int this[int index] => _list[index];
    public int M2(dynamic d) => this[d];
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment()
        {
            var code = @"
/// <summary>
/// <see cref=""C._goo""/>
/// </summary>
class C
{
    private static int {|IDE0052:_goo|};
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment_02()
        {
            var code = @"
class C
{
    /// <summary>
    /// <see cref=""_goo""/>
    /// </summary>
    private static int {|IDE0052:_goo|};
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment_03()
        {
            var code = @"
class C
{
    /// <summary>
    /// <see cref=""_goo""/>
    /// </summary>
    public void M() { }

    private static int {|IDE0052:_goo|};
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M()
    {
        _goo = 0;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33994, "https://github.com/dotnet/roslyn/issues/33994")]
        public async Task PropertyIsOnlyWritten()
        {
            var source =
@"class MyClass
{
    private int P { get; set; }
    public void M()
    {
        P = 0;
    }
}";

            var descriptor = new CSharpRemoveUnusedMembersDiagnosticAnalyzer().SupportedDiagnostics.First(x => x.Id == "IDE0052");
            var expectedMessage = string.Format(AnalyzersResources.Private_property_0_can_be_converted_to_a_method_as_its_get_accessor_is_never_invoked, "MyClass.P");

            await new VerifyCS.Test
            {
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // Test0.cs(3,17): info IDE0052: Private property 'MyClass.P' can be converted to a method as its get accessor is never invoked.
                    VerifyCS.Diagnostic(descriptor).WithMessage(expectedMessage).WithSpan(3, 17, 3, 18),
                },
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsOnlyWritten()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
    public void M(int x, int y)
    {
        this[x] = y;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsOnlyWritten()
        {
            var code = @"class MyClass
{
    private event System.EventHandler e { add { } remove { } }
    public void M()
    {
        // CS0079: The event 'MyClass.e' can only appear on the left hand side of += or -=
        {|CS0079:e|} = null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyInitialized_NonConstant()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|} = M();
    public static int M() => 0;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_Deconstruction()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M()
    {
        int x;
        (_goo, x) = (0, 0);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_ObjectInitializer()
        {
            var code = @"
class MyClass
{
    private int {|IDE0052:_goo|};
    public MyClass M() => new MyClass() { _goo = 0 };
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_InProperty()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public int Goo
    {
        get { return 0; }
        set { _goo = value; }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsReadAndWritten()
        {
            var code = @"class MyClass
{
    private int _goo;
    public void M()
    {
        _goo = 0;
        System.Console.WriteLine(_goo);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsReadAndWritten()
        {
            var code = @"class MyClass
{
    private int P { get; set; }
    public void M()
    {
        P = 0;
        System.Console.WriteLine(P);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsReadAndWritten()
        {
            var code = @"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public void M(int x)
    {
        this[x] = 0;
        System.Console.WriteLine(this[x]);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsReadAndWritten_InProperty()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int Goo
    {
        get { return _goo; }
        set { _goo = value; }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30397, "https://github.com/dotnet/roslyn/issues/30397")]
        public async Task FieldIsIncrementedAndValueUsed()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1() => ++_goo;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30397, "https://github.com/dotnet/roslyn/issues/30397")]
        public async Task FieldIsIncrementedAndValueUsed_02()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1() { return ++_goo; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueDropped()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1() => ++_goo;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueDropped_02()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1() { ++_goo; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsIncrementedAndValueUsed()
        {
            var code = @"class MyClass
{
    private int P { get; set; }
    public int M1() => ++P;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsIncrementedAndValueDropped()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:P|} { get; set; }
    public void M1() { ++P; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsIncrementedAndValueUsed()
        {
            var code = @"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M1(int x) => ++this[x];
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsIncrementedAndValueDropped()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
    public void M1(int x) => ++this[x];
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1(int x) => _goo += x;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed_02()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1(int x) { return _goo += x; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1(int x) => _goo += x;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped_02()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1(int x) { _goo += x; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsTargetOfCompoundAssignmentAndValueUsed()
        {
            var code = @"class MyClass
{
    private int P { get; set; }
    public int M1(int x) => P += x;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsTargetOfCompoundAssignmentAndValueDropped()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:P|} { get; set; }
    public void M1(int x) { P += x; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsTargetOfCompoundAssignmentAndValueUsed()
        {
            var code = @"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M1(int x, int y) => this[x] += y;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsTargetOfCompoundAssignmentAndValueDropped()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
    public void M1(int x, int y) => this[x] += y;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfAssignmentAndParenthesized()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M1(int x) => (_goo) = x;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfAssignmentAndHasImplicitConversion()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public static implicit operator int(MyClass c) => 0;
    public void M1(MyClass c) => _goo = c;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsArg()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1() => M2(_goo);
    public int M2(int i) { i = 0; return i; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsInArg()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1() => M2(_goo);
    public int M2(in int i) { return i; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRefArg()
        {
            var code = @"class MyClass
{
    private int _goo;
    public int M1() => M2(ref _goo);
    public int M2(ref int i) => i;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOutArg()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public int M1() => M2(out _goo);
    public int M2(out int i) { i = 0; return i; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsArg()
        {
            var code = @"class MyClass
{
    private int M() => 0;
    public int M1() => M2(M);
    public int M2(System.Func<int> m) => m();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsArg()
        {
            var code = @"class MyClass
{
    private int P => 0;
    public int M1() => M2(P);
    public int M2(int p) => p;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsArg()
        {
            var code = @"class MyClass
{
    private int this[int x] { get { return 0; } set { } }
    public int M1(int x) => M2(this[x]);
    public int M2(int p) => p;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsArg()
        {
            var code = @"using System;

class MyClass
{
    private event EventHandler _e;
    public EventHandler M1() => M2(_e);
    public EventHandler M2(EventHandler e) => e;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [CombinatorialData]
        public async Task MultipleFields_AllUnused_FixOne(
            [CombinatorialValues("[|_goo|]", "[|_goo|] = 0")] string firstField,
            [CombinatorialValues("[|_bar|]", "[|_bar|] = 2")] string secondField,
            [CombinatorialValues(0, 1)] int diagnosticIndex)
        {
            var source = $@"class MyClass
{{
    private int {firstField}, {secondField};
}}";
            var fixedSource = $@"class MyClass
{{
    private int {(diagnosticIndex == 0 ? secondField : firstField)};
}}";
            var batchFixedSource = @"class MyClass
{
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                BatchFixedCode = batchFixedSource,
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                DiagnosticSelector = fixableDiagnostics => fixableDiagnostics[diagnosticIndex],
            }.RunAsync();
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
        public int M() => _goo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsInvoked_InNestedType()
        {
            var code = @"class MyClass
{
    private int M1() => 0;

    class Derived : MyClass
    {
        public int M2() => M1();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class MyClass
{
    class NestedType
    {
        private int _goo;

        public int M() => _goo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"partial class MyClass
{
    private int _goo;
}
partial class MyClass
{
    public int M() => _goo;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class MyClass
{
    private int _goo;
    public int M() => (_goo);
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsWritten_InParens()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public void M() { (_goo) = 1; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsWritten_InParens_02()
        {
            var code = @"class MyClass
{
    private int {|IDE0052:_goo|};
    public int M() => (_goo) = 1;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_InDeconstruction_InParens()
        {
            var code = @"class C
{
    private int i;

    public void M()
    {
        var x = ((i, 0), 0);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"class C
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int i;

    public void M()
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldUsedInGeneratedCode()
        {
            var code = @"class C
{
    private int i;

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    public int M() => i;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SyntaxError()
        {
            var code = @"class C
{
    private int i;

    public int M() { return {|CS1525:=|} {|CS1525:;|} }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SemanticError()
        {
            var code = @"class C
{
    private int i;

    // 'ii' is undefined.
    public int M() => {|CS0103:ii|};
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Explicit)]
class C
{
    [FieldOffset(0)]
    private int i;

    [FieldOffset(4)]
    private int i2;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task StructLayoutAttribute_SequentialLayout()
        {
            var code = @"using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
struct S
{
    private int i;
    private int i2;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesField()
        {
            var code = @"[System.Diagnostics.DebuggerDisplayAttribute(""{s}"")]
class C
{
    private string s;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesMethod()
        {
            var code = @"[System.Diagnostics.DebuggerDisplayAttribute(""{GetString()}"")]
class C
{
    private string GetString() => """";
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesProperty()
        {
            var code = @"[System.Diagnostics.DebuggerDisplayAttribute(""{MyString}"")]
class C
{
    private string MyString => """";
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnField_ReferencesField()
        {
            var code = @"class C
{
    private string s;

    [System.Diagnostics.DebuggerDisplayAttribute(""{s}"")]
    public int M;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnProperty_ReferencesMethod()
        {
            var code = @"class C
{
    private string GetString() => """";

    [System.Diagnostics.DebuggerDisplayAttribute(""{GetString()}"")]
    public int M => 0;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnProperty_ReferencesProperty()
        {
            var code = @"class C
{
    private string MyString { get { return """"; } }

    [System.Diagnostics.DebuggerDisplayAttribute(""{MyString}"")]
    public int M { get { return 0; } }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnNestedTypeMember_ReferencesField()
        {
            var code = @"class C
{
    private static string s;

    class Nested
    {
        [System.Diagnostics.DebuggerDisplayAttribute(""{C.s}"")]
        public int M;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30886, "https://github.com/dotnet/roslyn/issues/30886")]
        public async Task SerializableConstructor_TypeImplementsISerializable()
        {
            var code = @"using System.Runtime.Serialization;

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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30886, "https://github.com/dotnet/roslyn/issues/30886")]
        public async Task SerializableConstructor_BaseTypeImplementsISerializable()
        {
            var code = @"using System;
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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = $@"class C
{{
    {attribute}
    private void M()
    {{
    }}
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30887, "https://github.com/dotnet/roslyn/issues/30887")]
        public async Task ShouldSerializePropertyMethod()
        {
            var code = @"class C
{
    private bool ShouldSerializeData()
    {
        return true;
    }

    public int Data { get; private set; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(38491, "https://github.com/dotnet/roslyn/issues/38491")]
        public async Task ResetPropertyMethod()
        {
            var code = @"class C
{
    private void ResetData()
    {
        return;
    }

    public int Data { get; private set; }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30377, "https://github.com/dotnet/roslyn/issues/30377")]
        public async Task EventHandlerMethod()
        {
            var code = $@"using System;

class C
{{
    private void M(object o, EventArgs args)
    {{
    }}
}}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32727, "https://github.com/dotnet/roslyn/issues/32727")]
        public async Task NestedStructLayoutTypeWithReference()
        {
            var code = @"using System.Runtime.InteropServices;

class Program
{
    private const int MAX_PATH = 260;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessEntry32
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string szExeFile;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
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
            var code = @"
public class MyClass
{
    private MyClass _field;
    public MyClass Property => _field ??= new MyClass();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem(32842, "https://github.com/dotnet/roslyn/issues/32842")]
        public async Task FieldIsNotRead_NullCoalesceAssignment()
        {
            var code = @"
public class MyClass
{
    private MyClass {|IDE0052:_field|};
    public void M() => _field ??= new MyClass();
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(37213, "https://github.com/dotnet/roslyn/issues/37213")]
        public async Task UsedPrivateExtensionMethod()
        {
            var code = @"public static class B
{
    public static void PublicExtensionMethod(this string s) => s.PrivateExtensionMethod();
    private static void PrivateExtensionMethod(this string s) { }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }
    }
}
