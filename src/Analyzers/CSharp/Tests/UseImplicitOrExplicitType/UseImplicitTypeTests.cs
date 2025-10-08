// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitOrExplicitType;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
public sealed partial class UseImplicitTypeTests(ITestOutputHelper? logger = null)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseImplicitTypeDiagnosticAnalyzer(), new UseImplicitTypeCodeFixProvider());

    private static readonly CodeStyleOption2<bool> onWithSilent = new(true, NotificationOption2.Silent);
    private static readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);
    private static readonly CodeStyleOption2<bool> offWithInfo = new(false, NotificationOption2.Suggestion);
    private static readonly CodeStyleOption2<bool> onWithWarning = new(true, NotificationOption2.Warning);
    private static readonly CodeStyleOption2<bool> onWithError = new(true, NotificationOption2.Error);

    // specify all options explicitly to override defaults.
    internal OptionsCollection ImplicitTypeEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    private OptionsCollection ImplicitTypeWhereApparent()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
        };

    private OptionsCollection ImplicitTypeWhereApparentAndForIntrinsics()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    internal OptionsCollection ImplicitTypeButKeepIntrinsics()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
        };

    private OptionsCollection ImplicitTypeEnforcements()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithWarning },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    private OptionsCollection ImplicitTypeSilentEnforcement()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithSilent },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent },
        };

    [Fact]
    public Task NotOnFieldDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                [|int|] _myfield = 5;
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnFieldLikeEvents()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public event [|D|] _myevent;
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnConstants()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    const [|int|] x = 5;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnNullLiteral()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Program|] x = null;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27221")]
    public Task NotOnRefVar()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                void Method()
                {
                    ref [|var|] x = Method2();
                }
                ref int Method2() => throw null;
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnDynamic()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|dynamic|] x = 1;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnAnonymousMethodExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Func<string, bool>|] comparer = delegate (string value) {
                        return value != "0";
                    };
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnLambdaExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Func<int, int>|] x = y => y * y;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnMethodGroup()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Func<string, string>|] copyStr = string.Copy;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnDeclarationWithMultipleDeclarators()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|int|] x = 5, y = x;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnDeclarationWithoutInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Program|] x;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnIFormattable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|IFormattable|] s = $"Hello, {name}"
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnFormattableString()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|FormattableString|] s = $"Hello, {name}"
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotInCatchDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    try
                    {
                    }
                    catch ([|Exception|] e)
                    {
                        throw;
                    }
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotDuringConflicts()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Program|] p = new Program();
                }

                class var
                {
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotIfAlreadyImplicitlyTyped()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] p = new Program();
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnImplicitConversion()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    int i = int.MaxValue;
                    [|long|] l = i;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnBoxingImplicitConversion()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    int i = int.MaxValue;
                    [|object|] o = i;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnRHS()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    C c = new [|C|]();
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnVariablesUsedInInitalizerExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [|int|] i = (i = 20);
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26894")]
    public Task NotOnVariablesOfEnumTypeNamedAsEnumTypeUsedInInitalizerExpressionAtFirstPosition()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            enum A { X, Y }

            class C
            {
                void M()
                {
                    [|A|] A = A.X;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26894")]
    public Task NotOnVariablesNamedAsTypeUsedInInitalizerExpressionContainingTypeNameAtFirstPositionOfMemberAccess()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class A 
            { 
                public static A Instance;
            }

            class C
            {
                void M()
                {
                    [|A|] A = A.Instance;
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26894")]
    public Task SuggestOnVariablesUsedInInitalizerExpressionAsInnerPartsOfQualifiedNameStartedWithGlobal()
        => TestAsync(
            """
            enum A { X, Y }

            class C
            {
                void M()
                {
                    [|A|] A = global::A.X;
                }
            }
            """,
            """
            enum A { X, Y }

            class C
            {
                void M()
                {
                    var A = global::A.X;
                }
            }
            """, new(CSharpParseOptions.Default, options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26894")]
    public Task SuggestOnVariablesUsedInInitalizerExpressionAsInnerPartsOfQualifiedName()
        => TestInRegularAndScriptAsync(
            """
            using System;

            namespace N
            {
                class A 
                { 
                    public static A Instance;
                }
            }

            class C
            {
                void M()
                {
                    [|N.A|] A = N.A.Instance;
                }
            }
            """,
            """
            using System;

            namespace N
            {
                class A 
                { 
                    public static A Instance;
                }
            }

            class C
            {
                void M()
                {
                    var A = N.A.Instance;
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26894")]
    public Task SuggestOnVariablesUsedInInitalizerExpressionAsLastPartOfQualifiedName()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class A {}

            class X
            { 
                public static A A;
            }

            class C
            {
                void M()
                {
                    [|A|] A = X.A;
                }
            }
            """,
            """
            using System;

            class A {}

            class X
            { 
                public static A A;
            }

            class C
            {
                void M()
                {
                    var A = X.A;
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnAssignmentToInterfaceType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    [|IInterface|] i = new A();
                }
            }

            class A : IInterface
            {
            }

            interface IInterface
            {
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task NotOnArrayInitializerWithoutNewKeyword()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|int[]|] n1 = {
                        2,
                        4,
                        6,
                        8
                    };
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnLocalWithIntrinsicTypeString()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|string|] s = "hello";
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    var s = "hello";
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnIntrinsicType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|int|] s = 5;
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    var s = 5;
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27221")]
    public Task SuggestVarOnRefIntrinsicType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    ref [|int|] s = Ref();
                }
                static ref int Ref() => throw null;
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    ref var s = Ref();
                }
                static ref int Ref() => throw null;
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27221")]
    public Task WithRefIntrinsicTypeInForeach()
        => TestInRegularAndScriptAsync("""
            class E
            {
                public ref int Current => throw null;
                public bool MoveNext() => throw null;
                public E GetEnumerator() => throw null;

                void M()
                {
                    foreach (ref [|int|] x in this) { }
                }
            }
            """, """
            class E
            {
                public ref int Current => throw null;
                public bool MoveNext() => throw null;
                public E GetEnumerator() => throw null;

                void M()
                {
                    foreach (ref var x in this) { }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnFrameworkType()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    [|List<int>|] c = new List<int>();
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var c = new List<int>();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnUserDefinedType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [|C|] c = new C();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    var c = new C();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnGenericType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C<T>
            {
                static void M()
                {
                    [|C<int>|] c = new C<int>();
                }
            }
            """,
            """
            using System;

            class C<T>
            {
                static void M()
                {
                    var c = new C<int>();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnSeeminglyConflictingType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class var<T>
            {
                void M()
                {
                    [|var<int>|] c = new var<int>();
                }
            }
            """,
            """
            using System;

            class var<T>
            {
                void M()
                {
                    var c = new var<int>();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnSingleDimensionalArrayTypeWithNewOperator()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|int[]|] n1 = new int[4] { 2, 4, 6, 8 };
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    var n1 = new int[4] { 2, 4, 6, 8 };
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnSingleDimensionalArrayTypeWithNewOperator2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|int[]|] n1 = new[] { 2, 4, 6, 8 };
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    var n1 = new[] { 2, 4, 6, 8 };
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnSingleDimensionalJaggedArrayType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|int[][]|] cs = new[] {
                        new[] { 1, 2, 3, 4 },
                        new[] { 5, 6, 7, 8 }
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    var cs = new[] {
                        new[] { 1, 2, 3, 4 },
                        new[] { 5, 6, 7, 8 }
                    };
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnDeclarationWithObjectInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|Customer|] cc = new Customer { City = "Madras" };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    var cc = new Customer { City = "Madras" };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnDeclarationWithCollectionInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    [|List<int>|] digits = new List<int> { 1, 2, 3 };
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var digits = new List<int> { 1, 2, 3 };
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnDeclarationWithCollectionAndObjectInitializers()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    [|List<Customer>|] cs = new List<Customer>
                    {
                        new Customer { City = "Madras" }
                    };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var cs = new List<Customer>
                    {
                        new Customer { City = "Madras" }
                    };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnForStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    for ([|int|] i = 0; i < 5; i++)
                    {
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    for (var i = 0; i < 5; i++)
                    {
                    }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnForeachStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var l = new List<int> { 1, 3, 5 };
                    foreach ([|int|] item in l)
                    {
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var l = new List<int> { 1, 3, 5 };
                    foreach (var item in l)
                    {
                    }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnQueryExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                static void M()
                {
                    var customers = new List<Customer>();
                    [|IEnumerable<Customer>|] expr = from c in customers
                                                 where c.City == "London"
                                                 select c;
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                static void M()
                {
                    var customers = new List<Customer>();
                    var expr = from c in customers
                                                 where c.City == "London"
                                                 select c;
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarInUsingStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    using ([|Res|] r = new Res())
                    {
                    }
                }

                private class Res : IDisposable
                {
                    public void Dispose()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    using (var r = new Res())
                    {
                    }
                }

                private class Res : IDisposable
                {
                    public void Dispose()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarOnExplicitConversion()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    double x = 1234.7;
                    [|int|] a = (int)x;
                }
            }
            """,
            """
            using System;

            class Program
            {
                void Method()
                {
                    double x = 1234.7;
                    var a = (int)x;
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarInConditionalAccessExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    C obj = new C();
                    [|C|] anotherObj = obj?.Test();
                }

                C Test()
                {
                    return this;
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    C obj = new C();
                    var anotherObj = obj?.Test();
                }

                C Test()
                {
                    return this;
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarInCheckedExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    long number1 = int.MaxValue + 20L;
                    [|int|] intNumber = checked((int)number1);
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    long number1 = int.MaxValue + 20L;
                    var intNumber = checked((int)number1);
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarInUnCheckedExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    long number1 = int.MaxValue + 20L;
                    [|int|] intNumber = unchecked((int)number1);
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    long number1 = int.MaxValue + 20L;
                    var intNumber = unchecked((int)number1);
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarInAwaitExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public async void ProcessRead()
                {
                    [|string|] text = await ReadTextAsync(null);
                }

                private async Task<string> ReadTextAsync(string filePath)
                {
                    return string.Empty;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public async void ProcessRead()
                {
                    var text = await ReadTextAsync(null);
                }

                private async Task<string> ReadTextAsync(string filePath)
                {
                    return string.Empty;
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestVarInParenthesizedExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    [|int|] text = (5);
                }
            }
            """,
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    var text = (5);
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task DoNotSuggestVarOnBuiltInType_Literal_WithOption()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|int|] s = 5;
                }
            }
            """, new TestParameters(options: ImplicitTypeButKeepIntrinsics()));

    [Fact]
    public Task DoNotSuggestVarOnBuiltInType_WithOption()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private const int maxValue = int.MaxValue;

                static void M()
                {
                    [|int|] s = (unchecked(maxValue + 10));
                }
            }
            """, new TestParameters(options: ImplicitTypeButKeepIntrinsics()));

    [Fact]
    public Task DoNotSuggestVarOnFrameworkTypeEquivalentToBuiltInType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private const int maxValue = int.MaxValue;

                static void M()
                {
                    [|Int32|] s = (unchecked(maxValue + 10));
                }
            }
            """, new TestParameters(options: ImplicitTypeButKeepIntrinsics()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_DefaultExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    [|C|] text = default(C);
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    var text = default(C);
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_Literals()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    [|int|] text = 5;
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    var text = 5;
                }
            }
            """, new(options: ImplicitTypeWhereApparentAndForIntrinsics()));

    [Fact]
    public Task DoNotSuggestVarWhereTypeIsEvident_Literals()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    [|int|] text = 5;
                }
            }
            """, new TestParameters(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_ObjectCreationExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    [|C|] c = new C();
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    var c = new C();
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_CastExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    object o = DateTime.MaxValue;
                    [|DateTime|] date = (DateTime)o;
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    object o = DateTime.MaxValue;
                    var date = (DateTime)o;
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task DoNotSuggestVar_BuiltInTypesRulePrecedesOverTypeIsApparentRule1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    object o = int.MaxValue;
                    [|int|] i = (Int32)o;
                }
            }
            """, new TestParameters(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task DoNotSuggestVar_BuiltInTypesRulePrecedesOverTypeIsApparentRule2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    object o = int.MaxValue;
                    [|Int32|] i = (Int32)o;
                }
            }
            """, new TestParameters(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task DoNotSuggestVarWhereTypeIsEvident_IsExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    A a = new A();
                    [|Boolean|] s = a is IInterface;
                }
            }

            class A : IInterface
            {
            }

            interface IInterface
            {
            }
            """, new TestParameters(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_AsExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    A a = new A();
                    [|IInterface|] s = a as IInterface;
                }
            }

            class A : IInterface
            {
            }

            interface IInterface
            {
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    A a = new A();
                    var s = a as IInterface;
                }
            }

            class A : IInterface
            {
            }

            interface IInterface
            {
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_ConversionHelpers()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    [|DateTime|] a = DateTime.Parse("1");
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    var a = DateTime.Parse("1");
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_CreationHelpers()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public void Process()
                {
                    [|XElement|] a = XElement.Load();
                }
            }

            class XElement
            {
                internal static XElement Load() => return null;
            }
            """,
            """
            class C
            {
                public void Process()
                {
                    var a = XElement.Load();
                }
            }

            class XElement
            {
                internal static XElement Load() => return null;
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_CreationHelpersWithInferredTypeArguments()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    [|Tuple<int, bool>|] a = Tuple.Create(0, true);
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    var a = Tuple.Create(0, true);
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_ConvertToType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    int integralValue = 12534;
                    [|DateTime|] date = Convert.ToDateTime(integralValue);
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    int integralValue = 12534;
                    var date = Convert.ToDateTime(integralValue);
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarWhereTypeIsEvident_IConvertibleToType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void Process()
                {
                    int codePoint = 1067;
                    IConvertible iConv = codePoint;
                    [|DateTime|] date = iConv.ToDateTime(null);
                }
            }
            """,
            """
            using System;

            class C
            {
                public void Process()
                {
                    int codePoint = 1067;
                    IConvertible iConv = codePoint;
                    var date = iConv.ToDateTime(null);
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task SuggestVarNotificationLevelSilent()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|C|] n1 = new C();
                }
            }
            """,
            options: ImplicitTypeSilentEnforcement(),
            diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Hidden);

    [Fact]
    public Task SuggestVarNotificationLevelInfo()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|int|] s = 5;
                }
            }
            """,
            options: ImplicitTypeEnforcements(),
            diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Info);

    [Fact]
    public Task SuggestVarNotificationLevelWarning()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|C[]|] n1 = new[] { new C() }; // type not apparent and not intrinsic
                }
            }
            """,
            options: ImplicitTypeEnforcements(),
            diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning);

    [Fact]
    public Task SuggestVarNotificationLevelError()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|C|] n1 = new C();
                }
            }
            """,
            options: ImplicitTypeEnforcements(),
            diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Error);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23893")]
    public async Task SuggestVarOnLocalWithIntrinsicArrayType()
    {
        var before = @"class C { static void M() { [|int[]|] s = new int[0]; } }";

        //The type is intrinsic and apparent
        await TestInRegularAndScriptAsync(before, @"class C { static void M() { var s = new int[0]; } }", new(options: ImplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeButKeepIntrinsics()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent())); // Preference of intrinsic types dominates
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23893")]
    public async Task SuggestVarOnLocalWithCustomArrayType()
    {
        var before = @"class C { static void M() { [|C[]|] s = new C[0]; } }";
        var after = @"class C { static void M() { var s = new C[0]; } }";

        //The type is not intrinsic but apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ImplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ImplicitTypeButKeepIntrinsics()));
        await TestInRegularAndScriptAsync(before, after, new(options: ImplicitTypeWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23893")]
    public async Task SuggestVarOnLocalWithNonApparentCustomArrayType()
    {
        var before = @"class C { static void M() { [|C[]|] s = new[] { new C() }; } }";
        var after = @"class C { static void M() { var s = new[] { new C() }; } }";

        //The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ImplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ImplicitTypeButKeepIntrinsics()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent()));
    }

    private static readonly string trivial2uple =
        """
        namespace System
        {
            public class ValueTuple
            {
                public static ValueTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) => new ValueTuple<T1, T2>(item1, item2);
            }
            public struct ValueTuple<T1, T2>
            {
                public T1 Item1;
                public T2 Item2;

                public ValueTuple(T1 item1, T2 item2) { }
            }
        }
        """;

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11094")]
    public async Task SuggestVarOnLocalWithIntrinsicTypeTuple()
    {
        var before = @"class C { static void M() { [|(int a, string)|] s = (a: 1, ""hello""); } }";
        var after = @"class C { static void M() { var s = (a: 1, ""hello""); } }";

        await TestInRegularAndScriptAsync(before, after, new(options: ImplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ImplicitTypeWhereApparentAndForIntrinsics()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11094")]
    public async Task SuggestVarOnLocalWithNonApparentTupleType()
    {
        var before = @"class C { static void M(C c) { [|(int a, C b)|] s = (a: 1, b: c); } }";
        await TestInRegularAndScriptAsync(before, @"class C { static void M(C c) { var s = (a: 1, b: c); } }", new(options: ImplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparentAndForIntrinsics()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11154")]
    public Task ValueTupleCreate()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|ValueTuple<int, int>|] s = ValueTuple.Create(1, 1);
                }
            }
            """ + trivial2uple,
            """
            using System;

            class C
            {
                static void M()
                {
                    var s = ValueTuple.Create(1, 1);
                }
            }
            """ + trivial2uple,
            new(options: ImplicitTypeWhereApparent()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11095")]
    public Task ValueTupleCreate_2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|(int, int)|] s = ValueTuple.Create(1, 1);
                }
            }
            """ + trivial2uple,
            """
            using System;

            class C
            {
                static void M()
                {
                    var s = ValueTuple.Create(1, 1);
                }
            }
            """ + trivial2uple,
            new(options: ImplicitTypeWhereApparent()));

    [Fact]
    public Task TupleWithDifferentNames()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    [|(int, string)|] s = (c: 1, d: "hello");
                }
            }
            """,
            new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14052")]
    public Task DoNotOfferOnForEachConversionIfItChangesSemantics()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            interface IContractV1
            {
            }

            interface IContractV2 : IContractV1
            {
            }

            class ContractFactory
            {
                public IEnumerable<IContractV1> GetContracts()
                {
                }
            }

            class Program
            {
                static void M()
                {
                    var contractFactory = new ContractFactory();
                    foreach ([|IContractV2|] contract in contractFactory.GetContracts())
                    {
                    }
                }
            }
            """,
            new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14052")]
    public Task OfferOnForEachConversionIfItDoesNotChangesSemantics()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            interface IContractV1
            {
            }

            interface IContractV2 : IContractV1
            {
            }

            class ContractFactory
            {
                public IEnumerable<IContractV1> GetContracts()
                {
                }
            }

            class Program
            {
                static void M()
                {
                    var contractFactory = new ContractFactory();
                    foreach ([|IContractV1|] contract in contractFactory.GetContracts())
                    {
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            interface IContractV1
            {
            }

            interface IContractV2 : IContractV1
            {
            }

            class ContractFactory
            {
                public IEnumerable<IContractV1> GetContracts()
                {
                }
            }

            class Program
            {
                static void M()
                {
                    var contractFactory = new ContractFactory();
                    foreach (var contract in contractFactory.GetContracts())
                    {
                    }
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20437")]
    public Task SuggestVarOnDeclarationExpressionSyntax()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    DateTime.TryParse(string.Empty, [|out DateTime|] date);
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    DateTime.TryParse(string.Empty, out var date);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23893")]
    public Task DoNotSuggestVarOnDeclarationExpressionSyntaxWithIntrinsicType()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                static void M(out int x)
                {
                    M([|out int|] x);
                }
            }
            """, new TestParameters(options: ImplicitTypeButKeepIntrinsics()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    public Task DoNotSuggestVarOnStackAllocExpressions_SpanType()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            namespace System
            {
                public readonly ref struct Span<T> 
                {
                    unsafe public Span(void* pointer, int length) { }
                }
            }
            class C
            {
                static void M()
                {
                    [|Span<int>|] x = stackalloc int [10];
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79337")]
    public Task DoSuggestVarOnStackAllocExpressions_SpanType_NestedConditional()
        => TestInRegularAndScriptAsync("""
            using System;
            namespace System
            {
                public readonly ref struct Span<T> 
                {
                    unsafe public Span(void* pointer, int length) { }
                }
            }
            class C
            {
                static void M(bool choice)
                {
                    [|Span<int>|] x = choice ? stackalloc int [10] : stackalloc int [100];
                }
            }
            """, """
            using System;
            namespace System
            {
                public readonly ref struct Span<T> 
                {
                    unsafe public Span(void* pointer, int length) { }
                }
            }
            class C
            {
                static void M(bool choice)
                {
                    var x = choice ? stackalloc int [10] : stackalloc int [100];
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79337")]
    public Task DoSuggestVarOnStackAllocExpressions_SpanType_NestedCast()
        => TestInRegularAndScriptAsync("""
            using System;
            namespace System
            {
                public readonly ref struct Span<T> 
                {
                    unsafe public Span(void* pointer, int length) { }
                }
            }
            class C
            {
                static void M()
                {
                    [|Span<int>|] x = (Span<int>)stackalloc int [100];
                }
            }
            """, """
            using System;
            namespace System
            {
                public readonly ref struct Span<T> 
                {
                    unsafe public Span(void* pointer, int length) { }
                }
            }
            class C
            {
                static void M()
                {
                    var x = (Span<int>)stackalloc int [100];
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    public Task SuggestVarOnLambdasWithNestedStackAllocs()
        => TestInRegularAndScriptAsync("""
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    [|int|] x = new int[] { 1, 2, 3 }.First(i =>
                    {
                        int* y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, """
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    var x = new int[] { 1, 2, 3 }.First(i =>
                    {
                        int* y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    public Task SuggestVarOnAnonymousMethodsWithNestedStackAllocs()
        => TestInRegularAndScriptAsync("""
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    [|int|] x = new int[] { 1, 2, 3 }.First(delegate (int i)
                    {
                        int* y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, """
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    var x = new int[] { 1, 2, 3 }.First(delegate (int i)
                    {
                        int* y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    public Task SuggestVarOnStackAllocsNestedInLambdas()
        => TestInRegularAndScriptAsync("""
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    var x = new int[] { 1, 2, 3 }.First(i =>
                    {
                        [|int*|] y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, """
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    var x = new int[] { 1, 2, 3 }.First(i =>
                    {
                        var y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    public Task SuggestVarOnStackAllocsNestedInAnonymousMethods()
        => TestInRegularAndScriptAsync("""
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    var x = new int[] { 1, 2, 3 }.First(delegate (int i)
                    {
                        [|int*|] y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, """
            using System.Linq;
            class C
            {
                unsafe static void M()
                {
                    var x = new int[] { 1, 2, 3 }.First(delegate (int i)
                    {
                        var y = stackalloc int[10];
                        return i == 1;
                    });
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22768")]
    public Task SuggestVarOnStackAllocsInOuterMethodScope()
        => TestInRegularAndScriptAsync("""
            class C
            {
                unsafe static void M()
                {
                    [|int*|] x = stackalloc int [10];
                }
            }
            """, """
            class C
            {
                unsafe static void M()
                {
                    var x = stackalloc int [10];
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23116")]
    public Task DoSuggestForDeclarationExpressionIfItWouldNotChangeOverloadResolution2()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue("key", out [|int|] value);
                    return value;
                }

                public static bool TryGetValue(string key, out int value) => false;
                public static bool TryGetValue(string key, out bool value, int x) => false;
            }
            """, """
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue("key", out var value);
                    return value;
                }

                public static bool TryGetValue(string key, out int value) => false;
                public static bool TryGetValue(string key, out bool value, int x) => false;
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23116")]
    public Task DoNotSuggestForDeclarationExpressionIfItWouldChangeOverloadResolution()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue("key", out [|int|] value);
                    return value;
                }

                public static bool TryGetValue(string key, out object value) => false;

                public static bool TryGetValue<T>(string key, out T value) => false;
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23116")]
    public Task DoNotSuggestIfChangesGenericTypeInference()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue("key", out [|int|] value);
                    return value;
                }

                public static bool TryGetValue<T>(string key, out T value) => false;
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23116")]
    public Task SuggestIfDoesNotChangeGenericTypeInference1()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue<int>("key", out [|int|] value);
                    return value;
                }

                public static bool TryGetValue<T>(string key, out T value) => false;
            }
            """, """
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue<int>("key", out var value);
                    return value;
                }

                public static bool TryGetValue<T>(string key, out T value) => false;
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23116")]
    public Task SuggestIfDoesNotChangeGenericTypeInference2()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue(0, out [|int|] value);
                    return value;
                }

                public static bool TryGetValue<T>(T key, out T value) => false;
            }
            """, """
            class Program
            {
                static int Main(string[] args)
                {
                    TryGetValue(0, out var value);
                    return value;
                }

                public static bool TryGetValue<T>(T key, out T value) => false;
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23711")]
    public Task SuggestVarForDelegateType()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|GetHandler|] handler = Handler;
                }

                private static GetHandler Handler;

                delegate object GetHandler();
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var handler = Handler;
                }

                private static GetHandler Handler;

                delegate object GetHandler();
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23711")]
    public Task DoNotSuggestVarForDelegateType1()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|GetHandler|] handler = () => new object();
                }

                private static GetHandler Handler;

                delegate object GetHandler();
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23711")]
    public Task DoNotSuggestVarForDelegateType2()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|GetHandler|] handler = Foo;
                }

                private static GetHandler Handler;

                private static object Foo() => new object();

                delegate object GetHandler();
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23711")]
    public Task DoNotSuggestVarForDelegateType3()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|GetHandler|] handler = delegate { return new object(); };
                }

                private static GetHandler Handler;

                delegate object GetHandler();
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24262")]
    public Task DoNotSuggestVarForInterfaceVariableInForeachStatement()
        => TestMissingInRegularAndScriptAsync("""
            public interface ITest
            {
                string Value { get; }
            }
            public class TestInstance : ITest
            {
                string ITest.Value => "Hi";
            }

            public class Test
            {
                public TestInstance[] Instances { get; }

                public void TestIt()
                {
                    foreach ([|ITest|] test in Instances)
                    {
                        Console.WriteLine(test.Value);
                    }
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24262")]
    public Task DoNotSuggestVarForInterfaceVariableInDeclarationStatement()
        => TestMissingInRegularAndScriptAsync("""
            public interface ITest
            {
                string Value { get; }
            }
            public class TestInstance : ITest
            {
                string ITest.Value => "Hi";
            }

            public class Test
            {
                public void TestIt()
                {
                    [|ITest|] test = new TestInstance();
                    Console.WriteLine(test.Value);
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24262")]
    public Task DoNotSuggestVarForAbstractClassVariableInForeachStatement()
        => TestMissingInRegularAndScriptAsync("""
            public abstract class MyAbClass
            {
                string Value { get; }
            }

            public class TestInstance : MyAbClass
            {
                public string Value => "Hi";
            }

            public class Test
            {
                public TestInstance[] Instances { get; }

                public void TestIt()
                {
                    foreach ([|MyAbClass|] instance in Instances)
                    {
                        Console.WriteLine(instance);
                    }
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24262")]
    public Task DoNotSuggestVarForAbstractClassVariableInDeclarationStatement()
        => TestMissingInRegularAndScriptAsync("""
            public abstract class MyAbClass
            {
                string Value { get; }
            }

            public class TestInstance : MyAbClass
            {
                public string Value => "Hi";
            }

            public class Test
            {
                public TestInstance[] Instances { get; }

                public void TestIt()
                {
                    [|MyAbClass|]  test = new TestInstance();
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task DoNoSuggestVarForRefForeachVar()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            namespace System
            {
                public readonly ref struct Span<T>
                {
                    unsafe public Span(void* pointer, int length) { }

                    public ref SpanEnum GetEnumerator() => throw new Exception();

                    public struct SpanEnum
                    {
                        public ref int Current => 0;
                        public bool MoveNext() => false;
                    }
                }
            }
            class C
            {
                public void M(Span<int> span)
                {
                    foreach ([|ref|] var rx in span)
                    {
                    }
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
    public Task NoSuggestionOnForeachCollectionExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    foreach (string arg in [|args|])
                    {

                    }
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39171")]
    public Task NoSuggestionForSwitchExpressionDifferentTypes()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                interface IFruit { }

                class Apple : IFruit { }

                class Banana : IFruit { }

                public static void Test(string name)
                {
                    [|IFruit|] fruit = name switch
                    {
                        "apple" => new Apple(),
                        "banana" => new Banana(),
                        _ => null,
                    };
                }
            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39171")]
    public Task SuggestSwitchExpressionSameOrInheritedTypes()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Test { }

            class Test2 : Test { }

            class C
            {
                void M()
                {
                    var str = "one";
                    [|Test|] t = str switch
                    {
                        "one" => new Test(),
                        "two" => new Test2(),
                        _ => throw new InvalidOperationException("Unknown test."),
                    };
                }     
            }
            """,
            """
            using System;

            class Test { }

            class Test2 : Test { }

            class C
            {
                void M()
                {
                    var str = "one";
                    var t = str switch
                    {
                        "one" => new Test(),
                        "two" => new Test2(),
                        _ => throw new InvalidOperationException("Unknown test."),
                    };
                }     
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32088")]
    public Task DoNotSuggestVarOnDeclarationExpressionWithInferredTupleNames()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            static class Program
            {
                static void Main(string[] args)
                {
                    if (!_data.TryGetValue(0, [|out List<(int X, int Y)>|] value))
                        return;

                    var x = value.FirstOrDefault().X;
                }

                private static Dictionary<int, List<(int, int)>> _data =
                    new Dictionary<int, List<(int, int)>>();
            }
            """, parameters: new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32088")]
    public Task DoSuggestVarOnDeclarationExpressionWithMatchingTupleNames()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            static class Program
            {
                static void Main(string[] args)
                {
                    if (!_data.TryGetValue(0, [|out List<(int X, int Y)>|] value))
                        return;

                    var x = value.FirstOrDefault().X;
                }

                private static Dictionary<int, List<(int X, int Y)>> _data =
                    new Dictionary<int, List<(int, int)>>();
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            static class Program
            {
                static void Main(string[] args)
                {
                    if (!_data.TryGetValue(0, out var value))
                        return;

                    var x = value.FirstOrDefault().X;
                }

                private static Dictionary<int, List<(int X, int Y)>> _data =
                    new Dictionary<int, List<(int, int)>>();
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44507")]
    public Task DoNotSuggestVarInAmbiguousSwitchExpression()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var i = 1;
                    [||]C x = i switch
                    {
                        0 => new A(),
                        1 => new B(),
                        _ => throw new ArgumentException(),
                    };
                }
            }

            class A : C
            {
            }

            class B : C
            {
            }
            """, parameters: new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44507")]
    public Task DoNotSuggestVarInSwitchExpressionWithDelegateType()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                private void M(object sender, EventArgs e)
                {
                    var x = 1;
                    [||]Action<object, EventArgs> a = x switch
                    {
                        0 => (sender, e) => f1(sender, e),
                        1 => (sender, e) => f2(sender, e),
                        _ => throw new ArgumentException()
                    };

                    a(sender, e);
                }

                private readonly Action<object, EventArgs> f1;
                private readonly Action<object, EventArgs> f2;
            }
            """, parameters: new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task DoNotSuggestVarForImplicitObjectCreation()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|string|] p = new('c', 1);
                }

            }
            """, new TestParameters(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestForNullable1()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    [|string?|] a = NullableString();
                    return a;
                }

                string? NullableString() => null;
            }
            """,
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    var a = NullableString();
                    return a;
                }

                string? NullableString() => null;
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestForNullable2()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    [|string?|] a = NonNullString();
                    return a;
                }

                string NonNullString() => string.Empty;
            }
            """,
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    var a = NonNullString();
                    return a;
                }

                string NonNullString() => string.Empty;
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestForNullable3()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    [|string|] a = NonNullString();
                    return a;
                }

                string NonNullString() => string.Empty;
            }
            """,
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    var a = NonNullString();
                    return a;
                }

                string NonNullString() => string.Empty;
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestForNullableOut1()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    if (GetNullString(out [|string?|] a))
                    {
                        return a;
                    }

                    return null;
                }

                bool GetNullString(out string? s)
                {
                    s = null;
                    return true;
                }
            }
            """,
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    if (GetNullString(out var a))
                    {
                        return a;
                    }

                    return null;
                }

                bool GetNullString(out string? s)
                {
                    s = null;
                    return true;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestForNullableOut2()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    if (GetNonNullString(out [|string?|] a))
                    {
                        return a;
                    }

                    return null;
                }

                bool GetNonNullString(out string s)
                {
                    s = string.Empty;
                    return true;
                }
            }
            """,
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    if (GetNonNullString(out var a))
                    {
                        return a;
                    }

                    return null;
                }

                bool GetNonNullString(out string s)
                {
                    s = string.Empty;
                    return true;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task SuggestForNullableOut3()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    if (GetNonNullString(out [|string|] a))
                    {
                        return a;
                    }

                    return null;
                }

                bool GetNonNullString(out string s)
                {
                    s = string.Empty;
                    return true;
                }
            }
            """,
            """
            #nullable enable

            class C
            {
                string? M()
                {
                    if (GetNonNullString(out var a))
                    {
                        return a;
                    }

                    return null;
                }

                bool GetNonNullString(out string s)
                {
                    s = string.Empty;
                    return true;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41780")]
    public Task SuggestOnRefType1()
        => TestAsync(
            """
            class C
            {
                void Method(ref int x)
                {
                  ref [|int|] y = ref x;
                }
            }
            """,
            """
            class C
            {
                void Method(ref int x)
                {
                  ref var y = ref x;
                }
            }
            """, new(CSharpParseOptions.Default, options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58404")]
    public Task TestLambdaNaturalType1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            
            class C
            {
                static void M()
                {
                    [|Func<int>|] s = int () => { };
                }
            }
            """,
            """
            using System;
            
            class C
            {
                static void M()
                {
                    var s = int () => { };
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58404")]
    public Task TestLambdaNaturalType1_CSharp9()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            
            class C
            {
                static void M()
                {
                    [|Func<int>|] s = int () => { };
                }
            }
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9), options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58404")]
    public Task TestLambdaNaturalType2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            
            class C
            {
                static void M()
                {
                    [|Action<int>|] s = (a) => { };
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58404")]
    public Task TestLambdaNaturalType3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            delegate int D()
            
            class C
            {
                static void M()
                {
                    [|D|] s = int () => { };
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64902")]
    public Task TestNotOnAwaitedTask()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Program
            {
                public static async Task Main()
                {
                    object test1 = DoSomeWork();
                    object test2 = await Task.Run(() => DoSomeWork());

                    IEnumerable<object> test3 = DoSomeWorkGeneric();
                    [|IEnumerable<object>|] test4 = await Task.Run(() => DoSomeWorkGeneric());
                }

                public static object DoSomeWork()
                {
                    return new object();
                }

                public static IEnumerable<object> DoSomeWorkGeneric()
                {
                    return new List<object>();
                }
            }
            """, new(options: ImplicitTypeWhereApparent()));
}
