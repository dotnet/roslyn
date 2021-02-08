// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class EnumAndCompletionListTagCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(EnumAndCompletionListTagCompletionProvider);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NullableEnum()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
        Colors? d = $$
        Colors c = Colors.Blue;
    }
}
 
enum Colors
{
    Red,
    Blue,
    Green,
}
";
            await VerifyItemExistsAsync(markup, "Colors");
        }

        [Fact]
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_EnumMemberAlways()
        {
            var markup = @"
class Program
{
    public void M()
    {
        Goo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public enum Goo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_EnumMemberNever()
        {
            var markup = @"
class Program
{
    public void M()
    {
        Goo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public enum Goo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_EnumMemberAdvanced()
        {
            var markup = @"
class Program
{
    public void M()
    {
        Goo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public enum Goo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(854099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854099")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInComment()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
        Colors c = // $$
    }
}
 
enum Colors
{
    Red,
    Blue,
    Green,
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InYieldReturnInMethod()
        {
            var markup =
@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<DayOfWeek> M()
    {
        yield return $$
    }
}";
            await VerifyItemExistsAsync(markup, "DayOfWeek");
        }

        [WorkItem(30235, "https://github.com/dotnet/roslyn/issues/30235")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InYieldReturnInLocalFunction()
        {
            var markup =
@"using System;
using System.Collections.Generic;

class Program
{
    void M()
    {
        IEnumerable<DayOfWeek> F()
        {
            yield return $$
        }
    }
}";
            await VerifyItemExistsAsync(markup, "DayOfWeek");
        }

        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InAsyncMethodReturnStatement()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<DayOfWeek> M()
    {
        await Task.Delay(1);
        return $$
    }
}";
            await VerifyItemExistsAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InSimpleLambdaAfterArrow()
        {
            var markup =
@"using System;

class Program
{
    Func<bool, DayOfWeek> M()
    {
        return _ => $$
    }
}";
            await VerifyItemExistsAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InParenthesizedLambdaAfterArrow()
        {
            var markup =
@"using System;

class Program
{
    Func<DayOfWeek> M()
    {
        return () => $$
    }
}";
            await VerifyItemExistsAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInAnonymousMethodAfterParameterList()
        {
            var markup =
@"using System;

class Program
{
    Func<DayOfWeek> M()
    {
        return delegate () $$
    }
}";
            await VerifyItemIsAbsentAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInSimpleLambdaAfterAsync()
        {
            var markup =
@"using System;

class Program
{
    Func<bool, DayOfWeek> M()
    {
        return async $$ _ =>
    }
}";
            await VerifyItemIsAbsentAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInParenthesizedLambdaAfterAsync()
        {
            var markup =
@"using System;

class Program
{
    Func<DayOfWeek> M()
    {
        return async $$ () =>
    }
}";
            await VerifyItemIsAbsentAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInAnonymousMethodAfterAsync()
        {
            var markup =
@"using System;

class Program
{
    Func<DayOfWeek> M()
    {
        return async $$ delegate ()
    }
}";
            await VerifyItemIsAbsentAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInSimpleLambdaBlock()
        {
            var markup =
@"using System;

class Program
{
    Func<bool, DayOfWeek> M()
    {
        return _ => { $$ }
    }
}";
            await VerifyItemIsAbsentAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInParenthesizedLambdaBlock()
        {
            var markup =
@"using System;

class Program
{
    Func<DayOfWeek> M()
    {
        return () => { $$ }
    }
}";
            await VerifyItemIsAbsentAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInAnonymousMethodBlock()
        {
            var markup =
@"using System;

class Program
{
    Func<DayOfWeek> M()
    {
        return delegate () { $$ }
    }
}";
            await VerifyItemIsAbsentAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionTreeSimpleLambdaAfterArrow()
        {
            var markup =
@"using System;
using System.Linq.Expressions;

class Program
{
    Expression<Func<bool, DayOfWeek>> M()
    {
        return _ => $$
    }
}";
            await VerifyItemExistsAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionTreeParenthesizedLambdaAfterArrow()
        {
            var markup =
@"using System;
using System.Linq.Expressions;

class Program
{
    Expression<Func<DayOfWeek>> M()
    {
        return () => $$
    }
}";
            await VerifyItemExistsAsync(markup, "DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoCompletionListTag()
        {
            var markup =
@"using System;
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
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionList()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""C""/>
class C
{
    
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyItemExistsAsync(markup, "C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListCrefToString()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""string""/>
class C
{
    
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyItemExistsAsync(markup, "string", glyph: (int)Glyph.ClassPublic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListEmptyCref()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""""/>
class C
{
    
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListInaccessibleType()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""C.Inner""/>
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
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListNotAType()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""C.Z()""/>
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
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SuggestAlias()
        {
            var markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  $$
    }
}";
            await VerifyItemExistsAsync(markup, "D");
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        D d=  $$
    }
}
}
";
            await VerifyItemExistsAsync(markup, "D");
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SuggestAlias3()
        {
            var markup = @"
namespace N
{
using D = System.Globalization.DigitShapes; 

class Program
{
    private void Goo(System.Globalization.DigitShapes shape)
    {
    }

    static void Main(string[] args)
    {
        Goo($$
    }
}
}
";
            await VerifyItemExistsAsync(markup, "D");
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInParameterNameContext()
        {
            var markup = @"
enum E
{
    a
}

class C
{
    void goo(E first, E second) 
    {
        goo(first: E.a, $$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "E");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionBodiedProperty()
        {
            var markup =
@"class C
{
    Colors Colors => $$
}

enum Colors
{
    Red,
    Blue,
    Green,
}
";
            await VerifyItemExistsAsync(markup, "Colors");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionBodiedMethod()
        {
            var markup =
@"class C
{
    Colors GetColors() => $$
}

enum Colors
{
    Red,
    Blue,
    Green,
}
";
            await VerifyItemExistsAsync(markup, "Colors");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterDot()
        {
            var markup =
@"namespace ConsoleApplication253
{
    class Program
    {
        static void Main(string[] args)
        {
            M(E.$$)
        }

        static void M(E e) { }
    }

    enum E
    {
        A,
        B,
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(18359, "https://github.com/dotnet/roslyn/issues/18359")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterDotWithTextTyped()
        {
            var markup =
@"namespace ConsoleApplication253
{
    class Program
    {
        static void Main(string[] args)
        {
            M(E.a$$)
        }

        static void M(E e) { }
    }

    enum E
    {
        A,
        B,
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer1()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath | Visibility | $$
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer2()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath | $$ Visibility
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer3()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath | $$ | Visibility
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer4()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath ^ $$
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer5()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath & $$
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumHasFlag()
        {
            var markup =
@"using System.IO;

class C
{
    void M()
    {
        FileInfo f;
        f.Attributes.HasFlag($$
    }
}";
            await VerifyItemExistsAsync(markup, "FileAttributes");
        }

        #region enum members

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEditorBrowsable_EnumTypeDotMemberAlways()
        {
            var markup = @"
class P
{
    public void S()
    {
        MyEnum d = $$;
    }
}";
            var referencedCode = @"
public enum MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    Member
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEditorBrowsable_EnumTypeDotMemberNever()
        {
            var markup = @"
class P
{
    public void S()
    {
        MyEnum d = $$;
    }
}";
            var referencedCode = @"
public enum MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEditorBrowsable_EnumTypeDotMemberAdvanced()
        {
            var markup = @"
class P
{
    public void S()
    {
        MyEnum d = $$;
    }
}";
            var referencedCode = @"
public enum MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTriggeredOnOpenParen()
        {
            var markup = @"
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

enum Goo
{
    AMember,
    BMember,
    CMember
}
";
            await VerifyItemExistsAsync(markup, "Goo.AMember", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRightSideOfAssignment()
        {
            var markup = @"
static class Program
{
    public static void Main(string[] args)
    {
        Goo x;
        x = $$;
    }
}

enum Goo
{
    AMember,
    BMember,
    CMember
}
";

            await VerifyItemExistsAsync(markup, "Goo.AMember", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCaseStatement()
        {
            var markup = @"
enum E
{
    A,
    B,
    C
}

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
";

            await VerifyItemExistsAsync(markup, "E.A", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInYieldReturn()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<DayOfWeek> M()
    {
        yield return $$;
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Friday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInAsyncMethodReturnStatement()
        {
            var markup = @"
using System;
using System.Threading.Tasks;

class C
{
    public async Task<DayOfWeek> M()
    {
        await Task.Delay(1);
        return $$;
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Friday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInIndexedProperty()
        {
            var markup = @"
static class Module1
{
    public enum MyEnum
    {
        flower
    }

    public class MyClass1
    {
        public bool this[MyEnum index]
        {
            set
            {
            }
        }
    }

    public static void Main()
    {
        var c = new MyClass1();
        c[$$MyEnum.flower] = true;
    }
}
";

            await VerifyItemExistsAsync(markup, "MyEnum.flower");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestFullyQualified()
        {
            var markup = @"
class C
{
    public void M(System.DayOfWeek day)
    {
        M($$);
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";

            await VerifyItemExistsAsync(markup, "System.DayOfWeek.Friday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTriggeredForNamedArgument()
        {
            var markup = @"
class C
{
    public void M(DayOfWeek day)
    {
        M(day: $$);
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.A", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestNotTriggeredAfterAssignmentEquals()
        {
            var markup = @"
class C
{
    public void M(DayOfWeek day)
    {
        var x = $$;
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "DayOfWeek.A", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCaseStatementWithInt32InferredType()
        {
            var markup = @"
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
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.A");
            await VerifyItemExistsAsync(markup, "DayOfWeek.B");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestLocalNoAs()
        {
            var markup = @"
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
";

            await VerifyItemExistsAsync(markup, "E.A");
            await VerifyItemIsAbsentAsync(markup, "e as E");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestIncludeEnumAfterTyping()
        {
            var markup = @"
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
";

            await VerifyItemExistsAsync(markup, "E.A");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestNotInTrivia()
        {
            var markup = @"
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
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCommitOnComma()
        {
            var markup = @"
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
";

            var expected = @"
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
";

            await VerifyProviderCommitAsync(markup, "E.A", expected, ';');
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EnumMember_NotAfterDot()
        {
            var markup = @"
static class Module1
{
    public static void Main()
    {
        while (System.Console.ReadKey().Key == System.ConsoleKey.$$
        {
        }
    }
}
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInCollectionInitializer1()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class C
{
    public void Main()
    {
        var y = new List<DayOfWeek>()
        {
            $$
        };
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Monday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInCollectionInitializer2()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class C
{
    public void Main()
    {
        var y = new List<DayOfWeek>()
        {
            DayOfWeek.Monday,
            $$
        };
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Monday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EnumMember_TestInEnumHasFlag()
        {
            var markup = @"
using System.IO;

class C
{
    public void Main()
    {
        FileInfo f;
        f.Attributes.HasFlag($$
    }
}
";

            await VerifyItemExistsAsync(markup, "FileAttributes.Hidden");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMultipleEnumsCausedByOverloads()
        {
            var markup = @"
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

    public void SetColor(Color color) { }
    public void SetColor(Palette palette) { }

    public void Main()
    {
        SetColor($$
    }
}
";
            await VerifyItemExistsAsync(markup, "Color.Red");
            await VerifyItemExistsAsync(markup, "Palette.AccentColor1");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestNullableEnum()
        {
            var markup = @"
class C
{
    public enum Color
    {
        Red,
        Green,
    }

    public void SetColor(Color? color) { }

    public void Main()
    {
        SetColor($$
    }
}
";
            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTypeAlias()
        {
            var markup = @"
using AT = System.AttributeTargets;

public class Program
{
    static void M(AT attributeTargets) { }
    
    public static void Main()
    {
        M($$
    }
}";
            await VerifyItemExistsAsync(markup, "AT.Assembly");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
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
            var markup = @$"
class C
{{
    public enum Color
    {{
        Red,
        Green,
    }}

    public void M(Color c)
    {{
        var isRed = c is {isPattern}$$;
    }}
}}
";
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
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("")]
        [InlineData("Col")]
        [InlineData("Red")]
        [InlineData("Color.Green or ")]
        [InlineData("Color.Green or Re")]
        [InlineData("not ")]
        [InlineData("not Re")]
        public async Task TestPatterns_Is_PropertyPattern(string partialWritten)
        {
            var markup = @$"
public enum Color
{{
    Red,
    Green,
}}

class C
{{
    public Color Color {{ get; }}

    public void M()
    {{
        var isRed = this is {{ Color: {partialWritten}$$
    }}
}}
";
            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestPatterns_Is_PropertyPattern_NotAfterEnumDot()
        {
            var markup = @$"
public enum Color
{{
    Red,
    Green,
}}

class C
{{
    public Color Color {{ get; }}

    public void M()
    {{
        var isRed = this is {{ Color: Color.R$$
    }}
}}
";
            await VerifyItemIsAbsentAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestPatterns_SwitchStatement_PropertyPattern()
        {
            var markup = @"
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
";
            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestPatterns_SwitchExpression_PropertyPattern()
        {
            var markup = @"
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
";
            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        #endregion
    }
}
