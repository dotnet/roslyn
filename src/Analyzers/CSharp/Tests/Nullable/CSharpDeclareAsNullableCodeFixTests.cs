// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.DeclareAsNullable
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsDeclareAsNullable)]
    public class CSharpDeclareAsNullableCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public CSharpDeclareAsNullableCodeFixTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpDeclareAsNullableCodeFixProvider());

        private static readonly TestParameters s_nullableFeature = new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));

        [Fact]
        public async Task FixAll()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static string M()
                    {
                        return {|FixAllInDocument:null|};
                    }
                    static string M2(bool b)
                    {
                        if (b)
                            return null;
                        else
                            return null;
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static string? M()
                    {
                        return null;
                    }
                    static string? M2(bool b)
                    {
                        if (b)
                            return null;
                        else
                            return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static string M()
                    {
                        return [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static string? M()
                    {
                        return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_Async()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static async System.Threading.Tasks.Task<string> M()
                    {
                        return [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static async System.Threading.Tasks.Task<string?> M()
                    {
                        return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_AsyncLocalFunction()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        async System.Threading.Tasks.Task<string> local()
                        {
                            return [|null|];
                        }
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        async System.Threading.Tasks.Task<string?> local()
                        {
                            return null;
                        }
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_WithTrivia()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static /*before*/ string /*after*/ M()
                    {
                        return [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static /*before*/ string? /*after*/ M()
                    {
                        return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_ArrowBody()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static string M() => [|null|];
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static string? M() => null;
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26639")]
        public async Task FixReturnType_LocalFunction_ArrowBody()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        string local() => [|null|];
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26639")]
        public async Task FixLocalFunctionReturnType()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        string local()
                        {
                            return [|null|];
                        }
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        string? local()
                        {
                            return null;
                        }
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task NoFixAlreadyNullableReturnType()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    static string? M()
                    {
                        return [|null|];
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26628")]
        public async Task FixField()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string x = [|null|];
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? x = null;
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixFieldEqualsNull()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string x;
                    void M()
                    {
                        x = [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? x;
                    void M()
                    {
                        x = null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixPropertyEqualsNull()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string x { get; set; }
                    void M()
                    {
                        x = [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? x { get; set; }
                    void M()
                    {
                        x = null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixLocalDeclaration()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        string x = [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        string? x = null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixLocalDeclaration_FromAssignment()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        string x = "";
                        x = [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        string? x = "";
                        x = null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task CannotFixMultiLocalDeclaration_FromAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        string x, y;
                        x = [|null|];
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixParameter_FromAssignment()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static void M(out string x)
                    {
                        x = [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static void M(out string? x)
                    {
                        x = null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task CannotFixParameterOfPartialMethod_FromAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                partial class Program
                {
                    partial void M(out string x);

                    partial void M(out string x)
                    {
                        x = [|null|];
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task CannotFixParameterOfExtendedPartialMethod_FromAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                partial class Program
                {
                    public partial void M(out string x);

                    public partial void M(out string x)
                    {
                        x = [|null|];
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixLocalDeclaration_WithVar()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        var x = [|null|];
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task NoFixMultiDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    static void M()
                    {
                        string x = [|null|], y = null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26628")]
        public async Task FixPropertyDeclaration()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string x { get; set; } = [|null|];
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? x { get; set; } = null;
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixPropertyDeclaration_WithReturnNull()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string x { get { return [|null|]; } }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? x { get { return null; } }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixPropertyDeclaration_ArrowBody()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string x => [|null|];
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? x => null;
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26626")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/30026")]
        public async Task FixOptionalParameter()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static void M(string x = [|null|]) { }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static void M(string? x = null) { }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixLocalWithAs()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static void M(object o)
                    {
                        string x = [|o as string|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static void M(object o)
                    {
                        string? x = o as string;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_Iterator_Enumerable()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static System.Collections.Generic.IEnumerable<string> M()
                    {
                        yield return [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static System.Collections.Generic.IEnumerable<string?> M()
                    {
                        yield return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_Iterator_Enumerator()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    static System.Collections.Generic.IEnumerator<string> M()
                    {
                        yield return [|null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    static System.Collections.Generic.IEnumerator<string?> M()
                    {
                        yield return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_IteratorProperty()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    System.Collections.Generic.IEnumerable<string> Property
                    {
                        get
                        {
                            yield return [|null|];
                        }
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    System.Collections.Generic.IEnumerable<string?> Property
                    {
                        get
                        {
                            yield return null;
                        }
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_Iterator_LocalFunction()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        System.Collections.Generic.IEnumerable<string> local()
                        {
                            yield return [|null|];
                        }
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        System.Collections.Generic.IEnumerable<string?> local()
                        {
                            yield return null;
                        }
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39422")]
        public async Task FixReturnType_ConditionalOperator_Function()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string Test(bool? value)
                    {
                        return [|value?.ToString()|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? Test(bool? value)
                    {
                        return value?.ToString();
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39422")]
        public async Task FixAllReturnType_ConditionalOperator_Function()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string field;
                    string Property { get; set; }

                    string Test(bool? value)
                    {
                        return {|FixAllInDocument:value?.ToString()|};
                    }

                    string Test1(bool? value)
                    {
                        return value?.ToString();
                    }

                    string Test2(bool? value)
                    {
                        field = null;
                        Property = null;
                        return null;
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string field;
                    string Property { get; set; }

                    string? Test(bool? value)
                    {
                        return value?.ToString();
                    }

                    string? Test1(bool? value)
                    {
                        return value?.ToString();
                    }

                    string Test2(bool? value)
                    {
                        field = null;
                        Property = null;
                        return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39422")]
        public async Task FixAllReturnType_Invocation()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string field;
                    string Property { get; set; }

                    void M(string value)
                    {
                        M({|FixAllInDocument:null|});
                    }
                    void M2(string value)
                    {
                        M2(null);
                    }
                    string Test(bool? value)
                    {
                        return value?.ToString();
                    }
                    string Test2(bool? value)
                    {
                        field = null;
                        Property = null;
                        return null;
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? field;
                    string? Property { get; set; }

                    void M(string? value)
                    {
                        M(null);
                    }
                    void M2(string? value)
                    {
                        M2(null);
                    }
                    string Test(bool? value)
                    {
                        return value?.ToString();
                    }
                    string Test2(bool? value)
                    {
                        field = null;
                        Property = null;
                        return null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39420")]
        public async Task FixReturnType_TernaryExpression_Function()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string Test(bool value)
                    {
                        return [|value ? "text" : null|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? Test(bool value)
                    {
                        return value ? "text" : null;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39423")]
        public async Task FixReturnType_Default()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string Test()
                    {
                        return [|default|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? Test()
                    {
                        return default;
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39423")]
        public async Task FixReturnType_DefaultWithNullableType()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    string Test()
                    {
                        return [|default(string)|];
                    }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    string? Test()
                    {
                        return default(string);
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixInvocation_NamedArgument()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2(x: [|null|]);
                    }
                    void M2(string x) { }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2(x: null);
                    }
                    void M2(string? x) { }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44338")]
        public async Task NoFixInvocationOfExternalMethod_NamedArgument()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        var list = new System.Collections.Generic.List<string>();
                        list.Add(item: [|null|]);
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixInvocation_NamedArgument_OutOfOrder()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2(x: [|null|], i: 1);
                    }
                    void M2(int i, string x) { }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2(x: null, i: 1);
                    }
                    void M2(int i, string? x) { }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44338")]
        public async Task NoFixInvocationOfExternalMethod_NamedArgument_OutOfOrder()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        var dict = new System.Collections.Generic.Dictionary<string, int>();
                        dict.Add(value: 0, key: [|null|]);
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixInvocation_NamedArgument_Partial()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                partial class Program
                {
                    void M()
                    {
                        M2(x: [|null|]);
                    }
                    partial void M2(string x);
                    partial void M2(string x) { }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixInvocation_PositionArgument()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2([|null|]);
                    }
                    void M2(string x) { }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2(null);
                    }
                    void M2(string? x) { }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44338")]
        public async Task NoFixInvocationOfExternalMethod_PositionArgument()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        var list = new System.Collections.Generic.List<string>();
                        list.Add([|null|]);
                    }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixInvocation_PositionArgument_SecondPosition()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2(1, [|null|]);
                    }
                    void M2(int i, string x) { }
                }
                """,
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2(1, null);
                    }
                    void M2(int i, string? x) { }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixInvocation_PositionArgument_Params()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        M2("", [|null|]);
                    }
                    void M2(params string[] x) { }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixInvocation_Indexer()
        {
            // Not supported yet
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    void M()
                    {
                        this[[|null|]];
                    }
                    int this[string x] { get { throw null!; } set { throw null!; } }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixPropertyDeclaration_Unassigned()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable

                class C
                {
                    string [|S|] { get; }
                }
                """,
                """
                #nullable enable

                class C
                {
                    string? S { get; }
                }
                """,
                parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44983")]
        public async Task FixFieldDeclaration_Unassigned()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable

                class C
                {
                    private string [|_value|];
                }
                """,
                """
                #nullable enable

                class C
                {
                    private string? _value;
                }
                """,
                parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44983")]
        public async Task MultipleDeclarator_NoDiagnostic()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #nullable enable
                class Program
                {
                    string [|s|], s2 = "hello";
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46354")]
        public async Task FixTupleFieldAssignment()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                  static void F1((string, string?) t)
                  {
                    if (t.Item2 == null) return;
                    t.Item1 = [|null|];
                  }
                }
                """,
                """
                #nullable enable
                class Program
                {
                  static void F1((string?, string?) t)
                  {
                    if (t.Item2 == null) return;
                    t.Item1 = null;
                  }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46354")]
        public async Task FixTupleNamedFieldAssignment()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                  static void F1((string Foo, string? Bar) t)
                  {
                    if (t.Bar == null) return;
                    t.Foo = [|null|];
                  }
                }
                """,
                """
                #nullable enable
                class Program
                {
                  static void F1((string? Foo, string? Bar) t)
                  {
                    if (t.Bar == null) return;
                    t.Foo = null;
                  }
                }
                """, parameters: s_nullableFeature);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46354")]
        public async Task FixTupleGenericFieldAssignment()
        {
            await TestInRegularAndScript1Async(
                """
                #nullable enable
                class Program
                {
                  static void F1<T>((T, T?) t) where T : class
                  {
                    if (t.Item2 == null) return;
                    t.Item1 = [|null|];

                    var (a, b) = t;
                  }
                }
                """,
                """
                #nullable enable
                class Program
                {
                  static void F1<T>((T?, T?) t) where T : class
                  {
                    if (t.Item2 == null) return;
                    t.Item1 = null;

                    var (a, b) = t;
                  }
                }
                """, parameters: s_nullableFeature);
        }
    }
}
