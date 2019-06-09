// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class EnumAndCompletionListTagCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public EnumAndCompletionListTagCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
            => new EnumAndCompletionListTagCompletionProvider();

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
    }
}
