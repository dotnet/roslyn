// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UseThrowExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseThrowExpression;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseThrowExpression;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
public partial class UseThrowExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseThrowExpressionTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseThrowExpressionDiagnosticAnalyzer(), new UseThrowExpressionCodeFixProvider());

    [Fact]
    public async Task WithoutBraces()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/38136")]
    public async Task TestMissingOnIf()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task WithBraces()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestNotOnAssign()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task OnlyInCSharp7AndHigher()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task WithIntermediaryStatements()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task NotWithIntermediaryWrite()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task NotWithIntermediaryMemberAccess()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestNullCheckOnLeft()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestWithLocal()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestNotOnField()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestAssignBeforeCheck()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16234")]
    public async Task TestNotInExpressionTree()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=404142")]
    public async Task TestNotWithAsCheck()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18670")]
    public async Task TestNotWithElseClause()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19377")]
    public async Task TestNotWithMultipleStatementsInIf1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19377")]
    public async Task TestNotWithMultipleStatementsInIf2()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21612")]
    public async Task TestNotWhenAccessedOnLeftOfAssignment()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24628")]
    public async Task TestNotWhenAccessedOnLineBefore()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22926")]
    public async Task TestNotWhenUnconstrainedTypeParameter()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22926")]
    public async Task TestWhenClassConstrainedTypeParameter()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22926")]
    public async Task TestWhenStructConstrainedTypeParameter()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44454")]
    public async Task TopLevelStatement()
    {
        await TestAsync(
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
            """, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38102")]
    public async Task PreserveTrailingTrivia1()
    {
        await TestAsync(
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
            """, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38102")]
    public async Task PreserveTrailingTrivia2()
    {
        await TestAsync(
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
            """, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
    }
}
