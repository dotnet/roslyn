// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NothingToInitialize()
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

            VerifyNoItemsExist(markup);
            VerifyExclusive(markup, true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OneItem1()
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

            VerifyItemExists(markup, "value");
            VerifyItemIsAbsent(markup, "<value>k__BackingField");
            VerifyExclusive(markup, true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowWithEqualsSign()
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

            VerifyItemExists(markup, "value");
            VerifyItemIsAbsent(markup, "<value>k__BackingField");
            VerifyExclusive(markup, true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OneItem2()
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

            VerifyItemExists(markup, "value");
            VerifyItemIsAbsent(markup, "<value>k__BackingField");
            VerifyExclusive(markup, true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldAndProperty()
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

            VerifyItemExists(markup, "value");
            VerifyItemExists(markup, "otherValue");
            VerifyExclusive(markup, true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void HidePreviouslyTyped()
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

            VerifyItemIsAbsent(markup, "value");
            VerifyExclusive(markup, true);
            VerifyItemExists(markup, "otherValue");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInEqualsValue()
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

            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NothingLeftToShow()
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

            VerifyNoItemsExist(markup);
            VerifyExclusive(markup, true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedObjectInitializers()
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
            VerifyItemIsAbsent(markup, "myValue");
            VerifyItemExists(markup, "value");
            VerifyItemExists(markup, "otherValue");
            VerifyExclusive(markup, true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotExclusive1()
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
            VerifyExclusive(markup, false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotExclusive2()
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
            VerifyExclusive(markup, false);
        }

        [WorkItem(544242)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInArgumentList()
        {
            var markup = @"class C
{
    void M(int i, int j)
    {
        M(i, j$$
    }
}
";
            VerifyNoItemsExist(markup);
        }

        [WorkItem(530075)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInArgumentList2()
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
            VerifyNoItemsExist(markup);
        }

        [WorkItem(544289)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DerivedMembers()
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
            VerifyItemExists(markup, "FooBase");
            VerifyItemExists(markup, "FooDerived");
            VerifyItemExists(markup, "BasePublic");
            VerifyItemIsAbsent(markup, "BasePrivate");
        }

        [WorkItem(544242)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInCollectionInitializer()
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
            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InitializeDerivedType()
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
            VerifyItemExists(markup, "foo");
        }

        [WorkItem(544550)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ReadOnlyPropertiesShouldNotBePresent()
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

            VerifyItemExists(markup, "Capacity");
            VerifyItemIsAbsent(markup, "Count");
        }

        [WorkItem(544550)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IndexersShouldNotBePresent()
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

            VerifyItemExists(markup, "Capacity");
            VerifyItemIsAbsent(markup, "this[]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ReadOnlyPropertiesThatFollowTheCollectionPatternShouldBePresent()
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

            VerifyItemIsAbsent(markup, "foo");
            VerifyItemExists(markup, "bar");
        }

        [WorkItem(544607)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DoNotIncludeStaticMember()
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

            VerifyItemIsAbsent(markup, "Gibberish");
        }

        [WpfFact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_PropertyInObjectCreationAlways()
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

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_PropertyInObjectCreationNever()
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
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_PropertyInObjectCreationAdvanced()
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
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Prop",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestCommitCharacter()
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

            VerifyCommonCommitCharacters(markup, textTypedSoFar: "v");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestEnter()
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

            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(markup))
            {
                var hostDocument = workspace.Documents.Single();
                var position = hostDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var triggerInfo = CompletionTriggerInfo.CreateTypeCharTriggerInfo('a');

                var completionList = GetCompletionList(document, position, triggerInfo);
                var item = completionList.Items.First();

                var completionService = document.Project.LanguageServices.GetService<ICompletionService>();
                var completionRules = completionService.GetCompletionRules();

                Assert.False(completionRules.SendEnterThroughToEditor(item, string.Empty, workspace.Options), "Expected false from SendEnterThroughToEditor()");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestTrigger()
        {
            TestCommonIsTextualTriggerCharacter();
        }

        [WorkItem(530828)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DoNotIncludeIndexedPropertyWithNonOptionalParameter()
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
            VerifyItemInEditorBrowsableContexts(
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
        public void CollectionInitializerPatternFromBaseType()
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

            VerifyItemExists(markup, "S");
            VerifyItemExists(markup, "D");
        }

        [WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CollectionInitializerPatternFromBaseTypeInaccessible()
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

            VerifyItemIsAbsent(markup, "S");
            VerifyItemIsAbsent(markup, "D");
        }

        [WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CollectionInitializerPatternFromBaseTypeAccessible()
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

            VerifyItemExists(markup, "S");
            VerifyItemExists(markup, "D");
        }

        private void VerifyExclusive(string markup, bool exclusive)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(markup))
            {
                var hostDocument = workspace.Documents.Single();
                var position = hostDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var triggerInfo = CompletionTriggerInfo.CreateTypeCharTriggerInfo('a');

                var completionList = GetCompletionList(document, position, triggerInfo);

                if (completionList != null)
                {
                    Assert.True(exclusive == completionList.IsExclusive, "group.IsExclusive == " + completionList.IsExclusive);
                }
            }
        }
    }
}
