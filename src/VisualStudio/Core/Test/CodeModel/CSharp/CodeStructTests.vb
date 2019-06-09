' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeStructTests
        Inherits AbstractCodeStructTests

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes1()
            Dim code =
<Code>
struct $$C { }
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes2()
            Dim code =
<Code>
using System;

[Serializable]
struct $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes3()
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
struct $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes4()
            Dim code =
<Code>using System;

[Serializable, CLSCompliant(true)]
struct $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub
#End Region

#Region "Bases tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBase1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBase2()
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub


#End Region

#Region "DataTypeKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDataTypeKind1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestDataTypeKind(code, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDataTypeKind2()
            Dim code =
<Code>
partial struct $$S { }

partial struct S { }
</Code>

            TestDataTypeKind(code, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial)
        End Sub

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestFullName(code, "S")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName2()
            Dim code =
<Code>
namespace N
{
    struct $$S { }
}
</Code>

            TestFullName(code, "N.S")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName3()
            Dim code =
<Code>
namespace N
{
    class C
    {
        struct $$S { }
    }
}
</Code>

            TestFullName(code, "N.C.S")
        End Sub

#End Region

#Region "ImplementedInterfaces tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestImplementedInterfaces1()
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            TestImplementedInterfaces(code, IsElement("IDisposable", kind:=EnvDTE.vsCMElement.vsCMElementInterface))
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestName(code, "S")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
namespace N
{
    struct $$S { }
}
</Code>

            TestName(code, "S")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName3()
            Dim code =
<Code>
namespace N
{
    class C
    {
        struct $$S { }
    }
}
</Code>

            TestName(code, "S")
        End Sub

#End Region

#Region "Parent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParent1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestParent(code, IsFileCodeModel)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParent2()
            Dim code =
<Code>
namespace N
{
    struct $$S { }
}
</Code>

            TestParent(code, IsElement("N", kind:=EnvDTE.vsCMElement.vsCMElementNamespace))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParent3()
            Dim code =
<Code>
namespace N
{
    class C
    {
        struct $$S { }
    }
}
</Code>

            TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts1()
            Dim code =
<Code>
struct $$S
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts2()
            Dim code =
<Code>
partial struct $$S
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts3()
            Dim code =
<Code>
partial struct $$S
{
}

partial struct S
{
}
</Code>

            TestParts(code, 2)
        End Sub
#End Region

#Region "AddAttribute tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
using System;

struct $$S { }
</Code>

            Dim expected =
<Code>
using System;

[Serializable()]
struct S { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
using System;

[Serializable]
struct $$S { }
</Code>

            Dim expected =
<Code>
using System;

[Serializable]
[CLSCompliant(true)]
struct S { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
            Dim code =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
struct $$S { }
</Code>

            Dim expected =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[CLSCompliant(true)]
struct S { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction1() As Task
            Dim code =
<Code>
struct $$S { }
</Code>

            Dim expected =
<Code>
struct S
{
    void Goo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Goo", .Type = "void"})
        End Function

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddImplementedInterface1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface2() As Task
            Dim code =
<Code>
struct $$S { }
interface I { }
</Code>

            Dim expected =
<Code>
struct S : I { }
interface I { }
</Code>

            Await TestAddImplementedInterface(code, "I", -1, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface3() As Task
            Dim code =
<Code>
struct $$S : I { }
interface I { }
interface J { }
</Code>

            Dim expected =
<Code>
struct S : I, J { }
interface I { }
interface J { }
</Code>

            Await TestAddImplementedInterface(code, "J", -1, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface4() As Task
            Dim code =
<Code>
struct $$S : I { }
interface I { }
interface J { }
</Code>

            Dim expected =
<Code>
struct S : J, I { }
interface I { }
interface J { }
</Code>

            Await TestAddImplementedInterface(code, "J", 0, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface5() As Task
            Dim code =
<Code>
struct $$S : I, K { }
interface I { }
interface J { }
interface K { }
</Code>

            Dim expected =
<Code>
struct S : I, J, K { }
interface I { }
interface J { }
interface K { }
</Code>

            Await TestAddImplementedInterface(code, "J", 1, expected)
        End Function

#End Region

#Region "RemoveImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface1() As Task
            Dim code =
<Code>
struct $$S : I { }
interface I { }
</Code>

            Dim expected =
<Code>
struct S { }
interface I { }
</Code>
            Await TestRemoveImplementedInterface(code, "I", expected)
        End Function

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
struct $$Goo
{
}
</Code>

            Dim expected =
<Code>
struct Bar
{
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
struct $$S
{
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeStruct2)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
