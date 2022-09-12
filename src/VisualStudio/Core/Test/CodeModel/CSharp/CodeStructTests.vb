' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    <Trait(Traits.Feature, Traits.Features.CodeModel)>
    Public Class CodeStructTests
        Inherits AbstractCodeStructTests

#Region "Attributes tests"

        <WpfFact
#Region "Attributes tests"
>
        Public Sub TestAttributes1()
            Dim code =
<Code>
struct $$C { }
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <WpfFact>
        Public Sub TestAttributes2()
            Dim code =
<Code>
using System;

[Serializable]
struct $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <WpfFact>
        Public Sub TestAttributes3()
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
struct $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <WpfFact>
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

        <WpfFact
#Region "Bases tests"
>
        Public Sub TestBase1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <WpfFact>
        Public Sub TestBase2()
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

#End Region

#Region "DataTypeKind tests"

        <WpfFact
#Region "DataTypeKind tests"
>
        Public Sub TestDataTypeKind1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestDataTypeKind(code, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Sub

        <WpfFact>
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

        <WpfFact
#Region "FullName tests"
>
        Public Sub TestFullName1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestFullName(code, "S")
        End Sub

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact
#Region "ImplementedInterfaces tests"
>
        Public Sub TestImplementedInterfaces1()
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            TestImplementedInterfaces(code, IsElement("IDisposable", kind:=EnvDTE.vsCMElement.vsCMElementInterface))
        End Sub

#End Region

#Region "Name tests"

        <WpfFact
#Region "Name tests"
>
        Public Sub TestName1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestName(code, "S")
        End Sub

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact
#Region "Parent tests"
>
        Public Sub TestParent1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestParent(code, IsFileCodeModel)
        End Sub

        <WpfFact>
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

        <WpfFact>
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
        <WpfFact
#Region "Parts tests"
>
        Public Sub TestParts1()
            Dim code =
<Code>
struct $$S
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <WpfFact>
        Public Sub TestParts2()
            Dim code =
<Code>
partial struct $$S
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <WpfFact>
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
        <WpfFact
#Region "AddAttribute tests"
>
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

        <WpfFact>
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
        <WpfFact>
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

        <WpfFact
#Region "AddFunction tests"
>
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

        <WpfFact
#Region "AddImplementedInterface tests"
>
        Public Sub TestAddImplementedInterface1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", Nothing)
        End Sub

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact
#Region "RemoveImplementedInterface tests"
>
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
        <WpfFact
#Region "Set Name tests"
>
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

        <WpfFact>
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
