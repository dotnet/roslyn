' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class EventCollectorTests
        Inherits AbstractEventCollectorTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Rename()
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

            Test(code, changedCode,
                 Rename("D"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddStaticModifier()
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

            Test(code, changedCode,
                 Unknown("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_ChangeToStruct()
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

            Test(code, changedCode,
                 Remove("C", Nothing),
                 Add("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddField()
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

            Test(code, changedCode,
                 Add("i", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddSecondField()
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

            Test(code, changedCode,
                 Add("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddTwoDisjointFields()
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

            Test(code, changedCode,
                 Unknown("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddTwoContiguousFields()
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

            Test(code, changedCode,
                 Add("a", "C"),
                 Add("b", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_RemoveTwoDisjointFields()
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

            Test(code, changedCode,
                 Unknown("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_ReplaceFourFieldsWithProperty()
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

            Test(code, changedCode,
                 Remove("i", "C"),
                 Remove("x", "C"),
                 Remove("j", "C"),
                 Remove("y", "C"),
                 Add("Foo", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_RemoveTwoContiguousFields()
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

            Test(code, changedCode,
                 Remove("a", "C"),
                 Remove("b", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_RemoveField()
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

            Test(code, changedCode,
                 Remove("i", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_ReplaceFieldWithEvent()
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

            Test(code, changedCode,
                 Remove("E", "C"),
                 Add("E", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddAutoProperty()
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

            Test(code, changedCode,
                 Add("Foo", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddIndexer()
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

            Test(code, changedCode,
                 Add("this", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_AddConstModifier()
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

            Test(code, changedCode,
                 Unknown("i"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_AddAccessModifier()
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

            Test(code, changedCode,
                 Unknown("M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_AddParameter1()
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

            Test(code, changedCode,
                 Add("i", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_AddParameter2()
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

            Test(code, changedCode,
                 Add("j", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_RemoveParameter()
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

            Test(code, changedCode,
                 Remove("i", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_AddAttribute1()
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

            Test(code, changedCode,
                 Add("System.CLSCompliant", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_AddAttribute2()
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

            Test(code, changedCode,
                 Add("System.Diagnostics.Conditional", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_RemoveAttributes()
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

            Test(code, changedCode,
                 Remove("System.Diagnostics.Conditional", "M"),
                 Remove("System.CLSCompliant", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_ChangeAttribute()
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

            Test(code, changedCode,
                 ArgChange("System.Diagnostics.Conditional"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub LargeTopLevelReplace()
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

            Test(code, changedCode,
                 Unknown(Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub ReplaceWithTwoClasses()
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

            Test(code, changedCode,
                 Unknown("N"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub ChangingClassClassToPartial()
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

            Test(code, changedCode,
                 Remove("C", "N"),
                 Add("C", "N"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub RenameDelegate()
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

            Test(code, changedCode,
                 Rename("Bar"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub Bug788750()
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

            Test(code, changedCode)
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_AddAttributeToField()
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

            Test(code, changedCode,
                 Add("System.CLSCompliant", "foo"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_AddAttributeToTwoFields()
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

            Test(code, changedCode,
                 Add("System.CLSCompliant", "foo"),
                 Add("System.CLSCompliant", "bar"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_RemoveAttributeFromField()
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

            Test(code, changedCode,
                 Remove("System.CLSCompliant", "foo"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_RemoveAttributeFromTwoFields()
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

            Test(code, changedCode,
                 Remove("System.CLSCompliant", "foo"),
                 Remove("System.CLSCompliant", "bar"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_ChangeAttributeOnField()
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

            Test(code, changedCode,
                 ArgChange("System.CLSCompliant", "foo"))
        End Sub

        <WorkItem(1147865)>
        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_ChangeAttributeOnTwoFields()
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

            Test(code, changedCode,
                 ArgChange("System.CLSCompliant", "foo"),
                 ArgChange("System.CLSCompliant", "bar"))
        End Sub

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_AddOneMoreAttribute()
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

            Test(code, changedCode,
                 Add("System.CLSCompliant", "bar"))
        End Sub

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_RemoveOneAttribute()
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

            Test(code, changedCode,
                 Remove("System.CLSCompliant", "bar"))
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
