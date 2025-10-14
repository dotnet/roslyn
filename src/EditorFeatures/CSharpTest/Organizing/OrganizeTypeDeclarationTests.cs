// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.Organizing;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing;

public sealed class OrganizeTypeDeclarationTests : AbstractOrganizerTests
{
    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task TestFieldsWithoutInitializers1(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int A;
                int B;
                int C;
            }
            """, $$"""
            {{typeKind}} C {
                int A;
                int B;
                int C;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task TestNestedTypes(string typeKind)
        => CheckAsync($$"""
            class C {
                {{typeKind}} Nested1 { }
                {{typeKind}} Nested2 { }
                int A;
            }
            """, $$"""
            class C {
                int A;
                {{typeKind}} Nested1 { }
                {{typeKind}} Nested2 { }
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestFieldsWithoutInitializers2(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int C;
                int B;
                int A;
            }
            """, $$"""
            {{typeKind}} C {
                int A;
                int B;
                int C;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record struct")]
    public Task TestFieldsWithInitializers1(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int C = 0;
                int B;
                int A;
            }
            """, $$"""
            {{typeKind}} C {
                int A;
                int B;
                int C = 0;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestFieldsWithInitializers2(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int C = 0;
                int B = 0;
                int A;
            }
            """, $$"""
            {{typeKind}} C {
                int A;
                int C = 0;
                int B = 0;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestEventFieldDeclaration(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                public void Goo() {}
                public event EventHandler MyEvent;
            }
            """, $$"""
            {{typeKind}} C {
                public event EventHandler MyEvent;
                public void Goo() {}
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestEventDeclaration(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C  {
                public void Goo() {}
                public event EventHandler Event
                {
                    remove { }
                    add { }
                }

                public static int Property { get; set; }
            }
            """, $$"""
            {{typeKind}} C  {
                public static int Property { get; set; }
                public event EventHandler Event
                {
                    remove { }
                    add { }
                }

                public void Goo() {}
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestOperator(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C  {
                public void Goo() {}
                public static int operator +(Goo<T> a, int b)
                {
                    return 1;
                }
            }
            """, $$"""
            {{typeKind}} C  {
                public static int operator +(Goo<T> a, int b)
                {
                    return 1;
                }
                public void Goo() {}
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestIndexer(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C  {
                public void Goo() {}
                public T this[int i]
                {
                    get
                    {
                        return default(T);
                    }
                }

                C() {}
            }
            """, $$"""
            {{typeKind}} C  {
                C() {}
                public T this[int i]
                {
                    get
                    {
                        return default(T);
                    }
                }

                public void Goo() {}
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestConstructorAndDestructors(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C  {
                public ~Goo() {}
                enum Days {Sat, Sun};
                public Goo() {}
            }
            """, $$"""
            {{typeKind}} C  {
                public Goo() {}
                public ~Goo() {}
                enum Days {Sat, Sun};
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestInterface(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C  {}
            interface I
            {
               void Goo();
               int Property { get; set; }
               event EventHandler Event;
            }
            """, $$"""
            {{typeKind}} C  {}
            interface I
            {
               event EventHandler Event;
               int Property { get; set; }
               void Goo();
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestStaticInstance(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int A;
                static int B;
                int C;
                static int D;
            }
            """, $$"""
            {{typeKind}} C {
                static int B;
                static int D;
                int A;
                int C;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record struct")]
    public Task TestAccessibility(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int A;
                private int B;
                internal int C;
                protected int D;
                public int E;
                protected internal int F;
            }
            """, $$"""
            {{typeKind}} C {
                public int E;
                protected int D;
                protected internal int F;
                internal int C;
                int A;
                private int B;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestStaticAccessibility(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int A1;
                private int B1;
                internal int C1;
                protected int D1;
                public int E1;
                static int A2;
                static private int B2;
                static internal int C2;
                static protected int D2;
                static public int E2;
            }
            """, $$"""
            {{typeKind}} C {
                public static int E2;
                protected static int D2;
                internal static int C2;
                static int A2;
                private static int B2;
                public int E1;
                protected int D1;
                internal int C1;
                int A1;
                private int B1;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestGenerics(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                void B<X,Y>();
                void B<Z>();
                void B();
                void A<X,Y>();
                void A<Z>();
                void A();
            }
            """, $$"""
            {{typeKind}} C {
                void A();
                void A<Z>();
                void A<X,Y>();
                void B();
                void B<Z>();
                void B<X,Y>();
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestInsidePPRegion(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
            #if true
                int c;
                int b;
                int a;
            #endif
            }
            """, $$"""
            {{typeKind}} C {
            #if true
                int a;
                int b;
                int c;
            #endif
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestInsidePPRegion2(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
            #if true
                int z;
                int y;
                int x;
            #endif
            #if true
                int c;
                int b;
                int a;
            #endif
            }
            """, $$"""
            {{typeKind}} C {
            #if true
                int x;
                int y;
                int z;
            #endif
            #if true
                int a;
                int b;
                int c;
            #endif
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestInsidePPRegion3(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int z;
                int y;
            #if true
                int x;
                int c;
            #endif
                int b;
                int a;
            }
            """, $$"""
            {{typeKind}} C {
                int y;
                int z;
            #if true
                int c;
                int x;
            #endif
                int a;
                int b;
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestInsidePPRegion4(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int c() {
                }
                int b {
                }
                int a {
            #if true
            #endif
                }
            }
            """, $$"""
            {{typeKind}} C {
                int a {
            #if true
            #endif
                }
                int b {
                }
                int c() {
                }
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestInsidePPRegion5(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int c() {
                }
                int b {
                }
                int a {
            #if true
            #else
            #endif
                }
            }
            """, $$"""
            {{typeKind}} C {
                int a {
            #if true
            #else
            #endif
                }
                int b {
                }
                int c() {
                }
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestInsidePPRegion6(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
            #region
                int e() {
                }
                int d() {
                }
                int c() {
            #region
                }
            #endregion
                int b {
                }
                int a {
                }
            #endregion
            }
            """, $$"""
            {{typeKind}} C {
            #region
                int d() {
                }
                int e() {
                }
                int c() {
            #region
                }
            #endregion
                int a {
                }
                int b {
                }
            #endregion
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestPinned(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int z() {
                }
                int y() {
                }
                int x() {
            #if true
                }
                int n;
                int m;
                int c() {
            #endif
                }
                int b() {
                }
                int a() {
                }
            }
            """, $$"""
            {{typeKind}} C {
                int y() {
                }
                int z() {
                }
                int x() {
            #if true
                }
                int m;
                int n;
                int c() {
            #endif
                }
                int a() {
                }
                int b() {
                }
            }
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestSensitivity(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} C {
                int Bb;
                int B;
                int bB;
                int b;
                int Aa;
                int a;
                int A;
                int aa;
                int aA;
                int AA;
                int bb;
                int BB;
                int bBb;
                int bbB;
                int あ;
                int ア;
                int ｱ;
                int ああ;
                int あア;
                int あｱ;
                int アあ;
                int cC;
                int Cc;
                int アア;
                int アｱ;
                int ｱあ;
                int ｱア;
                int ｱｱ;
                int BBb;
                int BbB;
                int bBB;
                int BBB;
                int c;
                int C;
                int bbb;
                int Bbb;
                int cc;
                int cC;
                int CC;
            }
            """, $$"""
            {{typeKind}} C {
                int a;
                int A;
                int aa;
                int aA;
                int Aa;
                int AA;
                int b;
                int B;
                int bb;
                int bB;
                int Bb;
                int BB;
                int bbb;
                int bbB;
                int bBb;
                int bBB;
                int Bbb;
                int BbB;
                int BBb;
                int BBB;
                int c;
                int C;
                int cc;
                int cC;
                int cC;
                int Cc;
                int CC;
                int ア;
                int ｱ;
                int あ;
                int アア;
                int アｱ;
                int ｱア;
                int ｱｱ;
                int アあ;
                int ｱあ;
                int あア;
                int あｱ;
                int ああ;
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestWhitespaceBetweenMethods1(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                void A()
                {
                }

                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestWhitespaceBetweenMethods2(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                void B()
                {
                }


                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                void A()
                {
                }


                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestWhitespaceBetweenMethods3(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {

                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {

                void A()
                {
                }

                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestWhitespaceBetweenMethods4(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {


                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {


                void A()
                {
                }

                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestWhitespaceBetweenMethods5(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {


                void B()
                {
                }


                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {


                void A()
                {
                }


                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestWhitespaceBetweenMethods6(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {


                void B()
                {
                }



                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {


                void A()
                {
                }



                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestMoveComments1(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                // B
                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                void A()
                {
                }

                // B
                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestMoveComments2(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                // B
                void B()
                {
                }

                // A
                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                // A
                void A()
                {
                }

                // B
                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestMoveDocComments1(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                /// B
                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                void A()
                {
                }

                /// B
                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestMoveDocComments2(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                /// B

                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                void A()
                {
                }

                /// B

                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestDoNotMoveBanner(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                // Banner

                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                // Banner

                void A()
                {
                }

                void B()
                {
                }
            }
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
    [InlineData("class")]
    [InlineData("record")]
    public Task TestDoNotMoveBanner2(string typeKind)
        => CheckAsync($$"""
            {{typeKind}} Program
            {
                // Banner

                // More banner
                // Bannery stuff

                void B()
                {
                }

                void A()
                {
                }
            }
            """, $$"""
            {{typeKind}} Program
            {
                // Banner

                // More banner
                // Bannery stuff

                void A()
                {
                }

                void B()
                {
                }
            }
            """);

    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.Organizing)]
    [Trait(Traits.Feature, Traits.Features.Interactive)]
    public void OrganizingCommandsDisabledInSubmission()
    {
        using var workspace = EditorTestWorkspace.Create(XElement.Parse("""
            <Workspace>
                <Submission Language="C#" CommonReferences="true">
                    class C
                    {
                        object $$goo;
                    }
                </Submission>
            </Workspace>
            """),
            workspaceKind: WorkspaceKind.Interactive,
            composition: EditorTestCompositions.EditorFeatures);
        // Force initialization.
        workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

        var textView = workspace.Documents.Single().GetTextView();

        var handler = new OrganizeDocumentCommandHandler(
            workspace.GetService<IThreadingContext>(),
            workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());

        var state = handler.GetCommandState(new SortAndRemoveUnnecessaryImportsCommandArgs(textView, textView.TextBuffer));
        Assert.True(state.IsUnspecified);

        state = handler.GetCommandState(new OrganizeDocumentCommandArgs(textView, textView.TextBuffer));
        Assert.True(state.IsUnspecified);
    }
}
