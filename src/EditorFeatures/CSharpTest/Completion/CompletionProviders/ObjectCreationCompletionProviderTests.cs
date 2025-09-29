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
    public Task InObjectCreation()
        => VerifyItemExistsAsync("""
            class MyGeneric<T> { }

            void goo()
            {
               MyGeneric<string> goo = new $$
            }
            """, "MyGeneric<string>");

    [Fact]
    public Task NotInAnonymousTypeObjectCreation1()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                void M()
                {
                    var x = new[] { new { Goo = "asdf", Bar = 1 }, new $$
                }
            }
            """, "<anonymous type: string Goo, int Bar>");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854497")]
    public Task NotVoid()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                void M()
                {
                    var x = new $$
                }
            }
            """, "void");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    public Task InYieldReturn()
        => VerifyItemExistsAsync("""
            using System;
            using System.Collections.Generic;

            class Program
            {
                IEnumerable<FieldAccessException> M()
                {
                    yield return new $$
                }
            }
            """, "FieldAccessException");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    public Task InAsyncMethodReturnStatement()
        => VerifyItemExistsAsync("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<FieldAccessException> M()
                {
                    await Task.Delay(1);
                    return new $$
                }
            }
            """, "FieldAccessException");

    [Fact]
    public Task InAsyncMethodReturnValueTask()
        => VerifyItemExistsAsync(MakeMarkup("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async ValueTask&lt;string&gt; M2Async()
                {
                    return new $$;
                }
            }
            """), "string");

    [Fact]
    public Task IsCommitCharacterTest()
        => VerifyCommitCharactersAsync("""
            using D = System.Globalization.DigitShapes; 
            class Program
            {
                static void Main(string[] args)
                {
                    D d = new $$
                }
            }
            """, textTypedSoFar: "",
            validChars: [' ', '(', '{', '['],
            invalidChars: ['x', ',', '#']);

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
        const string markup = """
            using D = System.Globalization.DigitShapes; 
            class Program
            {
                static void Main(string[] args)
                {
                    D d = new $$
                }
            }
            """;

        await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
        await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
        await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    public Task SuggestAlias()
        => VerifyItemExistsAsync("""
            using D = System.Globalization.DigitShapes; 
            class Program
            {
                static void Main(string[] args)
                {
                    D d=  new $$
                }
            }
            """, "D");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    public Task SuggestAlias2()
        => VerifyItemExistsAsync("""
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
            """, "D");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075275")]
    public Task CommitAlias()
        => VerifyProviderCommitAsync("""
            using D = System.Globalization.DigitShapes; 
            class Program
            {
                static void Main(string[] args)
                {
                    D d=  new $$
                }
            }
            """, "D", """
            using D = System.Globalization.DigitShapes; 
            class Program
            {
                static void Main(string[] args)
                {
                    D d=  new D(
                }
            }
            """, '(');

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
    public Task AfterNewFollowedByAssignment()
        => VerifyItemExistsAsync("""
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
            """, "Location");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
    public Task AfterNewFollowedByAssignment_GrandParentIsSimpleAssignment()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    Program p = new $$
                    bool b = false;
                }
            }
            """, "Program");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public Task AfterNewFollowedBySimpleAssignment_GrandParentIsEqualsValueClause()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    bool b;
                    Program p = new $$
                    b = false;
                }
            }
            """, "Program");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int i;
                    Program p = new $$
                    i += 5;
                }
            }
            """, "Program");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2836")]
    public Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause2()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int i = 1000;
                    Program p = new $$
                    i <<= 4;
                }
            }
            """, "Program");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public Task CommitObjectWithParenthesis1()
        => VerifyProviderCommitAsync("""
            class C
            {
                void M1()
                {
                    object o = new $$
                }
            }
            """, "object", """
            class C
            {
                void M1()
                {
                    object o = new object(
                }
            }
            """, '(');

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public Task CommitObjectWithParenthesis2()
        => VerifyProviderCommitAsync("""
            class C
            {
                void M1()
                {
                    M2(new $$
                }

                void M2(object o) { }
            }
            """, "object", """
            class C
            {
                void M1()
                {
                    M2(new object(
                }

                void M2(object o) { }
            }
            """, '(');

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public Task DoNotCommitObjectWithOpenBrace1()
        => VerifyProviderCommitAsync("""
            class C
            {
                void M1()
                {
                    object o = new $$
                }
            }
            """, "object", """
            class C
            {
                void M1()
                {
                    object o = new {
                }
            }
            """, '{');

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4115")]
    public Task DoNotCommitObjectWithOpenBrace2()
        => VerifyProviderCommitAsync("""
            class C
            {
                void M1()
                {
                    M2(new $$
                }

                void M2(object o) { }
            }
            """, "object", """
            class C
            {
                void M1()
                {
                    M2(new {
                }

                void M2(object o) { }
            }
            """, '{');

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public Task InExpressionBodiedProperty()
        => VerifyItemExistsAsync("""
            class C
            {
                object Object => new $$
            }
            """, "object");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public Task InExpressionBodiedMethod()
        => VerifyItemExistsAsync("""
            class C
            {
                object GetObject() => new $$
            }
            """, "object");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15804")]
    public Task BeforeAttributeParsedAsImplicitArray()
        => VerifyItemExistsAsync("""
            class Program
            {
                Program p = new $$ 

                [STAThread]
                static void Main() { }
            }
            """, "Program");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14084")]
    public Task InMethodCallBeforeAssignment1()
        => VerifyItemExistsAsync("""
            namespace ConsoleApplication1
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
            """, "TimeSpan");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14084")]
    public Task InMethodCallBeforeAssignment2()
        => VerifyItemExistsAsync("""
            namespace ConsoleApplication1
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
            """, "TimeSpan");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public Task InPropertyWithSameNameAsGenericTypeArgument1()
        => VerifyItemExistsAsync("""
            namespace ConsoleApplication1
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
            """, "List<Bar>");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public Task InPropertyWithSameNameAsGenericTypeArgument2()
        => VerifyItemExistsAsync("""
            namespace ConsoleApplication1
            {
                class Program
                {
                    public static List<Bar> Bar { get; set; } = new $$
                }

                class Bar
                {
                }
            }
            """, "List<Bar>");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public Task InPropertyWithSameNameAsGenericTypeArgument3()
        => VerifyItemExistsAsync("""
            namespace ConsoleApplication1
            {
                class Program
                {
                    public static List<Bar> Bar { get; set; } => new $$
                }

                class Bar
                {
                }
            }
            """, "List<Bar>");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public Task InPropertyWithSameNameAsGenericTypeArgument4()
        => VerifyItemExistsAsync("""
            namespace ConsoleApplication1
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
            """, "C<A>");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21674")]
    public Task PropertyWithSameNameAsOtherType()
        => VerifyItemExistsAsync("""
            namespace ConsoleApplication1
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
            """, "A");

    [Fact]
    public Task NullableTypeCreation()
        => VerifyItemExistsAsync("""
            #nullable enable
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
            """, "object");

    [Fact]
    public Task NullableTypeCreation_AssignedNull()
        => VerifyItemExistsAsync("""
            #nullable enable
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
            """, "object");

    [Fact]
    public Task NullableTypeCreation_NestedNull()
        => VerifyItemExistsAsync("""
            #nullable enable

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
            """, "List<object?>");

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CreateObjectAndCommitWithCustomizedCommitChar(char commitChar)
        => VerifyProviderCommitAsync("""
            class Program
            {
                void Bar()
                {
                    object o = new $$
                }
            }
            """, "object", $$"""
            class Program
            {
                void Bar()
                {
                    object o = new object(){{commitChar}}
                }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CreateNullableObjectAndCommitWithCustomizedCommitChar(char commitChar)
        => VerifyProviderCommitAsync("""
            class Program
            {
                void Bar()
                {
                    object? o = new $$
                }
            }
            """, "object", $$"""
            class Program
            {
                void Bar()
                {
                    object? o = new object(){{commitChar}}
                }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CreateStringAsLocalAndCommitWithCustomizedCommitChar(char commitChar)
        => VerifyProviderCommitAsync("""
            class Program
            {
                void Bar()
                {
                    string o = new $$
                }
            }
            """, "string", $$"""
            class Program
            {
                void Bar()
                {
                    string o = new string(){{commitChar}}
                }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CreateGenericListAsLocalAndCommitWithCustomizedChar(char commitChar)
        => VerifyProviderCommitAsync("""
            using System.Collections.Generic;
            class Program
            {
                void Bar()
                {
                    List<int> o = new $$
                }
            }
            """, "List<int>", $$"""
            using System.Collections.Generic;
            class Program
            {
                void Bar()
                {
                    List<int> o = new List<int>(){{commitChar}}
                }
            }
            """, commitChar: commitChar);

    [Fact]
    public Task CreateGenericListAsFieldAndCommitWithSemicolon()
        => VerifyProviderCommitAsync("""
            using System.Collections.Generic;
            class Program
            {
                private List<int> o = new $$
            }
            """, "List<int>", """
            using System.Collections.Generic;
            class Program
            {
                private List<int> o = new List<int>();
            }
            """, commitChar: ';');

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
