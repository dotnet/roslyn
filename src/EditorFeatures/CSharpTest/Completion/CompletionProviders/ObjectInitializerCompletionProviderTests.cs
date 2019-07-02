// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ObjectInitializerCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ObjectInitializerCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new ObjectInitializerCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NothingToInitialize()
        {
            var markup = @"
class C { }

class D
{
    void goo()
    {
       C goo = new C { $$
    }
}";

            await VerifyNoItemsExistAsync(markup);
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OneItem1()
        {
            var markup = @"
class C { public int value {set; get; }}

class D
{
    void goo()
    {
       C goo = new C { v$$
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
class C { public int value {set; get; }}

class D
{
    void goo()
    {
       C goo = new C { v$$=
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
class C
{
    public int value {set; get; }

    void goo()
    {
       C goo = new C { v$$
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
class C 
{ 
    public int value {set; get; }
    public int otherValue;
}

class D
{
    void goo()
    {
       C goo = new C { v$$
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
class C 
{ 
    public int value {set; get; }
    public int otherValue;
}

class D
{
    void goo()
    {
       C goo = new C { value = 3, o$$
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
class C 
{ 
    public int value {set; get; }
    public int otherValue;
}

class D
{
    void goo()
    {
       C goo = new C { value = v$$
    }
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NothingLeftToShow()
        {
            var markup = @"
class C 
{ 
    public int value {set; get; }
    public int otherValue;
}

class D
{
    void goo()
    {
       C goo = new C { value = 3, otherValue = 4, $$
    }
}";

            await VerifyNoItemsExistAsync(markup);
            await VerifyExclusiveAsync(markup, true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedObjectInitializers()
        {
            var markup = @"
class C 
{ 
    public int value {set; get; }
    public int otherValue;
}

class D
{
    public C myValue {set; get;}
}

class E
{
    void goo()
    {
       D bar = new D { myValue = new C { $$
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
class C : IEnumerable<int>
{ 
    public void Add(int a) { }
    public int value {set; get; }
    public int otherValue;
}

class D
{
    void goo()
    {
       C bar = new C { v$$
    }
}";
            await VerifyExclusiveAsync(markup, false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotExclusive2()
        {
            var markup = @"using System.Collections;
class C : IEnumerable
{ 
    public void Add(object a) { }
    public int value {set; get; }
    public int otherValue;
}

class D
{
    void goo()
    {
       C bar = new C { v$$
    }
}";
            await VerifyExclusiveAsync(markup, false);
        }

        [WorkItem(544242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544242")]
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

        [WorkItem(530075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530075")]
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

        [WorkItem(544289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544289")]
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
        public int GooBase;
        private int BasePrivate { get; set; }
        public int BasePublic{ get; set; }
    }

    class Derived : Base
    {
        public int GooDerived;
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
            await VerifyItemExistsAsync(markup, "GooBase");
            await VerifyItemExistsAsync(markup, "GooDerived");
            await VerifyItemExistsAsync(markup, "BasePublic");
            await VerifyItemIsAbsentAsync(markup, "BasePrivate");
        }

        [WorkItem(544242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544242")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInCollectionInitializer()
        {
            var markup = @"using System.Collections.Generic;
class C
{
    void goo()
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

class B {}
class D : B
{
    public int goo;
}

class C
{
    void stuff()
    {
        B a = new D { $$
    }
}
";
            await VerifyItemExistsAsync(markup, "goo");
        }

        [WorkItem(544550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544550")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyPropertiesShouldNotBePresent()
        {
            var markup = @"using System.Collections.Generic;
class C
{
    void goo()
    {
        var a = new List<int> {$$
    }
}
";

            await VerifyItemExistsAsync(markup, "Capacity");
            await VerifyItemIsAbsentAsync(markup, "Count");
        }

        [WorkItem(544550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544550")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IndexersShouldNotBePresent()
        {
            var markup = @"using System.Collections.Generic;
class C
{
    void goo()
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
    public readonly int goo;
    public readonly List<int> bar;

    void M()
    {
        new C() { $$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "goo");
            await VerifyItemExistsAsync(markup, "bar");
        }

        [WorkItem(544607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544607")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotIncludeStaticMember()
        {
            var markup = @"
class Goo
{
    public static int Gibberish { get; set; }
}
 
class Bar
{
    void goo()
    {
        var c = new Goo { $$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "Gibberish");
        }

        [Fact]
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_PropertyInObjectCreationAlways()
        {
            var markup = @"
public class C
{
    public void M()
    {
        var x = new Goo { $$
    }
}";
            var referencedCode = @"
public class Goo
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
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_PropertyInObjectCreationNever()
        {
            var markup = @"
public class C
{
    public void M()
    {
        var x = new Goo { $$
    }
}";
            var referencedCode = @"
public class Goo
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
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_PropertyInObjectCreationAdvanced()
        {
            var markup = @"
public class C
{
    public void M()
    {
        var x = new Goo { $$
    }
}";
            var referencedCode = @"
public class Goo
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
class C { public int value {set; get; }}

class D
{
    void goo()
    {
       C goo = new C { v$$
    }
}";

            await VerifyCommonCommitCharactersAsync(markup, textTypedSoFar: "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEnter()
        {
            const string markup = @"
class C { public int value {set; get; }}

class D
{
    void goo()
    {
       C goo = new C { v$$
    }
}";

            using (var workspace = TestWorkspace.CreateCSharp(markup))
            {
                var hostDocument = workspace.Documents.Single();
                var position = hostDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var triggerInfo = CompletionTrigger.CreateInsertionTrigger('a');

                var service = GetCompletionService(workspace);
                var completionList = await GetCompletionListAsync(service, document, position, triggerInfo);
                var item = completionList.Items.First();

                Assert.False(CommitManager.SendEnterThroughToEditor(service.GetRules(), item, string.Empty), "Expected false from SendEnterThroughToEditor()");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestTrigger()
        {
            TestCommonIsTextualTriggerCharacter();
        }

        [WorkItem(530828, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530828")]
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

            // Can't use S={3}, but the object initializer syntax S={} is still valid
            await VerifyItemExistsAsync(markup, "S");
            await VerifyItemExistsAsync(markup, "D");
        }

        [WorkItem(13158, "https://github.com/dotnet/roslyn/issues/13158")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CollectionInitializerForInterfaceType1()
        {
            var markup = @"
using System.Collections.Generic;

public class Goo
{
    public IList<int> Items { get; } = new List<int>();
    public int Bar;
}

class Program
{
    static void Main(string[] args)
    {
        var y = new Goo { $$ };
    }
}";

            await VerifyItemExistsAsync(markup, "Items");
            await VerifyItemExistsAsync(markup, "Bar");
        }

        [WorkItem(13158, "https://github.com/dotnet/roslyn/issues/13158")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CollectionInitializerForInterfaceType2()
        {
            var markup = @"
using System.Collections.Generic;

public interface ICustomCollection<T> : ICollection<T> { }

public class Goo
{
    public ICustomCollection<int> Items { get; } = new List<int>();
    public int Bar;
}

class Program
{
    static void Main(string[] args)
    {
        var y = new Goo { $$ };
    }
}";

            await VerifyItemExistsAsync(markup, "Items");
            await VerifyItemExistsAsync(markup, "Bar");
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

        [WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectInitializerOfGenericTypeConstructedWithInaccessibleType()
        {
            var markup = @"
class Generic<T>
{
    public string Value { get; set; }
}

class Program
{
    private class InaccessibleToGeneric
    {

    }

    static void Main(string[] args)
    {
        var g = new Generic<InaccessibleToGeneric> { $$ }
    }
}";

            await VerifyItemExistsAsync(markup, "Value");
        }

        [WorkItem(24612, "https://github.com/dotnet/roslyn/issues/24612")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectInitializerOfGenericTypeСonstraint1()
        {
            var markup = @"
internal interface IExample
{
    string A { get; set; }
    string B { get; set; }
}

internal class Example
{
    public static T Create<T>()
        where T : IExample, new()
    {
        return new T
        {
            $$
        };
    }
}";

            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemExistsAsync(markup, "B");
        }

        [WorkItem(24612, "https://github.com/dotnet/roslyn/issues/24612")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectInitializerOfGenericTypeСonstraint2()
        {
            var markup = @"
internal class Example
{
    public static T Create<T>()
        where T : new()
    {
        return new T
        {
            $$
        };
    }
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(24612, "https://github.com/dotnet/roslyn/issues/24612")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectInitializerOfGenericTypeСonstraint3()
        {
            var markup = @"
internal class Example
{
    public static T Create<T>()
        where T : System.Delegate, new()
    {
        return new T
        {
            $$
        };
    }
}";

            await VerifyItemExistsAsync(markup, "Target");
            await VerifyItemExistsAsync(markup, "Method");
        }

        [WorkItem(24612, "https://github.com/dotnet/roslyn/issues/24612")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectInitializerOfGenericTypeСonstraint4()
        {
            var markup = @"
internal class Example
{
    public static T Create<T>()
        where T : unmanaged
    {
        return new T
        {
            $$
        };
    }
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(26560, "https://github.com/dotnet/roslyn/issues/26560")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectInitializerEscapeKeywords()
        {
            var markup = @"
class C
{
    public int @new { get; set; }

    public int @this { get; set; }

    public int now { get; set; }
}

class D
{
    static void Main(string[] args)
    {
        var t = new C() { $$ };
    }
}";

            await VerifyItemExistsAsync(markup, "@new");
            await VerifyItemExistsAsync(markup, "@this");
            await VerifyItemExistsAsync(markup, "now");

            await VerifyItemIsAbsentAsync(markup, "new");
            await VerifyItemIsAbsentAsync(markup, "this");
        }

        [WorkItem(15205, "https://github.com/dotnet/roslyn/issues/15205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedPropertyInitializers1()
        {
            var markup = @"
class A
{
    public B PropB { get; }
}

class B
{
    public int Prop { get; set; }
}


class Program
{
    static void Main(string[] args)
    {
        var a = new A { $$ }
    }
}";

            await VerifyItemExistsAsync(markup, "PropB");
        }

        [WorkItem(15205, "https://github.com/dotnet/roslyn/issues/15205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedPropertyInitializers2()
        {
            var markup = @"
class A
{
    public B PropB { get; }
}

class B
{
    public C PropC { get; }
}

class C
{
    public int P { get; set; }
}


class Program
{
    static void Main(string[] args)
    {
        var a = new A { $$ }
    }
}";

            await VerifyItemExistsAsync(markup, "PropB");
        }

        [WorkItem(15205, "https://github.com/dotnet/roslyn/issues/15205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedPropertyInitializers3()
        {
            var markup = @"
class A
{
    public B PropB { get; }
}

class B
{
    public C PropC { get; }
}

class C
{
    public SupportsAdd P { get; set; }
}

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

class Program
{
    static void Main(string[] args)
    {
        var a = new A { $$ }
    }
}";

            await VerifyItemExistsAsync(markup, "PropB");
        }

        [WorkItem(15205, "https://github.com/dotnet/roslyn/issues/15205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedPropertyInitializers4()
        {
            var markup = @"
class A
{
    public B PropB { get; }
}

class B
{
    public C PropC { get; }
}

class C
{
    public int P;
}


class Program
{
    static void Main(string[] args)
    {
        var a = new A { $$ }
    }
}";

            await VerifyItemExistsAsync(markup, "PropB");
        }

        [WorkItem(15205, "https://github.com/dotnet/roslyn/issues/15205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedPropertyInitializers5()
        {
            var markup = @"
class A
{
    public B PropB { get; }
}

class B
{
    public C PropC { get; }
}

class C
{
    public int P { get; }
}

class Program
{
    static void Main(string[] args)
    {
        var a = new A { $$ }
    }
}";

            await VerifyItemExistsAsync(markup, "PropB");
        }

        [WorkItem(36702, "https://github.com/dotnet/roslyn/issues/36702")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedPropertyInitializers6()
        {
            var markup = @"
class A
{
    public B PropB { get; }
}

class B
{
    public C PropC { get; }
}

class C
{
    public int P { get; }
}

class Program
{
    static void Main(string[] args)
    {
        var a = new A { PropB = { $$ } }
    }
}";

            await VerifyItemExistsAsync(markup, "PropC");
        }

        private async Task VerifyExclusiveAsync(string markup, bool exclusive)
        {
            using (var workspace = TestWorkspace.CreateCSharp(markup))
            {
                var hostDocument = workspace.Documents.Single();
                var position = hostDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var triggerInfo = CompletionTrigger.CreateInsertionTrigger('a');

                var service = GetCompletionService(workspace);
                var completionList = await GetCompletionListAsync(service, document, position, triggerInfo);

                if (completionList != null)
                {
                    Assert.True(exclusive == completionList.GetTestAccessor().IsExclusive, "group.IsExclusive == " + completionList.GetTestAccessor().IsExclusive);
                }
            }
        }
    }
}
