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
public sealed class ObjectCreationCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(ObjectCreationCompletionProvider);

    [Fact]
    public async Task InObjectCreation()
    {
        await VerifyItemExistsAsync(@"
class MyGeneric<T> { }

void goo()
{
   MyGeneric<string> goo = new $$
}", "MyGeneric<string>");
    }

    [Fact]
    public async Task NotInAnonymousTypeObjectCreation1()
    {
        await VerifyItemIsAbsentAsync(@"
class C
{
    void M()
    {
        var x = new[] { new { Goo = ""asdf"", Bar = 1 }, new $$
    }
}", "<anonymous type: string Goo, int Bar>");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854497")]
    public async Task NotVoid()
    {
        await VerifyItemIsAbsentAsync(@"
class C
{
    void M()
    {
        var x = new $$
    }
}", "void");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    public async Task InYieldReturn()
    {
        await VerifyItemExistsAsync(@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<FieldAccessException> M()
    {
        yield return new $$
    }
}", "FieldAccessException");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    public async Task InAsyncMethodReturnStatement()
    {
        await VerifyItemExistsAsync(@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<FieldAccessException> M()
    {
        await Task.Delay(1);
        return new $$
    }
}", "FieldAccessException");
    }

    [Fact]
    public async Task InAsyncMethodReturnValueTask()
    {
        await VerifyItemExistsAsync(MakeMarkup(@"using System;
using System.Threading.Tasks;

class Program
{
    async ValueTask&lt;string&gt; M2Async()
    {
        return new $$;
    }
}"), "string");
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
        await VerifyItemExistsAsync(@"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}", "D");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    public async Task SuggestAlias2()
    {
        await VerifyItemExistsAsync(@"
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

", "D");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075275")]
    public async Task CommitAlias()
    {
        await VerifyProviderCommitAsync(@"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}", "D", @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new D(
    }
}", '(');
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
    public async Task AfterNewFollowedByAssignment()
    {
        await VerifyItemExistsAsync(@"
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

", "Location");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
    public async Task AfterNewFollowedByAssignment_GrandParentIsSimpleAssignment()
    {
        await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        Program p = new $$
        bool b = false;
    }
}", "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public async Task AfterNewFollowedBySimpleAssignment_GrandParentIsEqualsValueClause()
    {
        await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        bool b;
        Program p = new $$
        b = false;
    }
}", "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public async Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause()
    {
        await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        int i;
        Program p = new $$
        i += 5;
    }
}", "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public async Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause2()
    {
        await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        int i = 1000;
        Program p = new $$
        i <<= 4;
    }
}", "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task CommitObjectWithParenthesis1()
    {
        await VerifyProviderCommitAsync(@"
class C
{
    void M1()
    {
        object o = new $$
    }
}", "object", @"
class C
{
    void M1()
    {
        object o = new object(
    }
}", '(');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task CommitObjectWithParenthesis2()
    {
        await VerifyProviderCommitAsync(@"
class C
{
    void M1()
    {
        M2(new $$
    }

    void M2(object o) { }
}", "object", @"
class C
{
    void M1()
    {
        M2(new object(
    }

    void M2(object o) { }
}", '(');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task DoNotCommitObjectWithOpenBrace1()
    {
        await VerifyProviderCommitAsync(@"
class C
{
    void M1()
    {
        object o = new $$
    }
}", "object", @"
class C
{
    void M1()
    {
        object o = new {
    }
}", '{');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public async Task DoNotCommitObjectWithOpenBrace2()
    {
        await VerifyProviderCommitAsync(@"
class C
{
    void M1()
    {
        M2(new $$
    }

    void M2(object o) { }
}", "object", @"
class C
{
    void M1()
    {
        M2(new {
    }

    void M2(object o) { }
}", '{');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public async Task InExpressionBodiedProperty()
    {
        await VerifyItemExistsAsync(@"class C
{
    object Object => new $$
}
", "object");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public async Task InExpressionBodiedMethod()
    {
        await VerifyItemExistsAsync(@"class C
{
    object GetObject() => new $$
}
", "object");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15804")]
    public async Task BeforeAttributeParsedAsImplicitArray()
    {
        await VerifyItemExistsAsync(@"class Program
{
    Program p = new $$ 

    [STAThread]
    static void Main() { }
}
", "Program");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14084")]
    public async Task InMethodCallBeforeAssignment1()
    {
        await VerifyItemExistsAsync(@"namespace ConsoleApplication1
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
", "TimeSpan");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14084")]
    public async Task InMethodCallBeforeAssignment2()
    {
        await VerifyItemExistsAsync(@"namespace ConsoleApplication1
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
", "TimeSpan");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument1()
    {
        await VerifyItemExistsAsync(@"namespace ConsoleApplication1
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
", "List<Bar>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument2()
    {
        await VerifyItemExistsAsync(@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; } = new $$
    }

    class Bar
    {
    }
}
", "List<Bar>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument3()
    {
        await VerifyItemExistsAsync(@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; } => new $$
    }

    class Bar
    {
    }
}
", "List<Bar>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public async Task InPropertyWithSameNameAsGenericTypeArgument4()
    {
        await VerifyItemExistsAsync(@"namespace ConsoleApplication1
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
", "C<A>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21674")]
    public async Task PropertyWithSameNameAsOtherType()
    {
        await VerifyItemExistsAsync(@"namespace ConsoleApplication1
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
", "A");
    }

    [Fact]
    public async Task NullableTypeCreation()
    {
        await VerifyItemExistsAsync(@"#nullable enable
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
", "object");
    }

    [Fact]
    public async Task NullableTypeCreation_AssignedNull()
    {
        await VerifyItemExistsAsync(@"#nullable enable
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
", "object");
    }

    [Fact]
    public async Task NullableTypeCreation_NestedNull()
    {
        await VerifyItemExistsAsync(@"#nullable enable

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
", "List<object?>");
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateObjectAndCommitWithCustomizedCommitChar(char commitChar)
    {
        await VerifyProviderCommitAsync(@"
class Program
{
    void Bar()
    {
        object o = new $$
    }
}", "object", $@"
class Program
{{
    void Bar()
    {{
        object o = new object(){commitChar}
    }}
}}", commitChar: commitChar);
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateNullableObjectAndCommitWithCustomizedCommitChar(char commitChar)
    {
        await VerifyProviderCommitAsync(@"
class Program
{
    void Bar()
    {
        object? o = new $$
    }
}", "object", $@"
class Program
{{
    void Bar()
    {{
        object? o = new object(){commitChar}
    }}
}}", commitChar: commitChar);
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateStringAsLocalAndCommitWithCustomizedCommitChar(char commitChar)
    {
        await VerifyProviderCommitAsync(@"
class Program
{
    void Bar()
    {
        string o = new $$
    }
}", "string", $@"
class Program
{{
    void Bar()
    {{
        string o = new string(){commitChar}
    }}
}}", commitChar: commitChar);
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task CreateGenericListAsLocalAndCommitWithCustomizedChar(char commitChar)
    {
        await VerifyProviderCommitAsync(@"
using System.Collections.Generic;
class Program
{
    void Bar()
    {
        List<int> o = new $$
    }
}", "List<int>", $@"
using System.Collections.Generic;
class Program
{{
    void Bar()
    {{
        List<int> o = new List<int>(){commitChar}
    }}
}}", commitChar: commitChar);
    }

    [Fact]
    public async Task CreateGenericListAsFieldAndCommitWithSemicolon()
    {
        await VerifyProviderCommitAsync(@"
using System.Collections.Generic;
class Program
{
    private List<int> o = new $$
}", "List<int>", @"
using System.Collections.Generic;
class Program
{
    private List<int> o = new List<int>();
}", commitChar: ';');
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
