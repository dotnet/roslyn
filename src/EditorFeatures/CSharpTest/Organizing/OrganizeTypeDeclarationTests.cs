// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Editor.Implementation.Organizing;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing
{
    public class OrganizeTypeDeclarationTests : AbstractOrganizerTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestFieldsWithoutInitializers1()
        {
            var initial =
@"class C {
    int A;
    int B;
    int C;
}";

            var final =
@"class C {
    int A;
    int B;
    int C;
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestFieldsWithoutInitializers2()
        {
            var initial =
@"class C {
    int C;
    int B;
    int A;
}";

            var final =
@"class C {
    int A;
    int B;
    int C;
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestFieldsWithInitializers1()
        {
            var initial =
@"class C {
    int C = 0;
    int B;
    int A;
}";

            var final =
@"class C {
    int A;
    int B;
    int C = 0;
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestFieldsWithInitializers2()
        {
            var initial =
@"class C {
    int C = 0;
    int B = 0;
    int A;
}";

            var final =
@"class C {
    int A;
    int C = 0;
    int B = 0;
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestEventFieldDeclaration()
        {
            var initial =
@"class C {
    public void Foo() {}     
    public event EventHandler MyEvent;
}";

            var final =
@"class C {
    public event EventHandler MyEvent;
    public void Foo() {}     
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestEventDeclaration()
        {
            var initial =
@"class C  {
    public void Foo() {}     
    public event EventHandler Event
    {
        remove { }
        add { }
    }

    public static int Property { get; set; }
}";

            var final =
@"class C  {
    public static int Property { get; set; }
    public event EventHandler Event
    {
        remove { }
        add { }
    }

    public void Foo() {}     
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestOperator()
        {
            var initial =
@"class C  {
    public void Foo() {}     
    public static int operator +(Foo<T> a, int b)
    {
        return 1;
    }
}";

            var final =
@"class C  {
    public static int operator +(Foo<T> a, int b)
    {
        return 1;
    }
    public void Foo() {}     
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestIndexer()
        {
            var initial =
@"class C  {
    public void Foo() {}     
    public T this[int i]
    {
        get
        {
            return default(T);
        }
    }

    C() {}
}";

            var final =
@"class C  {
    C() {}
    public T this[int i]
    {
        get
        {
            return default(T);
        }
    }

    public void Foo() {}     
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestConstructorAndDestructors()
        {
            var initial =
@"class C  {
    public ~Foo() {}        
    enum Days {Sat, Sun};        
    public Foo() {}  
}";

            var final =
@"class C  {
    public ~Foo() {}        
    public Foo() {}  
    enum Days {Sat, Sun};        
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestInterface()
        {
            var initial =
@"class C  {}
interface I
{
   void Foo();
   int Property { get; set; }
   event EventHandler Event;
}";

            var final =
@"class C  {}
interface I
{
   event EventHandler Event;
   int Property { get; set; }
   void Foo();
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestStaticInstance()
        {
            var initial =
@"class C {
    int A;
    static int B;
    int C;
    static int D;
}";

            var final =
@"class C {
    static int B;
    static int D;
    int A;
    int C;
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestAccessibility()
        {
            var initial =
@"class C {
    int A;
    private int B;
    internal int C;
    protected int D;
    public int E;
    protected internal int F;
}";

            var final =
@"class C {
    public int E;
    protected int D;
    protected internal int F;
    internal int C;
    int A;
    private int B;
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestStaticAccessibility()
        {
            var initial =
@"class C {
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
}";

            var final =
@"class C {
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
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestGenerics()
        {
            var initial =
@"class C {
    void B<X,Y>();
    void B<Z>();
    void B();
    void A<X,Y>();
    void A<Z>();
    void A();
}";

            var final =
@"class C {
    void A();
    void A<Z>();
    void A<X,Y>();
    void B();
    void B<Z>();
    void B<X,Y>();
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestInsidePPRegion()
        {
            var initial =
@"class C {
#if true
    int c;
    int b;
    int a;
#endif
}";

            var final =
@"class C {
#if true
    int a;
    int b;
    int c;
#endif
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestInsidePPRegion2()
        {
            var initial =
@"class C {
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
}";

            var final =
@"class C {
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
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestInsidePPRegion3()
        {
            var initial =
@"class C {
    int z;
    int y;
#if true
    int x;
    int c;
#endif
    int b;
    int a;
}";

            var final =
@"class C {
    int y;
    int z;
#if true
    int c;
    int x;
#endif
    int a;
    int b;
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestInsidePPRegion4()
        {
            var initial =
@"class C {
    int c() {
    }
    int b {
    }
    int a {
#if true
#endif
    }
}";

            var final =
@"class C {
    int a {
#if true
#endif
    }
    int b {
    }
    int c() {
    }
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestInsidePPRegion5()
        {
            var initial =
@"class C {
    int c() {
    }
    int b {
    }
    int a {
#if true
#else
#endif
    }
}";

            var final =
@"class C {
    int a {
#if true
#else
#endif
    }
    int b {
    }
    int c() {
    }
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestInsidePPRegion6()
        {
            var initial =
@"class C {
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
}";

            var final =
@"class C {
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
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestPinned()
        {
            var initial =
@"class C {
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
}";

            var final =
@"class C {
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
}";
            await CheckAsync(initial, final);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task TestSensitivity()
        {
            var initial =
@"class C {
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
}";

            var final =
@"class C {
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
}";

            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestWhitespaceBetweenMethods1()
        {
            var initial =
@"class Program
{
    void B()
    {
    }

    void A()
    {
    }
}";

            var final =
@"class Program
{
    void A()
    {
    }

    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestWhitespaceBetweenMethods2()
        {
            var initial =
@"class Program
{
    void B()
    {
    }


    void A()
    {
    }
}";

            var final =
@"class Program
{
    void A()
    {
    }


    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestWhitespaceBetweenMethods3()
        {
            var initial =
@"class Program
{

    void B()
    {
    }

    void A()
    {
    }
}";

            var final =
@"class Program
{

    void A()
    {
    }

    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestWhitespaceBetweenMethods4()
        {
            var initial =
@"class Program
{


    void B()
    {
    }

    void A()
    {
    }
}";

            var final =
@"class Program
{


    void A()
    {
    }

    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestWhitespaceBetweenMethods5()
        {
            var initial =
@"class Program
{


    void B()
    {
    }


    void A()
    {
    }
}";

            var final =
@"class Program
{


    void A()
    {
    }


    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestWhitespaceBetweenMethods6()
        {
            var initial =
@"class Program
{


    void B()
    {
    }



    void A()
    {
    }
}";

            var final =
@"class Program
{


    void A()
    {
    }



    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestMoveComments1()
        {
            var initial =
@"class Program
{
    // B
    void B()
    {
    }

    void A()
    {
    }
}";

            var final =
@"class Program
{
    void A()
    {
    }

    // B
    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestMoveComments2()
        {
            var initial =
@"class Program
{
    // B
    void B()
    {
    }

    // A
    void A()
    {
    }
}";

            var final =
@"class Program
{
    // A
    void A()
    {
    }

    // B
    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestMoveDocComments1()
        {
            var initial =
@"class Program
{
    /// B
    void B()
    {
    }

    void A()
    {
    }
}";

            var final =
@"class Program
{
    void A()
    {
    }

    /// B
    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestMoveDocComments2()
        {
            var initial =
@"class Program
{
    /// B

    void B()
    {
    }

    void A()
    {
    }
}";

            var final =
@"class Program
{
    void A()
    {
    }

    /// B

    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestDontMoveBanner()
        {
            var initial =
@"class Program
{
    // Banner

    void B()
    {
    }

    void A()
    {
    }
}";

            var final =
@"class Program
{
    // Banner

    void A()
    {
    }

    void B()
    {
    }
}";
            await CheckAsync(initial, final);
        }

        [WorkItem(537614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [Fact]
        public async Task TestDontMoveBanner2()
        {
            var initial =
@"class Program
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
}";

            var final =
@"class Program
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
}";
            await CheckAsync(initial, final);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Organizing)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public async Task OrganizingCommandsDisabledInSubmission()
        {
            var exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(typeof(InteractiveDocumentSupportsFeatureService)));

            using (var workspace = await TestWorkspace.CreateAsync(XElement.Parse(@"
                <Workspace>
                    <Submission Language=""C#"" CommonReferences=""true"">  
                        class C
                        {
                            object $$foo;
                        }
                    </Submission>
                </Workspace> "),
                workspaceKind: WorkspaceKind.Interactive,
                exportProvider: exportProvider))
            {
                // Force initialization.
                workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

                var textView = workspace.Documents.Single().GetTextView();

                var handler = new OrganizeDocumentCommandHandler(workspace.GetService<Host.IWaitIndicator>());
                var delegatedToNext = false;
                Func<CommandState> nextHandler = () =>
                {
                    delegatedToNext = true;
                    return CommandState.Unavailable;
                };

                var state = handler.GetCommandState(new Commands.SortImportsCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);

                delegatedToNext = false;
                state = handler.GetCommandState(new Commands.SortAndRemoveUnnecessaryImportsCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);

                delegatedToNext = false;
                state = handler.GetCommandState(new Commands.RemoveUnnecessaryImportsCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);

                delegatedToNext = false;
                state = handler.GetCommandState(new Commands.OrganizeDocumentCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);
            }
        }
    }
}
