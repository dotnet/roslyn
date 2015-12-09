' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class EventCollectorTests
        Inherits AbstractEventCollectorTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Rename() As Task
            Dim code =
<Code>
class C
{
}
</Code>

            Dim changedCode =
<Code>
class D
{
}
</Code>

            Await TestAsync(code, changedCode,
                 Rename("D"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddStaticModifier() As Task
            Dim code =
<Code>
class C
{
}
</Code>

            Dim changedCode =
<Code>
static class C
{
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_ChangeToStruct() As Task
            Dim code =
<Code>
class C
{
}
</Code>

            Dim changedCode =
<Code>
struct C
{
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("C", Nothing),
                 Add("C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddField() As Task
            Dim code =
<Code>
class C
{
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int i;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("i", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddSecondField() As Task
            Dim code =
<Code>
class C
{
    int i;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int i, j;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddTwoDisjointFields() As Task
            Dim code =
<Code>
class C
{
    int i, j;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int i, x, j, y;
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddTwoContiguousFields() As Task
            Dim code =
<Code>
class C
{
    int i, x, j, y;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int i, x, a, b, j, y;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("a", "C"),
                 Add("b", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_RemoveTwoDisjointFields() As Task
            Dim code =
<Code>
class C
{
    int i, x, j, y;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int i, j;
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_ReplaceFourFieldsWithProperty() As Task
            Dim code =
<Code>
class C
{
    int i, x, j, y;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int Foo { get; set; }
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("i", "C"),
                 Remove("x", "C"),
                 Remove("j", "C"),
                 Remove("y", "C"),
                 Add("Foo", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_RemoveTwoContiguousFields() As Task
            Dim code =
<Code>
class C
{
    int i, x, a, b, j, y;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int i, x, j, y;
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("a", "C"),
                 Remove("b", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_RemoveField() As Task
            Dim code =
<Code>
class C
{
    int i;
}
</Code>

            Dim changedCode =
<Code>
class C
{
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("i", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_ReplaceFieldWithEvent() As Task
            Dim code =
<Code>
class C
{
    System.EventHandler E;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    event System.EventHandler E;
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("E", "C"),
                 Add("E", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddAutoProperty() As Task
            Dim code =
<Code>
class C
{
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int Foo { get; set; }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("Foo", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddIndexer() As Task
            Dim code =
<Code>
class C
{
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int this[int index] { get { return 0; } }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("this", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddConstModifier() As Task
            Dim code =
<Code>
class C
{
    int i;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    const int i;
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("i"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_AddAccessModifier() As Task
            Dim code =
<Code>
class C
{
    void M() { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    private void M() { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_AddParameter1() As Task
            Dim code =
<Code>
class C
{
    void M() { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    void M(int i) { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("i", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_AddParameter2() As Task
            Dim code =
<Code>
class C
{
    void M(int i) { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    void M(int i, int j) { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("j", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_RemoveParameter() As Task
            Dim code =
<Code>
class C
{
    void M(int i) { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    void M() { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("i", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_AddAttribute1() As Task
            Dim code =
<Code>
class C
{
    void M(int i) { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(true)]
    void M(int i) { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_AddAttribute2() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    void M(int i) { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.Diagnostics.Conditional("DEBUG"), System.CLSCompliant(true)]
    void M(int i) { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("System.Diagnostics.Conditional", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_RemoveAttributes() As Task
            Dim code =
<Code>
class C
{
    [System.Diagnostics.Conditional("DEBUG"), System.CLSCompliant(true)]
    void M(int i) { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    void M(int i) { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("System.Diagnostics.Conditional", "M"),
                 Remove("System.CLSCompliant", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_ChangeAttribute() As Task
            Dim code =
<Code>
class C
{
    [System.Diagnostics.Conditional("DEBUG")]
    void M(int i) { }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.Diagnostics.Conditional("TRACE")]
    void M(int i) { }
}
</Code>

            Await TestAsync(code, changedCode,
                 ArgChange("System.Diagnostics.Conditional"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestLargeTopLevelReplace() As Task
            Dim code =
<Code>
class A { }
</Code>

            Dim changedCode =
<Code>
class B { }
class C { }
class D { }
class E { }
class F { }
class G { }
class H { }
class I { }
class J { }
class K { }
</Code>

            Await TestAsync(code, changedCode,
                 Unknown(Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestReplaceWithTwoClasses() As Task
            Dim code =
<Code>
namespace N
{
    class A { }
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    class B { }
    class C { }
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestChangingClassClassToPartial() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
    }
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    parclass C
    {
    }
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("C", "N"),
                 Add("C", "N"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestRenameDelegate() As Task
            Dim code =
<Code>
namespace N
{
    public delegate void Foo();
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    public delegate void Bar();
}
</Code>

            Await TestAsync(code, changedCode,
                 Rename("Bar"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestBug788750() As Task
            Dim code =
<Code>
class C
{
    public int X;

    void Bar()
    {
        var b = new B()
        {
            ID = 0;
        }

        X = 0;
    }
}

class B
{
    public int ID { get; set; }
}
</Code>

            Dim changedCode =
<Code>
class C
{
    public int X;

    void Bar()
    {
        var b = new B()
        {
            ID = 0;
        };

        X = 0;
    }
}

class B
{
    public int ID { get; set; }
}
</Code>

            Await TestAsync(code, changedCode)
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddAttributeToField() As Task
            Dim code =
<Code>
class C
{
    int foo;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int foo;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "foo"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddAttributeToTwoFields() As Task
            Dim code =
<Code>
class C
{
    int foo, bar;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int foo, bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "foo"),
                 Add("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveAttributeFromField() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int foo;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int foo;
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("System.CLSCompliant", "foo"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveAttributeFromTwoFields() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int foo, bar;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int foo, bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("System.CLSCompliant", "foo"),
                 Remove("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_ChangeAttributeOnField() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int foo;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(false)]
    int foo;
}
</Code>

            Await TestAsync(code, changedCode,
                 ArgChange("System.CLSCompliant", "foo"))
        End Function

        <WorkItem(1147865)>
        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_ChangeAttributeOnTwoFields() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int foo, bar;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(false)]
    int foo, bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 ArgChange("System.CLSCompliant", "foo"),
                 ArgChange("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddOneMoreAttribute() As Task
            Dim code =
<Code>
using System;

class Program
{
    [System.NonSerialized()]
    public int bar;
}
</Code>

            Dim changedCode =
<Code>
using System;

class Program
{
    [System.NonSerialized(), System.CLSCompliant(true)]
    public int bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveOneAttribute() As Task
            Dim code =
<Code>
using System;

class Program
{
    [System.NonSerialized(), System.CLSCompliant(true)]
    public int bar;
}
</Code>

            Dim changedCode =
<Code>
using System;

class Program
{
    [System.NonSerialized()]
    public int bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedMethodsInNamespace() As Task
            Dim code =
<Code>
namespace N
{
    void M()
    {
    }
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    void M()
    {
    }

    void M()
    {
    }
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedPropertiesInNamespace() As Task
            Dim code =
<Code>
namespace N
{
    int P { get { return 42; } }
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    int P { get { return 42; } }
    int P { get { return 42; } }
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedEventsInNamespace1() As Task
            Dim code =
<Code>
namespace N
{
    event System.EventHandler E;
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    event System.EventHandler E;
    event System.EventHandler E;
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedEventsInNamespace2() As Task
            Dim code =
<Code>
namespace N
{
    event System.EventHandler E
    {
        add { }
        remove { }
    }
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    event System.EventHandler E
    {
        add { }
        remove { }
    }

    event System.EventHandler E
    {
        add { }
        remove { }
    }
}
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
