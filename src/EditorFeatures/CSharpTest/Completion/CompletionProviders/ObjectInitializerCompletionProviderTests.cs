// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ObjectInitializerCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ObjectInitializerCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new ObjectInitializerCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NothingToInitialize()
        {
            var markup = @"
class c { }

class d
{
    void foo()
    {
       c foo = new c { $$
    }
}";

            await VerifyNoItemsExistAsync(markup);
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OneItem1()
        {
            var markup = @"
class c { public int value {set; get; }}

class d
{
    void foo()
    {
       c foo = new c { v$$
    }
}";

            await VerifyItemExistsAsync(markup, "value");
            await VerifyItemIsAbsentAsync(markup, "<value>k__BackingField");
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowWithEqualsSign()
        {
            var markup = @"
class c { public int value {set; get; }}

class d
{
    void foo()
    {
       c foo = new c { v$$=
    }
}";

            await VerifyItemExistsAsync(markup, "value");
            await VerifyItemIsAbsentAsync(markup, "<value>k__BackingField");
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OneItem2()
        {
            var markup = @"
class c
{
    public int value {set; get; }

    void foo()
    {
       c foo = new c { v$$
    }
}";

            await VerifyItemExistsAsync(markup, "value");
            await VerifyItemIsAbsentAsync(markup, "<value>k__BackingField");
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldAndProperty()
        {
            var markup = @"
class c 
{ 
    public int value {set; get; }
    public int otherValue;
}

class d
{
    void foo()
    {
       c foo = new c { v$$
    }
}";

            await VerifyItemExistsAsync(markup, "value");
            await VerifyItemExistsAsync(markup, "otherValue");
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task HidePreviouslyTyped()
        {
            var markup = @"
class c 
{ 
    public int value {set; get; }
    public int otherValue;
}

class d
{
    void foo()
    {
       c foo = new c { value = 3, o$$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "value");
            await VerifyExclusiveAsync(markup, true);
            await VerifyItemExistsAsync(markup, "otherValue");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInEqualsValue()
        {
            var markup = @"
class c 
{ 
    public int value {set; get; }
    public int otherValue;
}

class d
{
    void foo()
    {
       c foo = new c { value = v$$
    }
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NothingLeftToShow()
        {
            var markup = @"
class c 
{ 
    public int value {set; get; }
    public int otherValue;
}

class d
{
    void foo()
    {
       c foo = new c { value = 3, otherValue = 4, $$
    }
}";

            await VerifyNoItemsExistAsync(markup);
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedObjectInitializers()
        {
            var markup = @"
class c 
{ 
    public int value {set; get; }
    public int otherValue;
}

class d
{
    public c myValue {set; get;}
}

class e
{
    void foo()
    {
       d bar = new d { myValue = new c { $$
    }
}";
            await VerifyItemIsAbsentAsync(markup, "myValue");
            await VerifyItemExistsAsync(markup, "value");
            await VerifyItemExistsAsync(markup, "otherValue");
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotExclusive1()
        {
            var markup = @"using System.Collections.Generic;
class c : IEnumerable<int>
{ 
    public void Add(int a) { }
    public int value {set; get; }
    public int otherValue;
}

class d
{
    void foo()
    {
       c bar = new c { v$$
    }
}";
            await VerifyExclusiveAsync(markup, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotExclusive2()
        {
            var markup = @"using System.Collections;
class c : IEnumerable
{ 
    public void Add(object a) { }
    public int value {set; get; }
    public int otherValue;
}

class d
{
    void foo()
    {
       c bar = new c { v$$
    }
}";
            await VerifyExclusiveAsync(markup, false);
        }

        [WorkItem(544242)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInArgumentList()
        {
            var markup = @"class C
{
    void M(int i, int j)
    {
        M(i, j$$
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(530075)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInArgumentList2()
        {
            var markup = @"class C
{
    public int A;
    void M(int i, int j)
    {
        new C(1, $$
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(544289)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DerivedMembers()
        {
            var markup = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
namespace ConsoleApplication1
{
    class Base
    {
        public int FooBase;
        private int BasePrivate { get; set; }
        public int BasePublic{ get; set; }
    }

    class Derived : Base
    {
        public int FooDerived;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var x = new Derived { F$$ 
        }
    }
}
";
            await VerifyItemExistsAsync(markup, "FooBase");
            await VerifyItemExistsAsync(markup, "FooDerived");
            await VerifyItemExistsAsync(markup, "BasePublic");
            await VerifyItemIsAbsentAsync(markup, "BasePrivate");
        }

        [WorkItem(544242)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInCollectionInitializer()
        {
            var markup = @"using System.Collections.Generic;
class C
{
    void foo()
    {
        var a = new List<int> {0, $$
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InitializeDerivedType()
        {
            var markup = @"using System.Collections.Generic;

class b {}
class d : b
{
    public int foo;
}

class C
{
    void stuff()
    {
        b a = new d { $$
    }
}
";
            await VerifyItemExistsAsync(markup, "foo");
        }

        [WorkItem(544550)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyPropertiesShouldNotBePresent()
        {
            var markup = @"using System.Collections.Generic;
class C
{
    void foo()
    {
        var a = new List<int> {$$
    }
}
";

            await VerifyItemExistsAsync(markup, "Capacity");
            await VerifyItemIsAbsentAsync(markup, "Count");
        }

        [WorkItem(544550)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IndexersShouldNotBePresent()
        {
            var markup = @"using System.Collections.Generic;
class C
{
    void foo()
    {
        var a = new List<int> {$$
    }
}
";

            await VerifyItemExistsAsync(markup, "Capacity");
            await VerifyItemIsAbsentAsync(markup, "this[]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyPropertiesThatFollowTheCollectionPatternShouldBePresent()
        {
            var markup = @"using System.Collections.Generic;
class C
{
    public readonly int foo;
    public readonly List<int> bar;

    void M()
    {
        new C() { $$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "foo");
            await VerifyItemExistsAsync(markup, "bar");
        }

        [WorkItem(544607)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotIncludeStaticMember()
        {
            var markup = @"
class Foo
{
    public static int Gibberish { get; set; }
}
 
class Bar
{
    void foo()
    {
        var c = new Foo { $$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "Gibberish");
        }

        [Fact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_PropertyInObjectCreationAlways()
        {
            var markup = @"
public class C
{
    public void M()
    {
        var x = new Foo { $$
    }
}";
            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public string Prop { get; set; }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_PropertyInObjectCreationNever()
        {
            var markup = @"
public class C
{
    public void M()
    {
        var x = new Foo { $$
    }
}";
            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public string Prop { get; set; }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_PropertyInObjectCreationAdvanced()
        {
            var markup = @"
public class C
{
    public void M()
    {
        var x = new Foo { $$
    }
}";
            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public string Prop { get; set; }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCommitCharacter()
        {
            const string markup = @"
class c { public int value {set; get; }}

class d
{
    void foo()
    {
       c foo = new c { v$$
    }
}";

            await VerifyCommonCommitCharactersAsync(markup, textTypedSoFar: "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEnter()
        {
            const string markup = @"
class c { public int value {set; get; }}

class d
{
    void foo()
    {
       c foo = new c { v$$
    }
}";

            using (var workspace = await TestWorkspaceFactory.CreateCSharpAsync(markup))
            {
                var hostDocument = workspace.Documents.Single();
                var position = hostDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var triggerInfo = CompletionTriggerInfo.CreateTypeCharTriggerInfo('a');

                var completionList = await GetCompletionListAsync(document, position, triggerInfo);
                var item = completionList.Items.First();

                var completionService = document.Project.LanguageServices.GetService<ICompletionService>();
                var completionRules = completionService.GetCompletionRules();

                Assert.False(completionRules.SendEnterThroughToEditor(item, string.Empty, workspace.Options), "Expected false from SendEnterThroughToEditor()");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTrigger()
        {
            await TestCommonIsTextualTriggerCharacterAsync();
        }

        [WorkItem(530828)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotIncludeIndexedPropertyWithNonOptionalParameter()
        {
            var markup = @"C c01 = new C() {$$ }";
            var referencedCode = @"Public Class C
    Public Property IndexProp(ByVal p1 As Integer) As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)
        End Set
    End Property
End Class";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "IndexProp",
                expectedSymbolsSameSolution: 0,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic,
                hideAdvancedMembers: false);
        }

        [WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CollectionInitializerPatternFromBaseType()
        {
            var markup = @"
using System;
using System.Collections;

public class SupportsAdd : IEnumerable
{
    public void Add(int x) { }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class SupportsAddDerived : SupportsAdd { }

class Container
{
    public SupportsAdd S { get; }
    public SupportsAddDerived D { get; }
}

class Program
{
    static void Main(string[] args)
    {
        var y = new Container { $$ };
    }
}";

            await VerifyItemExistsAsync(markup, "S");
            await VerifyItemExistsAsync(markup, "D");
        }

        [WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CollectionInitializerPatternFromBaseTypeInaccessible()
        {
            var markup = @"
using System;
using System.Collections;

public class SupportsAdd : IEnumerable
{
    protected void Add(int x) { }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class SupportsAddDerived : SupportsAdd { }

class Container
{
    public SupportsAdd S { get; }
    public SupportsAddDerived D { get; }
}

class Program
{
    static void Main(string[] args)
    {
        var y = new Container { $$ };
    }
}";

            await VerifyItemIsAbsentAsync(markup, "S");
            await VerifyItemIsAbsentAsync(markup, "D");
        }

        [WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CollectionInitializerPatternFromBaseTypeAccessible()
        {
            var markup = @"
using System;
using System.Collections;

public class SupportsAdd : IEnumerable
{
    protected void Add(int x) { }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class SupportsAddDerived : SupportsAdd 
{ 
class Container
{
    public SupportsAdd S { get; }
    public SupportsAddDerived D { get; }
}
    static void Main(string[] args)
    {
        var y = new Container { $$ };
    }
}";

            await VerifyItemExistsAsync(markup, "S");
            await VerifyItemExistsAsync(markup, "D");
        }

        private async Task VerifyExclusiveAsync(string markup, bool exclusive)
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpAsync(markup))
            {
                var hostDocument = workspace.Documents.Single();
                var position = hostDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var triggerInfo = CompletionTriggerInfo.CreateTypeCharTriggerInfo('a');

                var completionList = await GetCompletionListAsync(document, position, triggerInfo);

                if (completionList != null)
                {
                    Assert.True(exclusive == completionList.IsExclusive, "group.IsExclusive == " + completionList.IsExclusive);
                }
            }
        }
    }
}
