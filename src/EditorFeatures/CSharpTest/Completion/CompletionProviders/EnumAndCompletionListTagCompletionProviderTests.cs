// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class EnumAndCompletionListTagCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public EnumAndCompletionListTagCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new EnumAndCompletionListTagCompletionProvider();
        }

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
        Foo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public enum Foo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
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
        Foo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public enum Foo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
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
        Foo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public enum Foo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
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
        public async Task InYieldReturn()
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
    void Foo()
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
    void Foo()
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
    void Foo()
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
    void Foo()
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
    void Foo()
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
    void Foo()
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
    private void Foo(System.Globalization.DigitShapes shape)
    {
    }

    static void Main(string[] args)
    {
        Foo($$
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
    void foo(E first, E second) 
    {
        foo(first: E.a, $$
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
    }
}
