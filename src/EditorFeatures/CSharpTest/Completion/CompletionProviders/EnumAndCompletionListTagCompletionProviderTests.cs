// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NullableEnum()
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
            VerifyItemExists(markup, "Colors");
        }

        [WpfFact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_EnumMemberAlways()
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
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_EnumMemberNever()
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
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_EnumMemberAdvanced()
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
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(8540099)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInComment()
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
            VerifyNoItemsExist(markup);
        }

        [WorkItem(827897)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InYieldReturn()
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
            VerifyItemExists(markup, "DayOfWeek");
        }

        [WorkItem(827897)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InAsyncMethodReturnStatement()
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
            VerifyItemExists(markup, "DayOfWeek");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoCompletionListTag()
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
            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionList()
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
            VerifyItemExists(markup, "C");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionListCrefToString()
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
            VerifyItemExists(markup, "string", glyph: (int)Glyph.ClassPublic);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionListEmptyCref()
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
            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionListInaccessibleType()
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
            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionListNotAType()
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
            VerifyNoItemsExist(markup);
        }

        [WorkItem(828196)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestAlias()
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
            VerifyItemExists(markup, "D");
        }

        [WorkItem(828196)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestAlias2()
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
            VerifyItemExists(markup, "D");
        }

        [WorkItem(828196)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestAlias3()
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
            VerifyItemExists(markup, "D");
        }

        [WorkItem(828196)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInParameterNameContext()
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
            VerifyItemIsAbsent(markup, "E");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InExpressionBodiedProperty()
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
            VerifyItemExists(markup, "Colors");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InExpressionBodiedMethod()
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
            VerifyItemExists(markup, "Colors");
        }
    }
}
