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
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing
{
    public class OrganizeTypeDeclarationTests : AbstractOrganizerTests
    {
        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        [InlineData("record class")]
        [InlineData("record struct")]
        public async Task TestFieldsWithoutInitializers1(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int A;
    int B;
    int C;
}}";

            var final =
$@"{typeKind} C {{
    int A;
    int B;
    int C;
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record class")]
        [InlineData("record struct")]
        public async Task TestNestedTypes(string typeKind)
        {
            var initial =
$@"class C {{
    {typeKind} Nested1 {{ }}
    {typeKind} Nested2 {{ }}
    int A;
}}";

            var final =
$@"class C {{
    int A;
    {typeKind} Nested1 {{ }}
    {typeKind} Nested2 {{ }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestFieldsWithoutInitializers2(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int C;
    int B;
    int A;
}}";

            var final =
$@"{typeKind} C {{
    int A;
    int B;
    int C;
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        [InlineData("record struct")]
        public async Task TestFieldsWithInitializers1(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int C = 0;
    int B;
    int A;
}}";

            var final =
$@"{typeKind} C {{
    int A;
    int B;
    int C = 0;
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestFieldsWithInitializers2(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int C = 0;
    int B = 0;
    int A;
}}";

            var final =
$@"{typeKind} C {{
    int A;
    int C = 0;
    int B = 0;
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestEventFieldDeclaration(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    public void Goo() {{}}
    public event EventHandler MyEvent;
}}";

            var final =
$@"{typeKind} C {{
    public event EventHandler MyEvent;
    public void Goo() {{}}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestEventDeclaration(string typeKind)
        {
            var initial =
$@"{typeKind} C  {{
    public void Goo() {{}}
    public event EventHandler Event
    {{
        remove {{ }}
        add {{ }}
    }}

    public static int Property {{ get; set; }}
}}";

            var final =
$@"{typeKind} C  {{
    public static int Property {{ get; set; }}
    public event EventHandler Event
    {{
        remove {{ }}
        add {{ }}
    }}

    public void Goo() {{}}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestOperator(string typeKind)
        {
            var initial =
$@"{typeKind} C  {{
    public void Goo() {{}}
    public static int operator +(Goo<T> a, int b)
    {{
        return 1;
    }}
}}";

            var final =
$@"{typeKind} C  {{
    public static int operator +(Goo<T> a, int b)
    {{
        return 1;
    }}
    public void Goo() {{}}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestIndexer(string typeKind)
        {
            var initial =
$@"{typeKind} C  {{
    public void Goo() {{}}
    public T this[int i]
    {{
        get
        {{
            return default(T);
        }}
    }}

    C() {{}}
}}";

            var final =
$@"{typeKind} C  {{
    C() {{}}
    public T this[int i]
    {{
        get
        {{
            return default(T);
        }}
    }}

    public void Goo() {{}}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestConstructorAndDestructors(string typeKind)
        {
            var initial =
$@"{typeKind} C  {{
    public ~Goo() {{}}
    enum Days {{Sat, Sun}};
    public Goo() {{}}
}}";

            var final =
$@"{typeKind} C  {{
    public Goo() {{}}
    public ~Goo() {{}}
    enum Days {{Sat, Sun}};
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestInterface(string typeKind)
        {
            var initial =
$@"{typeKind} C  {{}}
interface I
{{
   void Goo();
   int Property {{ get; set; }}
   event EventHandler Event;
}}";

            var final =
$@"{typeKind} C  {{}}
interface I
{{
   event EventHandler Event;
   int Property {{ get; set; }}
   void Goo();
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestStaticInstance(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int A;
    static int B;
    int C;
    static int D;
}}";

            var final =
$@"{typeKind} C {{
    static int B;
    static int D;
    int A;
    int C;
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        [InlineData("record struct")]
        public async Task TestAccessibility(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int A;
    private int B;
    internal int C;
    protected int D;
    public int E;
    protected internal int F;
}}";

            var final =
$@"{typeKind} C {{
    public int E;
    protected int D;
    protected internal int F;
    internal int C;
    int A;
    private int B;
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestStaticAccessibility(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
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
}}";

            var final =
$@"{typeKind} C {{
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
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestGenerics(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    void B<X,Y>();
    void B<Z>();
    void B();
    void A<X,Y>();
    void A<Z>();
    void A();
}}";

            var final =
$@"{typeKind} C {{
    void A();
    void A<Z>();
    void A<X,Y>();
    void B();
    void B<Z>();
    void B<X,Y>();
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestInsidePPRegion(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
#if true
    int c;
    int b;
    int a;
#endif
}}";

            var final =
$@"{typeKind} C {{
#if true
    int a;
    int b;
    int c;
#endif
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestInsidePPRegion2(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
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
}}";

            var final =
$@"{typeKind} C {{
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
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestInsidePPRegion3(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int z;
    int y;
#if true
    int x;
    int c;
#endif
    int b;
    int a;
}}";

            var final =
$@"{typeKind} C {{
    int y;
    int z;
#if true
    int c;
    int x;
#endif
    int a;
    int b;
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestInsidePPRegion4(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int c() {{
    }}
    int b {{
    }}
    int a {{
#if true
#endif
    }}
}}";

            var final =
$@"{typeKind} C {{
    int a {{
#if true
#endif
    }}
    int b {{
    }}
    int c() {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestInsidePPRegion5(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int c() {{
    }}
    int b {{
    }}
    int a {{
#if true
#else
#endif
    }}
}}";

            var final =
$@"{typeKind} C {{
    int a {{
#if true
#else
#endif
    }}
    int b {{
    }}
    int c() {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestInsidePPRegion6(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
#region
    int e() {{
    }}
    int d() {{
    }}
    int c() {{
#region
    }}
#endregion
    int b {{
    }}
    int a {{
    }}
#endregion
}}";

            var final =
$@"{typeKind} C {{
#region
    int d() {{
    }}
    int e() {{
    }}
    int c() {{
#region
    }}
#endregion
    int a {{
    }}
    int b {{
    }}
#endregion
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestPinned(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
    int z() {{
    }}
    int y() {{
    }}
    int x() {{
#if true
    }}
    int n;
    int m;
    int c() {{
#endif
    }}
    int b() {{
    }}
    int a() {{
    }}
}}";

            var final =
$@"{typeKind} C {{
    int y() {{
    }}
    int z() {{
    }}
    int x() {{
#if true
    }}
    int m;
    int n;
    int c() {{
#endif
    }}
    int a() {{
    }}
    int b() {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Organizing)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestSensitivity(string typeKind)
        {
            var initial =
$@"{typeKind} C {{
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
}}";

            var final =
$@"{typeKind} C {{
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
}}";

            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestWhitespaceBetweenMethods1(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    void A()
    {{
    }}

    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestWhitespaceBetweenMethods2(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    void B()
    {{
    }}


    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    void A()
    {{
    }}


    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestWhitespaceBetweenMethods3(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{

    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{

    void A()
    {{
    }}

    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestWhitespaceBetweenMethods4(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{


    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{


    void A()
    {{
    }}

    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestWhitespaceBetweenMethods5(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{


    void B()
    {{
    }}


    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{


    void A()
    {{
    }}


    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestWhitespaceBetweenMethods6(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{


    void B()
    {{
    }}



    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{


    void A()
    {{
    }}



    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMoveComments1(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    // B
    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    void A()
    {{
    }}

    // B
    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMoveComments2(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    // B
    void B()
    {{
    }}

    // A
    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    // A
    void A()
    {{
    }}

    // B
    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMoveDocComments1(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    /// B
    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    void A()
    {{
    }}

    /// B
    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMoveDocComments2(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    /// B

    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    void A()
    {{
    }}

    /// B

    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestDoNotMoveBanner(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    // Banner

    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    // Banner

    void A()
    {{
    }}

    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537614")]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestDoNotMoveBanner2(string typeKind)
        {
            var initial =
$@"{typeKind} Program
{{
    // Banner

    // More banner
    // Bannery stuff

    void B()
    {{
    }}

    void A()
    {{
    }}
}}";

            var final =
$@"{typeKind} Program
{{
    // Banner

    // More banner
    // Bannery stuff

    void A()
    {{
    }}

    void B()
    {{
    }}
}}";
            await CheckAsync(initial, final);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Organizing)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void OrganizingCommandsDisabledInSubmission()
        {
            using var workspace = TestWorkspace.Create(XElement.Parse("""
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
                composition: EditorTestCompositions.EditorFeaturesWpf);
            // Force initialization.
            workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

            var textView = workspace.Documents.Single().GetTextView();

            var handler = new OrganizeDocumentCommandHandler(
                workspace.GetService<IThreadingContext>(),
                workspace.GlobalOptions,
                workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());

            var state = handler.GetCommandState(new SortAndRemoveUnnecessaryImportsCommandArgs(textView, textView.TextBuffer));
            Assert.True(state.IsUnspecified);

            state = handler.GetCommandState(new OrganizeDocumentCommandArgs(textView, textView.TextBuffer));
            Assert.True(state.IsUnspecified);
        }
    }
}
