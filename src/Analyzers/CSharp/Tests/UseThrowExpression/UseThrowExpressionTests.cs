// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UseThrowExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseThrowExpression;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
public sealed partial class UseThrowExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseThrowExpressionTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseThrowExpressionDiagnosticAnalyzer(), new UseThrowExpressionCodeFixProvider());

    [Fact]
    public Task WithoutBraces()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s == null)
                        [|throw|] new ArgumentNullException(nameof(s));
                    _s = s;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    _s = s ?? throw new ArgumentNullException(nameof(s));
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/38136")]
    public Task TestMissingOnIf()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    [|if|] (s == null)
                        throw new ArgumentNullException(nameof(s));
                    _s = s;
                }
            }
            """);

    [Fact]
    public Task WithBraces()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(s));
                    }

                    _s = s;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    _s = s ?? throw new ArgumentNullException(nameof(s));
                }
            }
            """);

    [Fact]
    public Task TestNotOnAssign()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s == null)
                        throw new ArgumentNullException(nameof(s));
                    _s = [|s|];
                }
            }
            """);

    [Fact]
    public Task OnlyInCSharp7AndHigher()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(s)) };
                    _s = s;
                }
            }
            """, new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

    [Fact]
    public Task WithIntermediaryStatements()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s, string t)
                {
                    if (s == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(s));
                    }

                    if (t == null)
                    {
                        throw new ArgumentNullException(nameof(t));
                    }

                    _s = s;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s, string t)
                {
                    if (t == null)
                    {
                        throw new ArgumentNullException(nameof(t));
                    }

                    _s = s ?? throw new ArgumentNullException(nameof(s));
                }
            }
            """);

    [Fact]
    public Task NotWithIntermediaryWrite()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s, string t)
                {
                    if (s == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(s));
                    };
                    s = "something";
                    _s = s;
                }
            }
            """);

    [Fact]
    public Task NotWithIntermediaryMemberAccess()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s, string t)
                {
                    if (s == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(s));
                    };
                    s.ToString();
                    _s = s;
                }
            }
            """);

    [Fact]
    public Task TestNullCheckOnLeft()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (null == s)
                        [|throw|] new ArgumentNullException(nameof(s));
                    _s = s;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    _s = s ?? throw new ArgumentNullException(nameof(s));
                }
            }
            """);

    [Fact]
    public Task TestWithLocal()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    string s = null;
                    if (null == s)
                        [|throw|] new ArgumentNullException(nameof(s));
                    _s = s;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    string s = null;
                    _s = s ?? throw new ArgumentNullException(nameof(s));
                }
            }
            """);

    [Fact]
    public Task TestNotOnField()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string s;

                void M()
                {
                    if (null == s)
                        [|throw|] new ArgumentNullException(nameof(s));
                    _s = s;
                }
            }
            """);

    [Fact]
    public Task TestAssignBeforeCheck()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    _s = s;
                    if (s == null)
                        [|throw|] new ArgumentNullException(nameof(s));
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16234")]
    public Task TestNotInExpressionTree()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                private string _s;

                void Goo()
                {
                    Expression<Action<string>> e = s =>
                    {
                        if (s == null)
                            [|throw|] new ArgumentNullException(nameof(s));

                        _s = s;
                    };
                }
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=404142")]
    public Task TestNotWithAsCheck()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class BswParser3
            {
                private ParserSyntax m_syntax;

                public BswParser3(ISyntax syntax)
                {
                    if (syntax == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(syntax));
                    }

                    m_syntax = syntax as ParserSyntax;

                    if (m_syntax == null)
                        throw new ArgumentException();
                }
            }

            internal class ParserSyntax
            {
            }

            public interface ISyntax
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18670")]
    public Task TestNotWithElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                int? _x;

                public C(int? x)
                {
                    if (x == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(x));
                    }
                    else
                    {
                        Console.WriteLine();
                    }

                    _x = x;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19377")]
    public Task TestNotWithMultipleStatementsInIf1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s == null)
                    {
                        Console.WriteLine();
                        [|throw|] new ArgumentNullException(nameof(s));
                    }
                    _s = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19377")]
    public Task TestNotWithMultipleStatementsInIf2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(s));
                        Console.WriteLine();
                    }
                    _s = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21612")]
    public Task TestNotWhenAccessedOnLeftOfAssignment()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class A
            {
                public string Id;
            }

            class B
            {
                private Dictionary<string, A> map = new Dictionary<string, A>();
                public B(A a)
                {
                    if (a == null) [|throw|] new ArgumentNullException();
                    map[a.Id] = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24628")]
    public Task TestNotWhenAccessedOnLineBefore()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class B
            {
                public B(object arg)
                {
                    Dictionary<object, object> map = null;

                    if (arg == null) [|throw|] new ArgumentNullException();
                    var key = MakeKey(arg);
                    map[key] = arg;
                }

                object MakeKey(object x) => null;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22926")]
    public Task TestNotWhenUnconstrainedTypeParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            class A<T>
            {
                T x;
                public A(T t)
                {
                    if (t == null) [|throw|] new ArgumentNullException();
                    x = t;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22926")]
    public Task TestWhenClassConstrainedTypeParameter()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class A<T> where T: class
            {
                T x;
                public A(T t)
                {
                    if (t == null) [|throw|] new ArgumentNullException();
                    x = t;
                }
            }
            """,
            """
            using System;
            class A<T> where T: class
            {
                T x;
                public A(T t)
                {
                    x = t ?? throw new ArgumentNullException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22926")]
    public Task TestWhenStructConstrainedTypeParameter()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class A<T> where T: struct
            {
                T? x;
                public A(T? t)
                {
                    if (t == null) [|throw|] new ArgumentNullException();
                    x = t;
                }
            }
            """,
            """
            using System;
            class A<T> where T: struct
            {
                T? x;
                public A(T? t)
                {
                    x = t ?? throw new ArgumentNullException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44454")]
    public Task TopLevelStatement()
        => TestAsync(
            """
            using System;
            string s = null;
            string x = null;
            if (s == null) [|throw|] new ArgumentNullException();
            x = s;
            """,
            """
            using System;
            string s = null;
            string x = null;

            x = s ?? throw new ArgumentNullException();
            """, new(TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38102")]
    public Task PreserveTrailingTrivia1()
        => TestAsync(
            """
            using System;

            class Program
            {
                object _arg;

                public Program(object arg)
                {
                    if (arg == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(arg)); // Oh no!
                    }
                    _arg = arg;
                }
            }
            """,
            """
            using System;

            class Program
            {
                object _arg;

                public Program(object arg)
                {
                    _arg = arg ?? throw new ArgumentNullException(nameof(arg)); // Oh no!
                }
            }
            """, new(TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38102")]
    public Task PreserveTrailingTrivia2()
        => TestAsync(
            """
            using System;

            class Program
            {
                object _arg;

                public Program(object arg)
                {
                    if (arg == null)
                    {
                        [|throw|] new ArgumentNullException(nameof(arg)); // Oh no!
                    }
                    _arg = arg; // oh yes!
                }
            }
            """,
            """
            using System;

            class Program
            {
                object _arg;

                public Program(object arg)
                {
                    // Oh no!
                    _arg = arg ?? throw new ArgumentNullException(nameof(arg)); // oh yes!
                }
            }
            """, new(TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));
}
