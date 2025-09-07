// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class EnumAndCompletionListTagCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(EnumAndCompletionListTagCompletionProvider);

    [Fact]
    public async Task NullableEnum()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    Colors? d = $$
                    Colors c = Colors.Blue;
                }
            }
            """;
        var colors = """
            enum Colors
            {
                Red,
                Blue,
                Green,
            }
            """;
        var colorsLike = """
            readonly struct Colors
            {
                public static readonly Colors Red;
                public static readonly Colors Blue;
                public static readonly Colors Green;
            }
            """;

        await VerifyItemExistsAsync(markup + colors, "Colors");
        await VerifyItemIsAbsentAsync(markup + colorsLike, "Colors");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
    public async Task EditorBrowsable_EnumMemberAlways()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    Goo d = $$
                }
            }
            """;
        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public enum Goo
            {
                Member
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
    public async Task EditorBrowsable_EnumMemberNever()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    Goo d = $$
                }
            }
            """;
        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public enum Goo
            {
                Member
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
    public async Task EditorBrowsable_EnumMemberAdvanced()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    Goo d = $$
                }
            }
            """;
        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public enum Goo
            {
                Member
            }
            """;
        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854099")]
    public async Task NotInComment()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    Colors c = // $$
                }
            """;
        var colors = """
            enum Colors
            {
                Red,
                Blue,
                Green,
            }
            """;
        var colorsLike = """
            readonly struct Colors
            {
                public static readonly Colors Red;
                public static readonly Colors Blue;
                public static readonly Colors Green;
            }
            """;

        await VerifyNoItemsExistAsync(markup + colors);
        await VerifyNoItemsExistAsync(markup + colorsLike);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task InYieldReturnInMethod(string typeName)
    {
        var markup =
            $$"""
            using System;
            using System.Collections.Generic;

            class Program
            {
                IEnumerable<{{typeName}}> M()
                {
                    yield return $$
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, typeName);
        else
            await VerifyItemIsAbsentAsync(markup, typeName);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/30235")]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task InYieldReturnInLocalFunction(string typeName)
    {
        var markup =
            $$"""
            using System;
            using System.Collections.Generic;

            class Program
            {
                void M()
                {
                    IEnumerable<{{typeName}}> F()
                    {
                        yield return $$
                    }
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, typeName);
        else
            await VerifyItemIsAbsentAsync(markup, typeName);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task InAsyncMethodReturnStatement(string typeName)
    {
        var markup =
            $$"""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<{{typeName}}> M()
                {
                    await Task.Delay(1);
                    return $$
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, typeName);
        else
            await VerifyItemIsAbsentAsync(markup, typeName);
    }

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task InSimpleLambdaAfterArrow(string typeName)
    {
        var markup =
            $$"""
            using System;

            class Program
            {
                Func<bool, {{typeName}}> M()
                {
                    return _ => $$
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, typeName);
        else
            await VerifyItemIsAbsentAsync(markup, typeName);
    }

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task InParenthesizedLambdaAfterArrow(string typeName)
    {
        var markup =
            $$"""
            using System;

            class Program
            {
                Func<{{typeName}}> M()
                {
                    return () => $$
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, typeName);
        else
            await VerifyItemIsAbsentAsync(markup, typeName);
    }

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task NotInAnonymousMethodAfterParameterList(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            using System;

            class Program
            {
                Func<{{typeName}}> M()
                {
                    return delegate () $$
                }
            }
            """, typeName);

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task NotInSimpleLambdaAfterAsync(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            using System;

            class Program
            {
                Func<bool, {{typeName}}> M()
                {
                    return async $$ _ =>
                }
            }
            """, typeName);

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task NotInParenthesizedLambdaAfterAsync(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            using System;

            class Program
            {
                Func<{{typeName}}> M()
                {
                    return async $$ () =>
                }
            }
            """, typeName);

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task NotInAnonymousMethodAfterAsync(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            using System;

            class Program
            {
                Func<{{typeName}}> M()
                {
                    return async $$ delegate ()
                }
            }
            """, typeName);

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task NotInSimpleLambdaBlock(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            using System;

            class Program
            {
                Func<bool, {{typeName}}> M()
                {
                    return _ => { $$ }
                }
            }
            """, typeName);

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task NotInParenthesizedLambdaBlock(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            using System;

            class Program
            {
                Func<{{typeName}}> M()
                {
                    return () => { $$ }
                }
            }
            """, typeName);

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task NotInAnonymousMethodBlock(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            using System;

            class Program
            {
                Func<{{typeName}}> M()
                {
                    return delegate () { $$ }
                }
            }
            """, typeName);

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task InExpressionTreeSimpleLambdaAfterArrow(string typeName)
    {
        var markup =
            $$"""
            using System;
            using System.Linq.Expressions;

            class Program
            {
                Expression<Func<bool, {{typeName}}>> M()
                {
                    return _ => $$
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, typeName);
        else
            await VerifyItemIsAbsentAsync(markup, typeName);
    }

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task InExpressionTreeParenthesizedLambdaAfterArrow(string typeName)
    {
        var markup =
            $$"""
            using System;
            using System.Linq.Expressions;

            class Program
            {
                Expression<Func<{{typeName}}>> M()
                {
                    return () => $$
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, typeName);
        else
            await VerifyItemIsAbsentAsync(markup, typeName);
    }

    [Fact]
    public Task NoCompletionListTag()
        => VerifyNoItemsExistAsync("""
            using System;
            using System.Threading.Tasks;

            class C
            {

            }

            class Program
            {
                void Goo()
                {
                    C c = $$
                }
            }
            """);

    [Fact]
    public Task CompletionList()
        => VerifyItemExistsAsync("""
            using System;
            using System.Threading.Tasks;

            /// <completionlist cref="C"/>
            class C
            {

            }

            class Program
            {
                void Goo()
                {
                    C c = $$
                }
            }
            """, "C");

    [Fact]
    public Task CompletionListCrefToString()
        => VerifyItemExistsAsync("""
            using System;
            using System.Threading.Tasks;

            /// <completionlist cref="string"/>
            class C
            {

            }

            class Program
            {
                void Goo()
                {
                    C c = $$
                }
            }
            """, "string", glyph: Glyph.ClassPublic);

    [Fact]
    public Task CompletionListEmptyCref()
        => VerifyNoItemsExistAsync("""
            using System;
            using System.Threading.Tasks;

            /// <completionlist cref=""/>
            class C
            {

            }

            class Program
            {
                void Goo()
                {
                    C c = $$
                }
            }
            """);

    [Fact]
    public Task CompletionListInaccessibleType()
        => VerifyNoItemsExistAsync("""
            using System;
            using System.Threading.Tasks;

            /// <completionlist cref="C.Inner"/>
            class C
            {
                private class Inner
                {   
                }
            }

            class Program
            {
                void Goo()
                {
                    C c = $$
                }
            }
            """);

    [Fact]
    public Task CompletionListNotAType()
        => VerifyNoItemsExistAsync("""
            using System;
            using System.Threading.Tasks;

            /// <completionlist cref="C.Z()"/>
            class C
            {
                public void Z()
                {   
                }
            }

            class Program
            {
                void Goo()
                {
                    C c = $$
                }
            }
            """);

    [Fact]
    public async Task CompletionListContainingMembers()
    {
        var markup =
            """
            /// <completionlist cref="TypeContainer" />
             public class SomeType
             { }

             public static class TypeContainer
             {
                 public static SomeType Foo1 = new SomeType();
                 public static Program Foo2 = new Program();
             }

             class Program
             {
                 void Goo()
                 {
                     SomeType c = $$
                 }
             }
            """;
        await VerifyItemExistsAsync(markup, "TypeContainer");
        await VerifyItemExistsAsync(markup, "TypeContainer.Foo1");
        await VerifyItemExistsAsync(markup, "TypeContainer.Foo2");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    [InlineData("System.Globalization.DigitShapes")]
    [InlineData("System.DateTime")]
    public async Task SuggestAlias(string fullTypeName)
    {
        var markup = $$"""
            using D = {{fullTypeName}}; 
            class Program
            {
                static void Main(string[] args)
                {
                    D d=  $$
                }
            }
            """;

        if (fullTypeName == "System.Globalization.DigitShapes")
            await VerifyItemExistsAsync(markup, "D");
        else
            await VerifyItemIsAbsentAsync(markup, "D");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    [InlineData("System.Globalization.DigitShapes")]
    [InlineData("System.DateTime")]
    public async Task SuggestAlias2(string fullTypeName)
    {
        var markup = $$"""
            namespace N
            {
            using D = {{fullTypeName}}; 

            class Program
            {
                static void Main(string[] args)
                {
                    D d=  $$
                }
            }
            }
            """;

        if (fullTypeName == "System.Globalization.DigitShapes")
            await VerifyItemExistsAsync(markup, "D");
        else
            await VerifyItemIsAbsentAsync(markup, "D");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    [InlineData("System.Globalization.DigitShapes")]
    [InlineData("System.DateTime")]
    public async Task SuggestAlias3(string fullTypeName)
    {
        var markup = $$"""
            namespace N
            {
            using D = {{fullTypeName}}; 

            class Program
            {
                private void Goo({{fullTypeName}} shape)
                {
                }

                static void Main(string[] args)
                {
                    Goo($$
                }
            }
            }
            """;

        if (fullTypeName == "System.Globalization.DigitShapes")
            await VerifyItemExistsAsync(markup, "D");
        else
            await VerifyItemIsAbsentAsync(markup, "D");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
    public async Task NotInParameterNameContext()
    {
        var enumE = """
            enum E
            {
                a
            }
            """;
        var enumLikeE = """
            readonly struct E
            {
                public static readonly E a;
            }
            """;
        var markup = """
            class C
            {
                void goo(E first, E second) 
                {
                    goo(first: E.a, $$
                }
            }
            """;

        await VerifyItemIsAbsentAsync(enumE + markup, "E");

        await VerifyItemIsAbsentAsync(enumLikeE + markup, "E");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public async Task InExpressionBodiedProperty()
    {
        var markup =
            """
            class C
            {
                Colors Colors => $$
            }
            """;
        var colors = """
            enum Colors
            {
                Red,
                Blue,
                Green,
            }
            """;
        var colorsLike = """
            readonly struct Colors
            {
                public static readonly Colors Red;
                public static readonly Colors Blue;
                public static readonly Colors Green;
            }
            """;

        await VerifyItemExistsAsync(markup + colors, "Colors");
        await VerifyItemIsAbsentAsync(markup + colorsLike, "Colors");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4310")]
    public async Task InExpressionBodiedMethod()
    {
        var markup =
            """
            class C
            {
                Colors GetColors() => $$
            }
            """;
        var colors = """
            enum Colors
            {
                Red,
                Blue,
                Green,
            }
            """;
        var colorsLike = """
            readonly struct Colors
            {
                public static readonly Colors Red;
                public static readonly Colors Blue;
                public static readonly Colors Green;
            }
            """;

        await VerifyItemExistsAsync(markup + colors, "Colors");
        await VerifyItemIsAbsentAsync(markup + colorsLike, "Colors");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public Task NotAfterAsync1()
        => VerifyNoItemsExistAsync("""
            class Test
            {
                public async $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public Task NotAfterAsync2()
        => VerifyNoItemsExistAsync("""
            class Test
            {
                public async $$
                public void M() {}
            }
            """);

    [Fact]
    public async Task NotAfterDot()
    {
        var markup =
            """
            namespace ConsoleApplication253
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        M(E.$$)
                    }

                    static void M(E e) { }
                }
            }
            """;
        var enumE = """
            enum E
            {
                A,
                B,
            }
            """;
        var enumLikeE = """
            readonly struct E
            {
                public static readonly E A;
                public static readonly E B;
            }
            """;

        await VerifyNoItemsExistAsync(markup + enumE);
        await VerifyNoItemsExistAsync(markup + enumLikeE);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18359")]
    public async Task NotAfterDotWithTextTyped()
    {
        var markup =
            """
            namespace ConsoleApplication253
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        M(E.a$$)
                    }

                    static void M(E e) { }
                }
            }
            """;
        var enumE = """
            enum E
            {
                A,
                B,
            }
            """;
        var enumLikeE = """
            readonly struct E
            {
                public static readonly E A;
                public static readonly E B;
            }
            """;

        await VerifyNoItemsExistAsync(markup + enumE);
        await VerifyNoItemsExistAsync(markup + enumLikeE);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5419")]
    public Task TestInEnumInitializer1()
        => VerifyItemExistsAsync("""
            using System;

            [Flags]
            internal enum ProjectTreeWriterOptions
            {
                None,
                Tags,
                FilePath,
                Capabilities,
                Visibility,
                AllProperties = FilePath | Visibility | $$
            }
            """, "ProjectTreeWriterOptions");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5419")]
    public Task TestInEnumInitializer2()
        => VerifyItemExistsAsync("""
            using System;

            [Flags]
            internal enum ProjectTreeWriterOptions
            {
                None,
                Tags,
                FilePath,
                Capabilities,
                Visibility,
                AllProperties = FilePath | $$ Visibility
            }
            """, "ProjectTreeWriterOptions");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5419")]
    public Task TestInEnumInitializer3()
        => VerifyItemExistsAsync("""
            using System;

            [Flags]
            internal enum ProjectTreeWriterOptions
            {
                None,
                Tags,
                FilePath,
                Capabilities,
                Visibility,
                AllProperties = FilePath | $$ | Visibility
            }
            """, "ProjectTreeWriterOptions");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5419")]
    public Task TestInEnumInitializer4()
        => VerifyItemExistsAsync("""
            using System;

            [Flags]
            internal enum ProjectTreeWriterOptions
            {
                None,
                Tags,
                FilePath,
                Capabilities,
                Visibility,
                AllProperties = FilePath ^ $$
            }
            """, "ProjectTreeWriterOptions");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5419")]
    public Task TestInEnumInitializer5()
        => VerifyItemExistsAsync("""
            using System;

            [Flags]
            internal enum ProjectTreeWriterOptions
            {
                None,
                Tags,
                FilePath,
                Capabilities,
                Visibility,
                AllProperties = FilePath & $$
            }
            """, "ProjectTreeWriterOptions");

    [Fact]
    public Task TestInEnumHasFlag()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    FileInfo f;
                    f.Attributes.HasFlag($$
                }
            }
            """, "FileAttributes");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39240")]
    public Task TestInSwitchExpression1()
        => VerifyItemIsAbsentAsync("""
            using System;

            class C
            {
                void M(ConsoleColor color)
                {
                    var number = color switch $$
                }
            }
            """, "ConsoleColor");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39240")]
    public Task TestInSwitchExpression2()
        => VerifyItemExistsAsync("""
            using System;

            class C
            {
                void M(ConsoleColor color)
                {
                    var number = color switch { $$
                }
            }
            """, "ConsoleColor");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39240")]
    public Task TestInSwitchExpression3()
        => VerifyItemIsAbsentAsync("""
            using System;

            class C
            {
                void M(ConsoleColor color)
                {
                    var number = color switch { ConsoleColor.Black $$
                }
            }
            """, "ConsoleColor");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39240")]
    public Task TestInSwitchExpression4()
        => VerifyItemIsAbsentAsync("""
            using System;

            class C
            {
                void M(ConsoleColor color)
                {
                    var number = color switch { ConsoleColor.Black => $$
                }
            }
            """, "ConsoleColor");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39240")]
    public Task TestInSwitchExpression5()
        => VerifyItemIsAbsentAsync("""
            using System;

            class C
            {
                void M(ConsoleColor color)
                {
                    var number = color switch { ConsoleColor.Black => 0 $$
                }
            }
            """, "ConsoleColor");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39240")]
    public Task TestInSwitchExpression6()
        => VerifyItemExistsAsync("""
            using System;

            class C
            {
                void M(ConsoleColor color)
                {
                    var number = color switch { ConsoleColor.Black => 0, $$
                }
            }
            """, "ConsoleColor");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39240")]
    public Task TestInSwitchExpression7()
        => VerifyItemIsAbsentAsync("""
            using System;

            class C
            {
                void M(ConsoleColor color)
                {
                    var number = color switch { ConsoleColor.Black => 0 } $$
                }
            }
            """, "ConsoleColor");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68240")]
    public Task TestNotCompilerGeneratedField()
        => VerifyItemIsAbsentAsync("""
            class Sample
            {
                public static Sample Instance { get; } = new Sample();

                private sealed class Nested
                {
                    Sample A()
                    {
                        return $$
                    }
                }
            }
            """, "Sample.<Instance>k__BackingField");

    #region enum members

    [Fact]
    public async Task TestEditorBrowsable_EnumTypeDotMemberAlways()
    {
        var markup = """
            class P
            {
                public void S()
                {
                    MyEnum d = $$;
                }
            }
            """;
        var referencedCode = """
            public enum MyEnum
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                Member
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: """
            public readonly struct MyEnum
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public static readonly MyEnum Member;
            }
            """,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact]
    public async Task TestEditorBrowsable_EnumTypeDotMemberNever()
    {
        var markup = """
            class P
            {
                public void S()
                {
                    MyEnum d = $$;
                }
            }
            """;
        var referencedCode = """
            public enum MyEnum
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                Member
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: """
            public readonly struct MyEnum
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public static readonly MyEnum Member;
            }
            """,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact]
    public async Task TestEditorBrowsable_EnumTypeDotMemberAdvanced()
    {
        var markup = """
            class P
            {
                public void S()
                {
                    MyEnum d = $$;
                }
            }
            """;
        var referencedCode = """
            public enum MyEnum
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
                Member
            }
            """;
        var referencedCode_EnumLike = """
            public readonly struct MyEnum
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
                public static readonly MyEnum Member;
            }
            """;

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode_EnumLike,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode_EnumLike,
            item: "MyEnum.Member",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact]
    public async Task TestTriggeredOnOpenParen()
    {
        var markup = """
            static class Program
            {
                public static void Main(string[] args)
                {
                    // type after this line
                    Bar($$
                }

                public static void Bar(Goo f)
                {
                }
            }
            """;
        var goo = """
            enum Goo
            {
                AMember,
                BMember,
                CMember
            }
            """;
        var gooLike = """
            readonly struct Goo
            {
                public static readonly Goo AMember;
                public static readonly Goo BMember;
                public static readonly Goo CMember;
            }
            """;

        await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: true);
        await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: false);
        await VerifyItemIsAbsentAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: true);
        await VerifyItemExistsAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: false);
    }

    [Fact]
    public async Task TestRightSideOfAssignment()
    {
        var markup = """
            static class Program
            {
                public static void Main(string[] args)
                {
                    Goo x;
                    x = $$;
                }
            }
            """;
        var goo = """
            enum Goo
            {
                AMember,
                BMember,
                CMember
            }
            """;
        var gooLike = """
            readonly struct Goo
            {
                public static readonly Goo AMember;
                public static readonly Goo BMember;
                public static readonly Goo CMember;
            }
            """;

        await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: true);
        await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: false);
        await VerifyItemIsAbsentAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: true);
        await VerifyItemExistsAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: false);
    }

    [Fact]
    public async Task TestCaseStatement()
    {
        var markup = """
            static class Module1
            {
                public static void Main(string[] args)
                {
                    var value = E.A;

                    switch (value)
                    {
                        case $$
                    }
                }
            }
            """;
        var e = """
            enum E
            {
                A,
                B,
                C
            }
            """;
        var eLike = """
            readonly struct E
            {
                public static readonly E A;
                public static readonly E B;
                public static readonly E C;
            }
            """;

        await VerifyItemExistsAsync(e + markup, "E.A", usePreviousCharAsTrigger: true);
        await VerifyItemExistsAsync(e + markup, "E.A", usePreviousCharAsTrigger: false);
        await VerifyItemIsAbsentAsync(eLike + markup, "E.A", usePreviousCharAsTrigger: true);
        await VerifyItemExistsAsync(eLike + markup, "E.A", usePreviousCharAsTrigger: false);
    }

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    public Task TestInYieldReturn(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            using System;
            using System.Collections.Generic;

            class C
            {
                public IEnumerable<{{typeName}}> M()
                {
                    yield return $$;
                }
            }
            """, $"{typeName}.{memberName}");

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    public Task TestInAsyncMethodReturnStatement(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                public async Task<{{typeName}}> M()
                {
                    await Task.Delay(1);
                    return $$;
                }
            }
            """, $"{typeName}.{memberName}");

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    public Task TestInIndexedProperty(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            using System;
            static class Module1
            {
                public class MyClass1
                {
                    public bool this[{{typeName}} index]
                    {
                        set
                        {
                        }
                    }
                }

                public static void Main()
                {
                    var c = new MyClass1();
                    c[$${{typeName}}.{{memberName}}] = true;
                }
            }
            """, $"{typeName}.{memberName}");

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    public Task TestFullyQualified(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            class C
            {
                public void M(System.{{typeName}} day)
                {
                    M($$);
                }

                enum DayOfWeek
                {
                    A,
                    B
                }

                struct DateTime
                {
                    public static readonly DateTime A;
                    public static readonly DateTime B;
                }
            }
            """, $"System.{typeName}.{memberName}");

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public async Task TestTriggeredForNamedArgument(string typeName)
    {
        var markup = $$"""
            class C
            {
                public void M({{typeName}} day)
                {
                    M(day: $$);
                }

                enum DayOfWeek
                {
                    A,
                    B
                }

                struct DateTime
                {
                    public static readonly DateTime A;
                    public static readonly DateTime B;
                }
            }
            """;

        if (typeName == nameof(DayOfWeek))
            await VerifyItemExistsAsync(markup, $"{typeName}.A", usePreviousCharAsTrigger: true);
        else
            await VerifyItemIsAbsentAsync(markup, $"{typeName}.A", usePreviousCharAsTrigger: true);

        await VerifyItemExistsAsync(markup, $"{typeName}.A", usePreviousCharAsTrigger: false);
    }

    [Theory]
    [InlineData(nameof(DayOfWeek))]
    [InlineData(nameof(DateTime))]
    public Task TestNotTriggeredAfterAssignmentEquals(string typeName)
        => VerifyItemIsAbsentAsync($$"""
            class C
            {
                public void M({{typeName}} day)
                {
                    var x = $$;
                }

                enum DayOfWeek
                {
                    A,
                    B
                }

                struct DateTime
                {
                    public static readonly DateTime A;
                    public static readonly DateTime B;
                }
            }
            """, $"{typeName}.A", usePreviousCharAsTrigger: true);

    [Fact]
    public async Task TestCaseStatementWithInt32InferredType()
    {
        var markup = """
            class C
            {
                public void M(DayOfWeek day)
                {
                    switch (day)
                    {
                        case DayOfWeek.A:
                            break;

                        case $$
                    }
                }

                enum DayOfWeek
                {
                    A,
                    B
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "DayOfWeek.A");
        await VerifyItemExistsAsync(markup, "DayOfWeek.B");
    }

    [Fact]
    public async Task TestLocalNoAs()
    {
        var markup = """
            enum E
            {
                A
            }

            class C
            {
                public void M()
                {
                    const E e = e$$;
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "E.A");
        await VerifyItemIsAbsentAsync(markup, "e as E");
    }

    [Fact]
    public Task TestIncludeEnumAfterTyping()
        => VerifyItemExistsAsync("""
            enum E
            {
                A
            }

            class C
            {
                public void M()
                {
                    const E e = e$$;
                }
            }
            """, "E.A");

    [Fact]
    public Task TestNotInTrivia()
        => VerifyNoItemsExistAsync("""
            class C
            {
                public void M(DayOfWeek day)
                {
                    switch (day)
                    {
                        case DayOfWeek.A:
                        case DayOfWeek.B// $$
                            :
                                break;
                    }
                }

                enum DayOfWeek
                {
                    A,
                    B
                }
            }
            """);

    [Fact]
    public Task TestCommitOnComma()
        => VerifyProviderCommitAsync("""
            enum E
            {
                A
            }

            class C
            {
                public void M()
                {
                    const E e = $$
                }
            }
            """, "E.A", """
            enum E
            {
                A
            }

            class C
            {
                public void M()
                {
                    const E e = E.A;
                }
            }
            """, ';');

    [Theory]
    [InlineData(nameof(ConsoleKey))]
    [InlineData(nameof(DateTime))]
    public Task EnumMember_NotAfterDot(string typeName)
        => VerifyNoItemsExistAsync($$"""
            static class Module1
            {
                public static void Main({{typeName}} x)
                {
                    while (x == System.{{typeName}}.$$
                    {
                    }
                }
            }
            """);

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Monday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    public Task TestInCollectionInitializer1(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            using System;
            using System.Collections.Generic;

            class C
            {
                public void Main()
                {
                    var y = new List<{{typeName}}>()
                    {
                        $$
                    };
                }
            }
            """, $"{typeName}.{memberName}");

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Monday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    public Task TestInCollectionInitializer2(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            using System;
            using System.Collections.Generic;

            class C
            {
                public void Main()
                {
                    var y = new List<{{typeName}}>()
                    {
                        {{typeName}}.{{memberName}},
                        $$
                    };
                }
            }
            """, $"{typeName}.{memberName}");

    [Fact]
    public Task EnumMember_TestInEnumHasFlag()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                public void Main()
                {
                    FileInfo f;
                    f.Attributes.HasFlag($$
                }
            }
            """, "FileAttributes.Hidden");

    [Fact]
    public async Task TestMultipleEnumsCausedByOverloads()
    {
        var markup = """
            class C
            {
                public enum Color
                {
                    Red,
                    Green,
                }

                public enum Palette
                {
                    AccentColor1,
                    AccentColor2,
                }

                public readonly struct ColorLike
                {
                    public static readonly ColorLike Red;
                    public static readonly ColorLike Green;
                }

                public readonly struct PaletteLike
                {
                    public static readonly PaletteLike AccentColor1;
                    public static readonly PaletteLike AccentColor2;
                }

                public void SetColor(Color color) { }
                public void SetColor(Palette palette) { }
                public void SetColor(ColorLike color) { }
                public void SetColor(PaletteLike palette) { }

                public void Main()
                {
                    SetColor($$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "Color.Red");
        await VerifyItemExistsAsync(markup, "Palette.AccentColor1");

        await VerifyItemExistsAsync(markup, "ColorLike.Red");
        await VerifyItemExistsAsync(markup, "PaletteLike.AccentColor1");
    }

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    [InlineData(nameof(TimeZoneInfo), nameof(TimeZoneInfo.Local))]
    public Task TestNullableEnum(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            #nullable enable
            using System;
            class C
            {
                public void SetValue({{typeName}}? value) { }

                public void Main()
                {
                    SetValue($$
                }
            }
            """, $"{typeName}.{memberName}");

    [Theory]
    [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
    [InlineData(nameof(DateTime), nameof(DateTime.Now))]
    [InlineData(nameof(TimeZoneInfo), nameof(TimeZoneInfo.Local))]
    public Task TestTypeAlias(string typeName, string memberName)
        => VerifyItemExistsAsync($$"""
            #nullable enable
            using AT = System.{{typeName}};

            public class Program
            {
                static void M(AT attributeTargets) { }

                public static void Main()
                {
                    M($$
                }
            }
            """, $"AT.{memberName}");

    [Theory]
    [InlineData("")]
    [InlineData("Re")]
    [InlineData("Col")]
    [InlineData("Color.Green or", false)]
    [InlineData("Color.Green or ")]
    [InlineData("(Color.Green or ")] // start of: is (Color.Red or Color.Green) and not Color.Blue
    [InlineData("Color.Green or Re")]
    [InlineData("Color.Green or Color.Red or ")]
    [InlineData("Color.Green orWrittenWrong ", false)]
    [InlineData("not ")]
    [InlineData("not Re")]
    public async Task TestPatterns_Is_ConstUnaryAndBinaryPattern(string isPattern, bool shouldOfferRed = true)
    {
        var markup = $$"""
            class C
            {
                public enum Color
                {
                    Red,
                    Green,
                }

                public void M(Color c)
                {
                    var isRed = c is {{isPattern}}$$;
                }
            }
            """;
        if (shouldOfferRed)
        {
            await VerifyItemExistsAsync(markup, "Color.Red");
        }
        else
        {
            await VerifyItemIsAbsentAsync(markup, "Color.Red");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("Col")]
    [InlineData("Red")]
    [InlineData("Color.Green or ")]
    [InlineData("Color.Green or Re")]
    [InlineData("not ")]
    [InlineData("not Re")]
    public Task TestPatterns_Is_PropertyPattern(string partialWritten)
        => VerifyItemExistsAsync($$"""
            public enum Color
            {
                Red,
                Green,
            }

            class C
            {
                public Color Color { get; }

                public void M()
                {
                    var isRed = this is { Color: {{partialWritten}}$$
                }
            }
            """, "Color.Red");

    [Fact]
    public Task TestPatterns_Is_PropertyPattern_NotAfterEnumDot()
        => VerifyItemIsAbsentAsync($$"""
            public enum Color
            {
                Red,
                Green,
            }

            class C
            {
                public Color Color { get; }

                public void M()
                {
                    var isRed = this is { Color: Color.R$$
                }
            }
            """, "Color.Red");

    [Fact]
    public Task TestPatterns_SwitchStatement_PropertyPattern()
        => VerifyItemExistsAsync("""
            public enum Color
            {
                Red,
                Green,
            }

            class C
            {
                public Color Color { get; }

                public void M()
                {
                    switch (this)
                    {
                        case { Color: $$
                }
            }
            """, "Color.Red");

    [Fact]
    public Task TestPatterns_SwitchExpression_PropertyPattern()
        => VerifyItemExistsAsync("""
            public enum Color
            {
                Red,
                Green,
            }

            class C
            {
                public Color Color { get; }

                public void M()
                {
                    var isRed = this switch
                    {
                        { Color: $$
                }
            }
            """, "Color.Red");

    [Fact]
    public async Task TestStaticAndInstanceMembers()
    {
        var markup = """
            public readonly struct Color
            {
                public static readonly Color Red;
                public readonly Color Green;
            }

            class C
            {
                public void M(Color color)
                {
                    M($$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "Color.Red");
        await VerifyItemIsAbsentAsync(markup, "Color.Green");
    }

    [Fact]
    public async Task TestProperties()
    {
        var markup = """
            public readonly struct Color
            {
                public static Color Red { get; }
                public Color Green { get; }
            }

            class C
            {
                public void M(Color color)
                {
                    M($$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "Color.Red");
        await VerifyItemIsAbsentAsync(markup, "Color.Green");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("private")]
    public async Task TestAccessibilityDifferentType(string modifier)
    {
        var markup = $$"""
            public class Color
            {
                {{modifier}} static readonly Color Red;
            }

            class C
            {
                public void M(Color color)
                {
                    M($$
                }
            }
            """;

        var expected = modifier switch
        {
            "public" => true,
            "internal" => true,
            "protected internal" => true,
            _ => false,
        };

        if (expected)
            await VerifyItemExistsAsync(markup, "Color.Red");
        else
            await VerifyItemIsAbsentAsync(markup, "Color.Red");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("private")]
    public Task TestAccessibilitySameType(string modifier)
        => VerifyItemExistsAsync($$"""
            public class Color
            {
                {{modifier}} static readonly Color Red;

                public void M(Color color)
                {
                    M($$
                }
            }
            """, "Color.Red");

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record")]
    public Task TestEnumLikeTypeKinds(string typeKeyword)
        => VerifyItemExistsAsync($$"""
            public {{typeKeyword}} Color
            {
                public static readonly Color Red;
            }

            class C
            {
                public void M(Color color)
                {
                    M($$
                }
            }
            """, "Color.Red");

    #endregion
}
