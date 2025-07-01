' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class EventCollectorTests
        Inherits AbstractEventCollectorTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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
    int Goo { get; set; }
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("i", "C"),
                 Remove("x", "C"),
                 Remove("j", "C"),
                 Remove("y", "C"),
                 Add("Goo", "C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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
    int Goo { get; set; }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("Goo", "C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestChangingClassClassToPartial1() As Task
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
                 Remove("C", "N"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestChangingClassClassToPartial2() As Task
            Dim code =
<Code>
namespace N
{
    parclass C
    {
    }
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    partial class C
    {
    }
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("C", "N"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestRenameDelegate() As Task
            Dim code =
<Code>
namespace N
{
    public delegate void Goo();
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844611")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddAttributeToField() As Task
            Dim code =
<Code>
class C
{
    int goo;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int goo;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "goo"))
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844611")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddAttributeToTwoFields() As Task
            Dim code =
<Code>
class C
{
    int goo, bar;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int goo, bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "goo"),
                 Add("System.CLSCompliant", "bar"))
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844611")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveAttributeFromField() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int goo;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int goo;
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("System.CLSCompliant", "goo"))
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844611")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveAttributeFromTwoFields() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int goo, bar;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    int goo, bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 Remove("System.CLSCompliant", "goo"),
                 Remove("System.CLSCompliant", "bar"))
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844611")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_ChangeAttributeOnField() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int goo;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(false)]
    int goo;
}
</Code>

            Await TestAsync(code, changedCode,
                 ArgChange("System.CLSCompliant", "goo"))
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147865")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844611")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_ChangeAttributeOnTwoFields() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    int goo, bar;
}
</Code>

            Dim changedCode =
<Code>
class C
{
    [System.CLSCompliant(false)]
    int goo, bar;
}
</Code>

            Await TestAsync(code, changedCode,
                 ArgChange("System.CLSCompliant", "goo"),
                 ArgChange("System.CLSCompliant", "bar"))
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147865")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147865")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DoNotFireEventForMethodAddedInsideNamespace() As Task
            Dim code =
<Code>
namespace N
{
}
</Code>

            Dim changedCode =
<Code>
namespace N
{
    void M()
    {
    }
}
</Code>

            Await TestAsync(code, changedCode)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DoNotCrashOnDuplicatedMethodsInNamespace() As Task
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

            Await TestAsync(code, changedCode)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DoNotCrashOnDuplicatedPropertiesInNamespace() As Task
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

            Await TestAsync(code, changedCode)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DoNotCrashOnDuplicatedEventsInNamespace1() As Task
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

            Await TestAsync(code, changedCode)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DoNotCrashOnDuplicatedEventsInNamespace2() As Task
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

            Await TestAsync(code, changedCode)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
