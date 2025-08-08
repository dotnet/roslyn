// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CallHierarchy;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CallHierarchy)]
public sealed class CSharpCallHierarchyTests
{
    [WpfFact]
    public async Task InvokeOnMethod()
    {
        var text = """
            namespace N
            {
                class C
                {
                    void G$$oo()
                    {
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo()");
    }

    [WpfFact]
    public async Task InvokeOnProperty()
    {
        var text = """
            namespace N
            {
                class C
                {
                    public int G$$oo { get; set;}
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo");
    }

    [WpfFact]
    public async Task InvokeOnEvent()
    {
        var text = """
            using System;
            namespace N
            {
                class C
                {
                    public event EventHandler Go$$o;
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo");
    }

    [WpfFact]
    public async Task Method_FindCalls()
    {
        var text = """
            namespace N
            {
                class C
                {
                    void G$$oo()
                    {
                    }
                }

                class G
                {
                    void Main()
                    {
                        var c = new C();
                        c.Goo();
                    }

                    void Main2()
                    {
                        var c = new C();
                        c.Goo();
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), ["N.G.Main()", "N.G.Main2()"]);
    }

    [WpfFact]
    public async Task Method_InterfaceImplementation()
    {
        var text = """
            namespace N
            {
                interface I
                {
                    void Goo();
                }

                class C : I
                {
                    public void G$$oo()
                    {
                    }
                }

                class G
                {
                    void Main()
                    {
                        I c = new C();
                        c.Goo();
                    }

                    void Main2()
                    {
                        var c = new C();
                        c.Goo();
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), string.Format(EditorFeaturesResources.Calls_To_Interface_Implementation_0, "N.I.Goo()")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), ["N.G.Main2()"]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_Interface_Implementation_0, "N.I.Goo()"), ["N.G.Main()"]);
    }

    [WpfFact]
    public async Task Method_CallToOverride()
    {
        var text = """
            namespace N
            {
                class C
                {
                    protected virtual void G$$oo() { }
                }

                class D : C
                {
                    protected override void Goo() { }

                    void Bar()
                    {
                        C c; 
                        c.Goo()
                    }

                    void Baz()
                    {
                        D d;
                        d.Goo();
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), EditorFeaturesResources.Calls_To_Overrides]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), ["N.D.Bar()"]);
        testState.VerifyResult(root, EditorFeaturesResources.Calls_To_Overrides, ["N.D.Baz()"]);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829705")]
    public async Task Method_CallToBase()
    {
        var text = """
            namespace N
            {
                class C
                {
                    protected virtual void Goo() { }
                }

                class D : C
                {
                    protected override void Goo() { }

                    void Bar()
                    {
                        C c; 
                        c.Goo()
                    }

                    void Baz()
                    {
                        D d;
                        d.Go$$o();
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.D.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), string.Format(EditorFeaturesResources.Calls_To_Base_Member_0, "N.C.Goo()")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), ["N.D.Baz()"]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_Base_Member_0, "N.C.Goo()"), ["N.D.Bar()"]);
    }

    [WpfFact]
    public async Task FieldInitializers()
    {
        var text = """
            namespace N
            {
                class C
                {
                    public int goo = Goo();

                    protected int Goo$$() { return 0; }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo")]);
        testState.VerifyResultName(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), [EditorFeaturesResources.Initializers]);
    }

    [WpfFact]
    public async Task FieldReferences()
    {
        var text = """
            namespace N
            {
                class C
                {
                    public int g$$oo;

                    protected void Goo() { goo = 3; }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.goo", [string.Format(EditorFeaturesResources.References_To_Field_0, "goo")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.References_To_Field_0, "goo"), ["N.C.Goo()"]);
    }

    [WpfFact]
    public async Task PropertyGet()
    {
        var text = """
            namespace N
            {
                class C
                {
                    public int val
                    {
                        g$$et
                        {
                            return 0;
                        }
                    }

                    public int goo()
                    {
                        var x = this.val;
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.val.get", [string.Format(EditorFeaturesResources.Calls_To_0, "get_val")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "get_val"), ["N.C.goo()"]);
    }

    [WpfFact]
    public async Task Generic()
    {
        var text = """
            namespace N
            {
                class C
                {
                    public int gen$$eric<T>(this string generic, ref T stuff)
                    {
                        return 0;
                    }

                    public int goo()
                    {
                        int i;
                        generic(", ref i);
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.generic<T>(this string, ref T)", [string.Format(EditorFeaturesResources.Calls_To_0, "generic")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "generic"), ["N.C.goo()"]);
    }

    [WpfFact]
    public async Task ExtensionMethods()
    {
        var text = """
            namespace ConsoleApplication10
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = "string";
                        x.BarStr$$ing();
                    }
                }

                public static class Extensions
                {
                    public static string BarString(this string s)
                    {
                        return s;
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "ConsoleApplication10.Extensions.BarString(this string)", [string.Format(EditorFeaturesResources.Calls_To_0, "BarString")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "BarString"), ["ConsoleApplication10.Program.Main(string[])"]);
    }

    [WpfFact]
    public async Task GenericExtensionMethods()
    {
        var text = """
            using System.Collections.Generic;
            using System.Linq;
            namespace N
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> x = new List<int>();
                        var z = x.Si$$ngle();
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "System.Linq.Enumerable.Single<TSource>(this System.Collections.Generic.IEnumerable<TSource>)", [string.Format(EditorFeaturesResources.Calls_To_0, "Single")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Single"), ["N.Program.Main(string[])"]);
    }

    [WpfFact]
    public async Task InterfaceImplementors()
    {
        var text = """
            namespace N
            {
                interface I
                {
                    void Go$$o();
                }

                class C : I
                {
                    public void Goo()
                    {
                    }
                }

                class G
                {
                    void Main()
                    {
                        I c = new C();
                        c.Goo();
                    }

                    void Main2()
                    {
                        var c = new C();
                        c.Goo();
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.I.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), string.Format(EditorFeaturesResources.Implements_0, "Goo")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), ["N.G.Main()"]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Implements_0, "Goo"), ["N.C.Goo()"]);
    }

    [WpfFact]
    public async Task NoFindOverridesOnSealedMethod()
    {
        var text = """
            namespace N
            {
                class C
                {
                    void G$$oo()
                    {
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        Assert.DoesNotContain("Overrides", root.SupportedSearchCategories.Select(s => s.DisplayName));
    }

    [WpfFact]
    public async Task FindOverrides()
    {
        var text = """
            namespace N
            {
                class C
                {
                    public virtual void G$$oo()
                    {
                    }
                }

                class G : C
                {
                    public override void Goo()
                    {
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), EditorFeaturesResources.Overrides_]);
        testState.VerifyResult(root, EditorFeaturesResources.Overrides_, ["N.G.Goo()"]);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844613")]
    public async Task AbstractMethodInclusionToOverrides()
    {
        var text = """
            using System;

            abstract class Base
            {
                public abstract void $$M();
            }

            class Derived : Base
            {
                public override void M()
                {
                    throw new NotImplementedException();
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "Base.M()", [string.Format(EditorFeaturesResources.Calls_To_0, "M"), EditorFeaturesResources.Overrides_, EditorFeaturesResources.Calls_To_Overrides]);
        testState.VerifyResult(root, EditorFeaturesResources.Overrides_, ["Derived.M()"]);
    }

    [WpfFact]
    public async Task SearchAfterEditWorks()
    {
        var text = """
            namespace N
            {
                class C
                {
                    void G$$oo()
                    {
                    }

                    void M()
                    {   
                        Goo();
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();

        testState.Workspace.Documents.Single().GetTextBuffer().Insert(0, "/* hello */");

        testState.VerifyRoot(root, "N.C.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo"),]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), expectedCallers: ["N.C.M()"]);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57856")]
    public async Task PropertySet()
    {
        var code = """
            namespace N
            {
                class C
                {
                    public int Property { get; s$$et; }
                    void M()
                    {
                        Property = 2;
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(code);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Property.set", [string.Format(EditorFeaturesResources.Calls_To_0, "set_Property")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "set_Property"), ["N.C.M()"]);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77327")]
    public async Task PrimaryConstructor()
    {
        var code = """
            public class $$Class1(string test)
            {
            }

            class D
            {
                public void M()
                {
                    var c = new Class1("test");
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(code);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "Class1.Class1(string)", [string.Format(EditorFeaturesResources.Calls_To_0, ".ctor")]);
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, ".ctor"), ["D.M()"]);
    }

    [WpfFact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/71068")]
    public async Task Method_ExcludeNameofReferencesWithoutMemberAccess()
    {
        var text = """
            namespace N
            {
                class G
                {
                    void B$$oo()
                    {
                    }

                    void Main()
                    {
                        var g = new G();
                        g.Boo(); // This should appear in call hierarchy
                    }

                    void TestNameof()
                    {
                        var methodName = nameof(Boo); // This should NOT appear
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.G.Boo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Boo")]);
        // Only the actual method call should appear, not the nameof reference
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Boo"), ["N.G.Main()"]);
    }

    [WpfFact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/71068")]
    public async Task Method_ExcludeNameofReferences()
    {
        var text = """
            namespace N
            {
                class C
                {
                    void G$$oo()
                    {
                    }
                }

                class G
                {
                    void Main()
                    {
                        var c = new C();
                        c.Goo(); // This should appear in call hierarchy
                    }

                    void TestNameof()
                    {
                        var methodName = nameof(C.Goo); // This should NOT appear
                    }
                }
            }
            """;
        using var testState = CallHierarchyTestState.Create(text);
        var root = await testState.GetRootAsync();
        testState.VerifyRoot(root, "N.C.Goo()", [string.Format(EditorFeaturesResources.Calls_To_0, "Goo")]);
        // Only the actual method call should appear, not the nameof reference
        testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), ["N.G.Main()"]);
    }
}
