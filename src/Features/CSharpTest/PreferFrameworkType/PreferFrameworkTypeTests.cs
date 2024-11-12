// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.PreferFrameworkType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PreferFrameworkType;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)]
public partial class PreferFrameworkTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public PreferFrameworkTypeTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpPreferFrameworkTypeDiagnosticAnalyzer(), new PreferFrameworkTypeCodeFixProvider());

    private readonly CodeStyleOption2<bool> onWithInfo = new CodeStyleOption2<bool>(true, NotificationOption2.Suggestion);
    private readonly CodeStyleOption2<bool> offWithInfo = new CodeStyleOption2<bool>(false, NotificationOption2.Suggestion);

    private OptionsCollection NoFrameworkType
        => new OptionsCollection(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, onWithInfo },
        };

    private OptionsCollection FrameworkTypeEverywhere
        => new OptionsCollection(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, offWithInfo },
        };

    private OptionsCollection FrameworkTypeInDeclaration
        => new OptionsCollection(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, onWithInfo },
        };

    private OptionsCollection FrameworkTypeInMemberAccess
        => new OptionsCollection(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption2.Suggestion },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, offWithInfo },
        };

    [Fact]
    public async Task NotWhenOptionsAreNotSet()
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|int|] x = 1;
    }
}", new TestParameters(options: NoFrameworkType));
    }

    [Fact]
    public async Task NotOnDynamic()
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|dynamic|] x = 1;
    }
}", new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Fact]
    public async Task NotOnSystemVoid()
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    [|void|] Method()
    {
    }
}", new TestParameters(options: FrameworkTypeEverywhere));
    }

    [Fact]
    public async Task NotOnUserdefinedType()
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Program|] p;
    }
}", new TestParameters(options: FrameworkTypeEverywhere));
    }

    [Fact]
    public async Task NotOnFrameworkType()
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Int32|] p;
    }
}", new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Fact]
    public async Task NotOnQualifiedTypeSyntax()
    {
        await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Method()
    {
        [|System.Int32|] p;
    }
}", new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Fact]
    public async Task NotOnFrameworkTypeWithNoPredefinedKeywordEquivalent()
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|List|]<int> p;
    }
}", new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Fact]
    public async Task NotOnIdentifierThatIsNotTypeSyntax()
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        int [|p|];
    }
}", new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Fact]
    public async Task QualifiedReplacementWhenNoUsingFound()
    {
        var code =
@"class Program
{
    [|string|] _myfield = 5;
}";

        var expected =
@"class Program
{
    System.String _myfield = 5;
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task FieldDeclaration()
    {
        var code =
@"using System;
class Program
{
    [|int|] _myfield;
}";

        var expected =
@"using System;
class Program
{
    Int32 _myfield;
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TestNint_WithNumericIntPtr_CSharp11()
    {
        var code =
@"<Workspace>
    <Project Language=""C#"" CommonReferencesNet7=""true"" LanguageVersion=""11"">
        <Document>using System;
class Program
{
    [|nint|] _myfield;
}</Document>
    </Project>
</Workspace>";

        var expected =
@"using System;
class Program
{
    IntPtr _myfield;
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TestNint_WithNumericIntPtr_CSharp8()
    {
        var code =
@"<Workspace>
    <Project Language=""C#"" CommonReferencesNet7=""true"" LanguageVersion=""8"">
        <Document>using System;
class Program
{
    [|nint|] _myfield;
}</Document>
    </Project>
</Workspace>";
        await TestMissingInRegularAndScriptAsync(code, new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Fact]
    public async Task TestNint_WithoutNumericIntPtr()
    {
        var code =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" LanguageVersion=""11"">
        <Document>using System;
class Program
{
    [|nint|] _myfield;
}</Document>
    </Project>
</Workspace>";
        await TestMissingInRegularAndScriptAsync(code, new TestParameters(options: FrameworkTypeInDeclaration));
    }

    [Fact]
    public async Task FieldDeclarationWithInitializer()
    {
        var code =
@"using System;
class Program
{
    [|string|] _myfield = 5;
}";

        var expected =
@"using System;
class Program
{
    String _myfield = 5;
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task DelegateDeclaration()
    {
        var code =
@"using System;
class Program
{
    public delegate [|int|] PerformCalculation(int x, int y);
}";

        var expected =
@"using System;
class Program
{
    public delegate Int32 PerformCalculation(int x, int y);
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task PropertyDeclaration()
    {
        var code =
@"using System;
class Program
{
    public [|long|] MyProperty { get; set; }
}";

        var expected =
@"using System;
class Program
{
    public Int64 MyProperty { get; set; }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task GenericPropertyDeclaration()
    {
        var code =
@"using System;
using System.Collections.Generic;
class Program
{
    public List<[|long|]> MyProperty { get; set; }
}";

        var expected =
@"using System;
using System.Collections.Generic;
class Program
{
    public List<Int64> MyProperty { get; set; }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task QualifiedReplacementInGenericTypeParameter()
    {
        var code =
@"using System.Collections.Generic;
class Program
{
    public List<[|long|]> MyProperty { get; set; }
}";

        var expected =
@"using System.Collections.Generic;
class Program
{
    public List<System.Int64> MyProperty { get; set; }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MethodDeclarationReturnType()
    {
        var code =
@"using System;
class Program
{
    public [|long|] Method() { }
}";

        var expected =
@"using System;
class Program
{
    public Int64 Method() { }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MethodDeclarationParameters()
    {
        var code =
@"using System;
class Program
{
    public void Method([|double|] d) { }
}";

        var expected =
@"using System;
class Program
{
    public void Method(Double d) { }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task GenericMethodInvocation()
    {
        var code =
@"using System;
class Program
{
    public void Method<T>() { }
    public void Test() { Method<[|int|]>(); }
}";

        var expected =
@"using System;
class Program
{
    public void Method<T>() { }
    public void Test() { Method<Int32>(); }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task LocalDeclaration()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        [|int|] f = 5;
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        Int32 f = 5;
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MemberAccess()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        Console.Write([|int|].MaxValue);
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        Console.Write(Int32.MaxValue);
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInMemberAccess);
    }

    [Fact]
    public async Task MemberAccess2()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        var x = [|int|].Parse(""1"");
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        var x = Int32.Parse(""1"");
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInMemberAccess);
    }

    [Fact]
    public async Task DocCommentTriviaCrefExpression()
    {
        var code =
@"using System;
class Program
{
    /// <see cref=""[|int|].MaxValue""/>
    void Method()
    {
    }
}";

        var expected =
@"using System;
class Program
{
    /// <see cref=""Int32.MaxValue""/>
    void Method()
    {
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInMemberAccess);
    }

    [Fact]
    public async Task DefaultExpression()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        var v = default([|int|]);
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        var v = default(Int32);
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TypeOfExpression()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        var v = typeof([|int|]);
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        var v = typeof(Int32);
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task NameOfExpression()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        var v = nameof([|int|]);
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        var v = nameof(Int32);
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task FormalParametersWithinLambdaExression()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        Func<int, int> func3 = ([|int|] z) => z + 1;
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        Func<int, int> func3 = (Int32 z) => z + 1;
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task DelegateMethodExpression()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        Func<int, int> func7 = delegate ([|int|] dx) { return dx + 1; };
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        Func<int, int> func7 = delegate (Int32 dx) { return dx + 1; };
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ObjectCreationExpression()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        string s2 = new [|string|]('c', 1);
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        string s2 = new String('c', 1);
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ArrayDeclaration()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        [|int|][] k = new int[4];
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        Int32[] k = new int[4];
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ArrayInitializer()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        int[] k = new [|int|][] { 1, 2, 3 };
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        int[] k = new Int32[] { 1, 2, 3 };
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task MultiDimentionalArrayAsGenericTypeParameter()
    {
        var code =
@"using System;
using System.Collections.Generic;
class Program
{
    void Method()
    {
        List<[|string|][][,][,,,]> a;
    }
}";

        var expected =
@"using System;
using System.Collections.Generic;
class Program
{
    void Method()
    {
        List<String[][,][,,,]> a;
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ForStatement()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        for ([|int|] j = 0; j < 4; j++) { }
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        for (Int32 j = 0; j < 4; j++) { }
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task ForeachStatement()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        foreach ([|int|] item in new int[] { 1, 2, 3 }) { }
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        foreach (Int32 item in new int[] { 1, 2, 3 }) { }
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task LeadingTrivia()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        // this is a comment
        [|int|] x = 5;
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        // this is a comment
        Int32 x = 5;
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }

    [Fact]
    public async Task TrailingTrivia()
    {
        var code =
@"using System;
class Program
{
    void Method()
    {
        [|int|] /* 2 */ x = 5;
    }
}";

        var expected =
@"using System;
class Program
{
    void Method()
    {
        Int32 /* 2 */ x = 5;
    }
}";
        await TestInRegularAndScriptAsync(code, expected, options: FrameworkTypeInDeclaration);
    }
}
