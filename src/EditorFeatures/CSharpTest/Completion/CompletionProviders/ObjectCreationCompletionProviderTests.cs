// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class ObjectCreationCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(ObjectCreationCompletionProvider);

    [Fact]
    public async Task InObjectCreation()
    {
        var markup = @"
class MyGeneric<T> { }

void goo()
{
   MyGeneric<string> goo = new $$
}";

        await VerifyItemExistsAsync(markup, "MyGeneric<string>");
    }

    [Fact]
    public async Task NotInAnonymousTypeObjectCreation1()
    {
        var markup = @"
class C
{
    void M()
    {
        var x = new[] { new { Goo = ""asdf"", Bar = 1 }, new $$
    }
}";

        await VerifyItemIsAbsentAsync(markup, "<anonymous type: string Goo, int Bar>");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854497")]
    public async Task NotVoid()
    {
        var markup = @"
class C
{
    void M()
    {
        var x = new $$
    }
}";

        await VerifyItemIsAbsentAsync(markup, "void");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    public async Task InYieldReturn()
    {
        var markup =
@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<FieldAccessException> M()
    {
        yield return new $$
    }
}";
        await VerifyItemExistsAsync(markup, "FieldAccessException");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    public async Task InAsyncMethodReturnStatement()
    {
        var markup =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<FieldAccessException> M()
    {
        await Task.Delay(1);
        return new $$
    }
}";
        await VerifyItemExistsAsync(markup, "FieldAccessException");
    }

    [Fact]
    public async Task InAsyncMethodReturnValueTask()
    {
        var markup =
@"using System;
using System.Threading.Tasks;

class Program
{
    async ValueTask&lt;string&gt; M2Async()
    {
        return new $$;
    }
}";
        await VerifyItemExistsAsync(MakeMarkup(markup), "string");
    }

    [Fact]
    public async Task IsCommitCharacterTest()
    {
        const string markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d = new $$
    }
}";

        await VerifyCommitCharactersAsync(markup, textTypedSoFar: "",
            validChars: [' ', '(', '{', '['],
            invalidChars: ['x', ',', '#']);
    }

    [Fact]
    public void IsTextualTriggerCharacterTest()
    {
        VerifyTextualTriggerCharacter("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
        VerifyTextualTriggerCharacter("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
        VerifyTextualTriggerCharacter("Abc $$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        VerifyTextualTriggerCharacter("Abc$$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        VerifyTextualTriggerCharacter("Abc$$.", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
    }

    [Fact]
    public async Task SendEnterThroughToEditorTest()
    {
        const string markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d = new $$
    }
}";

        await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
        await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
        await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    public async Task SuggestAlias()
    {
        var markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}";
        await VerifyItemExistsAsync(markup, "D");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    public async Task SuggestAlias2()
    {
        var markup = @"
namespace N
{
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}
}

";
        await VerifyItemExistsAsync(markup, "D");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075275")]
    public async Task CommitAlias()
    {
        var markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}";

        var expected = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new D(
    }
}";
        await VerifyProviderCommitAsync(markup, "D", expected, '(');
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
    public async Task AfterNewFollowedByAssignment()
    {
        var markup = @"
class Location {}
enum EAB { A, B }
class Goo
{
    Location Loc {get; set;}
    EAB E {get; set;}

    void stuff()
    {
        var x = new Goo
            {
                Loc = new $$
                E = EAB.A
            };
    }
}

";
        await VerifyItemExistsAsync(markup, "Location");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
    public async Task AfterNewFollowedByAssignment_GrandParentIsSimpleAssignment()
    {
        var markup = @"
class Program
{
    static void Main(string[] args)
    {
        Program p = new $$
        bool b = false;
    }
}";
        await VerifyItemExistsAsync(markup, "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public async Task AfterNewFollowedBySimpleAssignment_GrandParentIsEqualsValueClause()
    {
        var markup = @"
class Program
{
    static void Main(string[] args)
    {
        bool b;
        Program p = new $$
        b = false;
    }
}";
        await VerifyItemExistsAsync(markup, "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public async Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause()
    {
        var markup = @"
class Program
{
    static void Main(string[] args)
    {
        int i;
        Program p = new $$
        i += 5;
    }
}";
        await VerifyItemExistsAsync(markup, "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public async Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause2()
    {
        var markup = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1000;
        Program p = new $$
        i <<= 4;
    }
}";
        await VerifyItemExistsAsync(markup, "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task CommitObjectWithParenthesis1()
    {
        var markup = @"
class C
{
    void M1()
    {
        object o = new $$
    }
}";

        var expected = @"
class C
{
    void M1()
    {
        object o = new object(
    }
}";

        await VerifyProviderCommitAsync(markup, "object", expected, '(');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task CommitObjectWithParenthesis2()
    {
        var markup = @"
class C
{
    void M1()
    {
        M2(new $$
    }

    void M2(object o) { }
}";

        var expected = @"
class C
{
    void M1()
    {
        M2(new object(
    }

    void M2(object o) { }
}";

        await VerifyProviderCommitAsync(markup, "object", expected, '(');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task DoNotCommitObjectWithOpenBrace1()
    {
        var markup = @"
class C
{
    void M1()
    {
        object o = new $$
    }
}";

        var expected = @"
class C
{
    void M1()
    {
        object o = new {
    }
}";

        await VerifyProviderCommitAsync(markup, "object", expected, '{');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task DoNotCommitObjectWithOpenBrace2()
    {
        var markup = @"
class C
{
    void M1()
    {
        M2(new $$
    }

    void M2(object o) { }
}";

        var expected = @"
class C
{
    void M1()
    {
        M2(new {
    }

    void M2(object o) { }
}";

        await VerifyProviderCommitAsync(markup, "object", expected, '{');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public async Task InExpressionBodiedProperty()
    {
        var markup =
@"class C
{
    object Object => new $$
}
";
        await VerifyItemExistsAsync(markup, "object");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public async Task InExpressionBodiedMethod()
    {
        var markup =
@"class C
{
    object GetObject() => new $$
}
";
        await VerifyItemExistsAsync(markup, "object");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15804")]
    public async Task BeforeAttributeParsedAsImplicitArray()
    {
        var markup =
@"class Program
{
    Program p = new $$ 

    [STAThread]
    static void Main() { }
}
";
        await VerifyItemExistsAsync(markup, "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14084")]
    public async Task InMethodCallBeforeAssignment1()
    {
        var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            object o;
            string s;

            Test(new $$
            o = s;
        }
        static void Test(TimeSpan t, TimeSpan t2) { }
    }
}
";
        await VerifyItemExistsAsync(markup, "TimeSpan");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14084")]
    public async Task InMethodCallBeforeAssignment2()
    {
        var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            object o;
            string s;

            Test(new TimeSpan(), new $$
            o = s;
        }
        static void Test(TimeSpan t, TimeSpan t2) { }
    }
}
";
        await VerifyItemExistsAsync(markup, "TimeSpan");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument1()
    {
        var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; }

        static void Main(string[] args)
        {
            Bar = new $$
        }
    }

    class Bar
    {
    }
}
";
        await VerifyItemExistsAsync(markup, "List<Bar>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument2()
    {
        var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; } = new $$
    }

    class Bar
    {
    }
}
";
        await VerifyItemExistsAsync(markup, "List<Bar>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument3()
    {
        var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; } => new $$
    }

    class Bar
    {
    }
}
";
        await VerifyItemExistsAsync(markup, "List<Bar>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument4()
    {
        var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static C<A> B { get; set; }
        static C<B> A { get; set; }

        static void Main(string[] args)
        {
            B = new $$
        }
    }
    class A { }
    class B { }
    class C<T> { }
}
";
        await VerifyItemExistsAsync(markup, "C<A>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21674")]
    public async Task PropertyWithSameNameAsOtherType()
    {
        var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static A B { get; set; }
        static B A { get; set; }

        static void Main()
        {
            B = new $$
        }
    }
    class A { }
    class B { }
}
";
        await VerifyItemExistsAsync(markup, "A");
    }

    [Fact]
    public async Task NullableTypeCreation()
    {
        var markup =
@"#nullable enable
namespace ConsoleApplication1
{
    class Program
    {
        void M()
        {
            object? o;
            o = new $$
        }
    }
}
";
        await VerifyItemExistsAsync(markup, "object");
    }

    [Fact]
    public async Task NullableTypeCreation_AssignedNull()
    {
        var markup =
@"#nullable enable
namespace ConsoleApplication1
{
    class Program
    {
        void M()
        {
            object? o = null;
            o = new $$
        }
    }
}
";
        await VerifyItemExistsAsync(markup, "object");
    }

    [Fact]
    public async Task NullableTypeCreation_NestedNull()
    {
        var markup =
@"#nullable enable

using System.Collections.Generic;

namespace ConsoleApplication1
{
    class Program
    {
        void M()
        {
            List<object?> l;
            l = new $$
        }
    }
}
";
        await VerifyItemExistsAsync(markup, "List<object?>");
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateObjectAndCommitWithCustomizedCommitChar(char commitChar)
    {
        var markup = @"
class Program
{
    void Bar()
    {
        object o = new $$
    }
}";
        var expectedMark = $@"
class Program
{{
    void Bar()
    {{
        object o = new object(){commitChar}
    }}
}}";
        await VerifyProviderCommitAsync(markup, "object", expectedMark, commitChar: commitChar);
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateNullableObjectAndCommitWithCustomizedCommitChar(char commitChar)
    {
        var markup = @"
class Program
{
    void Bar()
    {
        object? o = new $$
    }
}";
        var expectedMark = $@"
class Program
{{
    void Bar()
    {{
        object? o = new object(){commitChar}
    }}
}}";
        await VerifyProviderCommitAsync(markup, "object", expectedMark, commitChar: commitChar);
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateStringAsLocalAndCommitWithCustomizedCommitChar(char commitChar)
    {
        var markup = @"
class Program
{
    void Bar()
    {
        string o = new $$
    }
}";
        var expectedMark = $@"
class Program
{{
    void Bar()
    {{
        string o = new string(){commitChar}
    }}
}}";
        await VerifyProviderCommitAsync(markup, "string", expectedMark, commitChar: commitChar);
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateGenericListAsLocalAndCommitWithCustomizedChar(char commitChar)
    {
        var markup = @"
using System.Collections.Generic;
class Program
{
    void Bar()
    {
        List<int> o = new $$
    }
}";
        var expectedMark = $@"
using System.Collections.Generic;
class Program
{{
    void Bar()
    {{
        List<int> o = new List<int>(){commitChar}
    }}
}}";
        await VerifyProviderCommitAsync(markup, "List<int>", expectedMark, commitChar: commitChar);
    }

    [Fact]
    public async Task CreateGenericListAsFieldAndCommitWithSemicolon()
    {
        var markup = @"
using System.Collections.Generic;
class Program
{
    private List<int> o = new $$
}";
        var expectedMark = @"
using System.Collections.Generic;
class Program
{
    private List<int> o = new List<int>();
}";
        await VerifyProviderCommitAsync(markup, "List<int>", expectedMark, commitChar: ';');
    }

    private static string MakeMarkup(string source, string languageVersion = "Preview")
    {
        return $$"""
<Workspace>
    <Project Language="C#" AssemblyName="Assembly" CommonReferencesNet6="true" LanguageVersion="{{languageVersion}}">
        <Document FilePath="Test.cs">
{{source}}
        </Document>
    </Project>
</Workspace>
""";
    }
}
