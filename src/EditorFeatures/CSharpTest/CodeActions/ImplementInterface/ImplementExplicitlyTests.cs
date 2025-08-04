// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ImplementInterface;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementInterface;

[Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
public sealed class ImplementExplicitlyTests : AbstractCSharpCodeActionTest
{
    private const int SingleMember = 0;
    private const int SameInterface = 1;
    private const int AllInterfaces = 2;

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new CSharpImplementExplicitlyCodeRefactoringProvider();

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Fact]
    public Task TestSingleMember()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo { void Goo1(); void Goo2(); }
            interface IBar { void Bar(); }

            class C : IGoo, IBar
            {
                public void [||]Goo1() { }

                public void Goo2() { }

                public void Bar() { }
            }
            """,
            """
            interface IGoo { void Goo1(); void Goo2(); }
            interface IBar { void Bar(); }

            class C : IGoo, IBar
            {
                void IGoo.Goo1() { }

                public void Goo2() { }

                public void Bar() { }
            }
            """, index: SingleMember);

    [Fact]
    public Task TestSameInterface()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo { void Goo1(); void Goo2(); }
            interface IBar { void Bar(); }

            class C : IGoo, IBar
            {
                public void [||]Goo1() { }

                public void Goo2() { }

                public void Bar() { }
            }
            """,
            """
            interface IGoo { void Goo1(); void Goo2(); }
            interface IBar { void Bar(); }

            class C : IGoo, IBar
            {
                void IGoo.Goo1() { }

                void IGoo.Goo2() { }

                public void Bar() { }
            }
            """, index: SameInterface);

    [Fact]
    public Task TestAllInterfaces()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo { void Goo1(); void Goo2(); }
            interface IBar { void Bar(); }

            class C : IGoo, IBar
            {
                public void [||]Goo1() { }

                public void Goo2() { }

                public void Bar() { }
            }
            """,
            """
            interface IGoo { void Goo1(); void Goo2(); }
            interface IBar { void Bar(); }

            class C : IGoo, IBar
            {
                void IGoo.Goo1() { }

                void IGoo.Goo2() { }

                void IBar.Bar() { }
            }
            """, index: AllInterfaces);

    [Fact]
    public Task TestProperty()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo { int Goo1 { get; } }

            class C : IGoo
            {
                public int [||]Goo1 { get { } }
            }
            """,
            """
            interface IGoo { int Goo1 { get; } }

            class C : IGoo
            {
                int IGoo.Goo1 { get { } }
            }
            """, index: SingleMember);

    [Fact]
    public Task TestEvent()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo { event Action E; }

            class C : IGoo
            {
                public event Action [||]E { add { } remove { } }
            }
            """,
            """
            interface IGoo { event Action E; }

            class C : IGoo
            {
                event Action IGoo.E { add { } remove { } }
            }
            """, index: SingleMember);

    [Fact]
    public Task TestNotOnExplicitMember()
        => TestMissingAsync(
            """
            interface IGoo { void Goo1(); }

            class C : IGoo
            {
                void IGoo.[||]Goo1() { }
            }
            """);

    [Fact]
    public Task TestNotOnUnboundImplicitImpl()
        => TestMissingAsync(
            """
            interface IGoo { void Goo1(); }

            class C
            {
                public void [||]Goo1() { }
            }
            """);

    [Fact]
    public Task TestUpdateReferences_InsideDeclarations_Explicit()
        => TestInRegularAndScriptAsync(
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                public int [||]Prop { get { return this.Prop; } set { this.Prop = value; } }
                public int M(int i) { return this.M(i); }
                public event Action Ev { add { this.Ev += value; } remove { this.Ev -= value; } }
            }
            """,
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                int IGoo.Prop { get { return ((IGoo)this).Prop; } set { ((IGoo)this).Prop = value; } }
                int IGoo.M(int i) { return ((IGoo)this).M(i); }
                event Action IGoo.Ev { add { ((IGoo)this).Ev += value; } remove { ((IGoo)this).Ev -= value; } }
            }
            """, index: SameInterface);

    [Fact]
    public Task TestUpdateReferences_InsideDeclarations_Implicit()
        => TestInRegularAndScriptAsync(
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                public int [||]Prop { get { return Prop; } set { Prop = value; } }
                public int M(int i) { return M(i); }
                public event Action Ev { add { Ev += value; } remove { Ev -= value; } }
            }
            """,
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                int IGoo.Prop { get { return ((IGoo)this).Prop; } set { ((IGoo)this).Prop = value; } }
                int IGoo.M(int i) { return ((IGoo)this).M(i); }
                event Action IGoo.Ev { add { ((IGoo)this).Ev += value; } remove { ((IGoo)this).Ev -= value; } }
            }
            """, index: SameInterface);

    [Fact]
    public Task TestUpdateReferences_InternalImplicit()
        => TestInRegularAndScriptAsync(
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                public int [||]Prop { get { } set { } }
                public int M(int i) { }
                public event Action Ev { add { } remove { } }

                void InternalImplicit()
                {
                    var v = Prop;
                    Prop = 1;
                    Prop++;
                    ++Prop;

                    M(0);
                    M(M(0));

                    Ev += () => {};

                    var v1 = nameof(Prop);
                }
            }
            """,
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                int IGoo.Prop { get { } set { } }
                int IGoo.M(int i) { }
                event Action IGoo.Ev { add { } remove { } }

                void InternalImplicit()
                {
                    var v = ((IGoo)this).Prop;
                    ((IGoo)this).Prop = 1;
                    ((IGoo)this).Prop++;
                    ++((IGoo)this).Prop;

                    ((IGoo)this).M(0);
                    ((IGoo)this).M(((IGoo)this).M(0));

                    ((IGoo)this).Ev += () => {};

                    var v1 = nameof(((IGoo)this).Prop);
                }
            }
            """, index: SameInterface);

    [Fact]
    public Task TestUpdateReferences_InternalExplicit()
        => TestInRegularAndScriptAsync(
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                public int [||]Prop { get { } set { } }
                public int M(int i) { }
                public event Action Ev { add { } remove { } }

                void InternalExplicit()
                {
                    var v = this.Prop;
                    this.Prop = 1;
                    this.Prop++;
                    ++this.Prop;

                    this.M(0);
                    this.M(this.M(0));

                    this.Ev += () => {};

                    var v1 = nameof(this.Prop);
                }
            }
            """,
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                int IGoo.Prop { get { } set { } }
                int IGoo.M(int i) { }
                event Action IGoo.Ev { add { } remove { } }

                void InternalExplicit()
                {
                    var v = ((IGoo)this).Prop;
                    ((IGoo)this).Prop = 1;
                    ((IGoo)this).Prop++;
                    ++((IGoo)this).Prop;

                    ((IGoo)this).M(0);
                    ((IGoo)this).M(((IGoo)this).M(0));

                    ((IGoo)this).Ev += () => {};

                    var v1 = nameof(((IGoo)this).Prop);
                }
            }
            """, index: SameInterface);

    [Fact]
    public Task TestUpdateReferences_External()
        => TestInRegularAndScriptAsync(
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                public int [||]Prop { get { } set { } }
                public int M(int i) { }
                public event Action Ev { add { } remove { } }
            }

            class T
            {
                void External(C c)
                {
                    var v = c.Prop;
                    c.Prop = 1;
                    c.Prop++;
                    ++c.Prop;

                    c.M(0);
                    c.M(c.M(0));

                    c.Ev += () => {};

                    new C
                    {
                        Prop = 1
                    };

                    var v1 = nameof(c.Prop);
                }
            }
            """,
            """
            using System;
            interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            class C : IGoo
            {
                int IGoo.Prop { get { } set { } }
                int IGoo.M(int i) { }
                event Action IGoo.Ev { add { } remove { } }
            }

            class T
            {
                void External(C c)
                {
                    var v = ((IGoo)c).Prop;
                    ((IGoo)c).Prop = 1;
                    ((IGoo)c).Prop++;
                    ++((IGoo)c).Prop;

                    ((IGoo)c).M(0);
                    ((IGoo)c).M(((IGoo)c).M(0));

                    ((IGoo)c).Ev += () => {};

                    new C
                    {
                        Prop = 1
                    };

                    var v1 = nameof(((IGoo)c).Prop);
                }
            }
            """, index: SameInterface);

    [Fact]
    public Task TestUpdateReferences_CrossLanguage()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="A1">
                    <Document FilePath="File.cs">
            using System;
            public interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            public class C : IGoo
            {
                public int [||]Prop { get { } set { } }
                public int M(int i) { }
                public event Action Ev { add { } remove { } }
            }
                    </Document>
                </Project>
                <Project Language="Visual Basic" CommonReferences="true" AssemblyName="A2">
                    <ProjectReference>A1</ProjectReference>
                    <Document>
            class T
                sub External(c1 as C)
                    dim v = c1.Prop
                    c1.Prop = 1

                    c1.M(0)
                    c1.M(c1.M(0))

                    dim x = new C() with {
                        .Prop = 1
                    }

                    dim v1 = nameof(c1.Prop)
                end sub
            end class
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="A1">
                    <Document FilePath="File.cs">
            using System;
            public interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

            public class C : IGoo
            {
                int IGoo.Prop { get { } set { } }
                int IGoo.M(int i) { }
                event Action IGoo.Ev { add { } remove { } }
            }
                    </Document>
                </Project>
                <Project Language="Visual Basic" CommonReferences="true" AssemblyName="A2">
                    <ProjectReference>A1</ProjectReference>
                    <Document>
            class T
                sub External(c1 as C)
                    dim v = DirectCast(c1, IGoo).Prop
                    DirectCast(c1, IGoo).Prop = 1

                    DirectCast(c1, IGoo).M(0)
                    DirectCast(c1, IGoo).M(DirectCast(c1, IGoo).M(0))

                    dim x = new C() with {
                        .Prop = 1
                    }

                    dim v1 = nameof(c1.Prop)
                end sub
            end class
                    </Document>
                </Project>
            </Workspace>
            """, index: SameInterface);

    [Fact]
    public Task TestMemberWhichImplementsMultipleMembers()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo { int M(int i); }
            interface IBar { int M(int i); }

            class C : IGoo, IBar
            {
                public int [||]M(int i)
                {
                    throw new System.Exception();
                }
            }
            """,
            """
            interface IGoo { int M(int i); }
            interface IBar { int M(int i); }

            class C : IGoo, IBar
            {
                int IGoo.M(int i)
                {
                    throw new System.Exception();
                }
                int IBar.M(int i)
                {
                    throw new System.Exception();
                }
            }
            """, index: SingleMember);

    [Fact]
    public Task TestMemberWhichImplementsMultipleMembers2()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo { int M(int i); }
            interface IBar { int M(int i); }

            class C : IGoo, IBar
            {
                public int [||]M(int i)
                {
                    return this.M(1);
                }
            }
            """,
            """
            interface IGoo { int M(int i); }
            interface IBar { int M(int i); }

            class C : IGoo, IBar
            {
                int IGoo.M(int i)
                {
                    return ((IGoo)this).M(1);
                }
                int IBar.M(int i)
                {
                    return ((IGoo)this).M(1);
                }
            }
            """, index: SingleMember);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52020")]
    public Task TestWithContraints()
        => TestInRegularAndScriptAsync(
            """
            interface IRepro
            {
                void A<T>(int value) where T : class;
            }

            class Repro : IRepro
            {
                public void [||]A<T>(int value) where T : class
                {
                }
            }
            """,
            """
            interface IRepro
            {
                void A<T>(int value) where T : class;
            }

            class Repro : IRepro
            {
                void IRepro.A<T>(int value)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52020")]
    public Task TestWithDefaultParameterValues()
        => TestInRegularAndScriptAsync(
            """
            interface IRepro
            {
                void A(int value = 0);
            }

            class Repro : IRepro
            {
                public void [||]A(int value = 0)
                {
                }
            }
            """,
            """
            interface IRepro
            {
                void A(int value = 0);
            }

            class Repro : IRepro
            {
                void IRepro.A(int value)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52020")]
    public Task TestWithMismatchedDefaultParameterValues()
        => TestInRegularAndScriptAsync(
            """
            interface IRepro
            {
                void A(int value = 0);
            }

            class Repro : IRepro
            {
                public void [||]A(int value = 1)
                {
                }
            }
            """,
            """
            interface IRepro
            {
                void A(int value = 0);
            }

            class Repro : IRepro
            {
                void IRepro.A(int value = 1)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52020")]
    public Task TestWithMismatchedDefault1()
        => TestInRegularAndScriptAsync(
            """
            interface IRepro
            {
                void A(int value);
            }

            class Repro : IRepro
            {
                public void [||]A(int value = 1)
                {
                }
            }
            """,
            """
            interface IRepro
            {
                void A(int value);
            }

            class Repro : IRepro
            {
                void IRepro.A(int value = 1)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52020")]
    public Task TestWithMismatchedDefault2()
        => TestInRegularAndScriptAsync(
            """
            interface IRepro
            {
                void A(int value = 0);
            }

            class Repro : IRepro
            {
                public void [||]A(int value)
                {
                }
            }
            """,
            """
            interface IRepro
            {
                void A(int value = 0);
            }

            class Repro : IRepro
            {
                void IRepro.A(int value)
                {
                }
            }
            """);

    [Fact]
    public Task TestPreserveReadOnly()
        => TestInRegularAndScriptAsync(
            """
            interface IRepro
            {
                void A();
            }

            class Repro : IRepro
            {
                public readonly void [||]A()
                {
                }
            }
            """,
            """
            interface IRepro
            {
                void A();
            }

            class Repro : IRepro
            {
                readonly void IRepro.A()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72024")]
    public Task TestFieldEvent()
        => TestInRegularAndScriptAsync(
            """
            using System;

            interface IGoo { event Action E; void M(); }

            class C : IGoo
            {
                public event Action E;
                public void [||]M() { }
            }
            """,
            """
            using System;
            
            interface IGoo { event Action E; void M(); }
            
            class C : IGoo
            {
                public event Action E;
                void IGoo.M() { }
            }
            """, index: SameInterface);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72024")]
    public Task TestPropertyEvent()
        => TestInRegularAndScriptAsync(
            """
            using System;

            interface IGoo { event Action E; }

            class C : IGoo
            {
                public event Action [||]E { add { } remove { } };
            }
            """,
            """
            using System;
            
            interface IGoo { event Action E; }
            
            class C : IGoo
            {
                event Action IGoo.E { add { } remove { } };
            }
            """, index: SingleMember);
}
