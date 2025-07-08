// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.PreferFrameworkType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PreferFrameworkType;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)]
public sealed partial class PreferFrameworkTypeTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpPreferFrameworkTypeDiagnosticAnalyzer(), new PreferFrameworkTypeCodeFixProvider());

    private readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);
    private readonly CodeStyleOption2<bool> offWithInfo = new(false, NotificationOption2.Suggestion);

    private OptionsCollection NoFrameworkType
        => new(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, onWithInfo },
        };

    private OptionsCollection FrameworkTypeEverywhere
        => new(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, offWithInfo },
        };

    private OptionsCollection FrameworkTypeInDeclaration
        => new(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, onWithInfo },
        };

    private OptionsCollection FrameworkTypeInMemberAccess
        => new(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, offWithInfo },
        };

    [Fact]
    public Task NotWhenOptionsAreNotSet()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|int|] x = 1;
                }
            }
            """, new TestParameters(options: NoFrameworkType));

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
            """, new TestParameters(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task NotOnSystemVoid()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                [|void|] Method()
                {
                }
            }
            """, new TestParameters(options: FrameworkTypeEverywhere));

    [Fact]
    public Task NotOnUserdefinedType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Program|] p;
                }
            }
            """, new TestParameters(options: FrameworkTypeEverywhere));

    [Fact]
    public Task NotOnFrameworkType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Int32|] p;
                }
            }
            """, new TestParameters(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task NotOnQualifiedTypeSyntax()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void Method()
                {
                    [|System.Int32|] p;
                }
            }
            """, new TestParameters(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task NotOnFrameworkTypeWithNoPredefinedKeywordEquivalent()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|List|]<int> p;
                }
            }
            """, new TestParameters(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task NotOnIdentifierThatIsNotTypeSyntax()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    int [|p|];
                }
            }
            """, new TestParameters(options: FrameworkTypeInDeclaration));

    [Fact]
    public async Task QualifiedReplacementWhenNoUsingFound()
    {
        await TestInRegularAndScriptAsync("""
            class Program
            {
                [|string|] _myfield = 5;
            }
            """, """
            class Program
            {
                System.String _myfield = 5;
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task FieldDeclaration()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                [|int|] _myfield;
            }
            """, """
            using System;
            class Program
            {
                Int32 _myfield;
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TestNint_WithNumericIntPtr_CSharp11()
    {
        await TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet7="true" LanguageVersion="11">
                    <Document>using System;
            class Program
            {
                [|nint|] _myfield;
            }</Document>
                </Project>
            </Workspace>
            """, """
            using System;
            class Program
            {
                IntPtr _myfield;
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TestNint_WithNumericIntPtr_CSharp8()
    {
        await TestMissingInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet7="true" LanguageVersion="8">
                    <Document>using System;
            class Program
            {
                [|nint|] _myfield;
            }</Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Theory]
    [InlineData("CommonReferences")]
    [InlineData("CommonReferencesNet7")]
    public async Task TestNint_WithoutNumericIntPtr_CSharp10(string references)
    {
        await TestMissingInRegularAndScriptAsync($$"""
            <Workspace>
                <Project Language="C#" {{references}}="true" LanguageVersion="10">
                    <Document>using System;
            class Program
            {
                [|nint|] _myfield;
            }</Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74973")]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    public Task TestNint_WithoutNumericIntPtr(LanguageVersion version)
        => TestMissingInRegularAndScriptAsync($$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" LanguageVersion="{{version.ToDisplayString()}}">
                    <Document>using System;
            class Program
            {
                [|nint|] _myfield;
            }</Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: FrameworkTypeInDeclaration));

    [Fact]
    public async Task FieldDeclarationWithInitializer()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                [|string|] _myfield = 5;
            }
            """, """
            using System;
            class Program
            {
                String _myfield = 5;
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task DelegateDeclaration()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                public delegate [|int|] PerformCalculation(int x, int y);
            }
            """, """
            using System;
            class Program
            {
                public delegate Int32 PerformCalculation(int x, int y);
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task PropertyDeclaration()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                public [|long|] MyProperty { get; set; }
            }
            """, """
            using System;
            class Program
            {
                public Int64 MyProperty { get; set; }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task GenericPropertyDeclaration()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            using System.Collections.Generic;
            class Program
            {
                public List<[|long|]> MyProperty { get; set; }
            }
            """, """
            using System;
            using System.Collections.Generic;
            class Program
            {
                public List<Int64> MyProperty { get; set; }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task QualifiedReplacementInGenericTypeParameter()
    {
        await TestInRegularAndScriptAsync("""
            using System.Collections.Generic;
            class Program
            {
                public List<[|long|]> MyProperty { get; set; }
            }
            """, """
            using System.Collections.Generic;
            class Program
            {
                public List<System.Int64> MyProperty { get; set; }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MethodDeclarationReturnType()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                public [|long|] Method() { }
            }
            """, """
            using System;
            class Program
            {
                public Int64 Method() { }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MethodDeclarationParameters()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                public void Method([|double|] d) { }
            }
            """, """
            using System;
            class Program
            {
                public void Method(Double d) { }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task GenericMethodInvocation()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                public void Method<T>() { }
                public void Test() { Method<[|int|]>(); }
            }
            """, """
            using System;
            class Program
            {
                public void Method<T>() { }
                public void Test() { Method<Int32>(); }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task LocalDeclaration()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    [|int|] f = 5;
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    Int32 f = 5;
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MemberAccess()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    Console.Write([|int|].MaxValue);
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    Console.Write(Int32.MaxValue);
                }
            }
            """, options: FrameworkTypeInMemberAccess);
    }

    [Fact]
    public async Task MemberAccess2()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    var x = [|int|].Parse("1");
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    var x = Int32.Parse("1");
                }
            }
            """, options: FrameworkTypeInMemberAccess);
    }

    [Fact]
    public async Task DocCommentTriviaCrefExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                /// <see cref="[|int|].MaxValue"/>
                void Method()
                {
                }
            }
            """, """
            using System;
            class Program
            {
                /// <see cref="Int32.MaxValue"/>
                void Method()
                {
                }
            }
            """, options: FrameworkTypeInMemberAccess);
    }

    [Fact]
    public async Task DefaultExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    var v = default([|int|]);
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    var v = default(Int32);
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TypeOfExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    var v = typeof([|int|]);
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    var v = typeof(Int32);
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task NameOfExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    var v = nameof([|int|]);
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    var v = nameof(Int32);
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task FormalParametersWithinLambdaExression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    Func<int, int> func3 = ([|int|] z) => z + 1;
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    Func<int, int> func3 = (Int32 z) => z + 1;
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task DelegateMethodExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    Func<int, int> func7 = delegate ([|int|] dx) { return dx + 1; };
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    Func<int, int> func7 = delegate (Int32 dx) { return dx + 1; };
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ObjectCreationExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    string s2 = new [|string|]('c', 1);
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    string s2 = new String('c', 1);
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ArrayDeclaration()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    [|int|][] k = new int[4];
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    Int32[] k = new int[4];
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ArrayInitializer()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    int[] k = new [|int|][] { 1, 2, 3 };
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    int[] k = new Int32[] { 1, 2, 3 };
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MultiDimentionalArrayAsGenericTypeParameter()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            using System.Collections.Generic;
            class Program
            {
                void Method()
                {
                    List<[|string|][][,][,,,]> a;
                }
            }
            """, """
            using System;
            using System.Collections.Generic;
            class Program
            {
                void Method()
                {
                    List<String[][,][,,,]> a;
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ForStatement()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    for ([|int|] j = 0; j < 4; j++) { }
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    for (Int32 j = 0; j < 4; j++) { }
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ForeachStatement()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    foreach ([|int|] item in new int[] { 1, 2, 3 }) { }
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    foreach (Int32 item in new int[] { 1, 2, 3 }) { }
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task LeadingTrivia()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    // this is a comment
                    [|int|] x = 5;
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    // this is a comment
                    Int32 x = 5;
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TrailingTrivia()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                void Method()
                {
                    [|int|] /* 2 */ x = 5;
                }
            }
            """, """
            using System;
            class Program
            {
                void Method()
                {
                    Int32 /* 2 */ x = 5;
                }
            }
            """, options: FrameworkTypeInDeclaration);
    }
}
