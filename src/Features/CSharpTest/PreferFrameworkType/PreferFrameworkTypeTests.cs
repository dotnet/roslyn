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
    public Task QualifiedReplacementWhenNoUsingFound()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                [|string|] _myfield = 5;
            }
            """, """
            class Program
            {
                System.String _myfield = 5;
            }
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task FieldDeclaration()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task TestNint_WithNumericIntPtr_CSharp11()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task TestNint_WithNumericIntPtr_CSharp8()
        => TestMissingInRegularAndScriptAsync("""
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

    [Theory]
    [InlineData("CommonReferences")]
    [InlineData("CommonReferencesNet7")]
    public Task TestNint_WithoutNumericIntPtr_CSharp10(string references)
        => TestMissingInRegularAndScriptAsync($$"""
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
    public Task FieldDeclarationWithInitializer()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task DelegateDeclaration()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task PropertyDeclaration()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task GenericPropertyDeclaration()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task QualifiedReplacementInGenericTypeParameter()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task MethodDeclarationReturnType()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task MethodDeclarationParameters()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task GenericMethodInvocation()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task LocalDeclaration()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task MemberAccess()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInMemberAccess));

    [Fact]
    public Task MemberAccess2()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInMemberAccess));

    [Fact]
    public Task DocCommentTriviaCrefExpression()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInMemberAccess));

    [Fact]
    public Task DefaultExpression()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task TypeOfExpression()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task NameOfExpression()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task FormalParametersWithinLambdaExression()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task DelegateMethodExpression()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task ObjectCreationExpression()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task ArrayDeclaration()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task ArrayInitializer()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task MultiDimentionalArrayAsGenericTypeParameter()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task ForStatement()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task ForeachStatement()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task LeadingTrivia()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));

    [Fact]
    public Task TrailingTrivia()
        => TestInRegularAndScriptAsync("""
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
            """, new(options: FrameworkTypeInDeclaration));
}
