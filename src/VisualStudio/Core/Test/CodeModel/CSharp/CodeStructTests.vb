' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeStructTests
        Inherits AbstractCodeStructTests

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes1()
            Dim code =
<Code>
struct $$C { }
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes2()
            Dim code =
<Code>
using System;

[Serializable]
struct $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes3()
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
struct $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes4()
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
        Public Sub Base1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Base2()
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub


#End Region

#Region "DataTypeKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DataTypeKind1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestDataTypeKind(code, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DataTypeKind2()
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
        Public Sub FullName1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestFullName(code, "S")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName2()
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
        Public Sub FullName3()
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
        Public Sub ImplementedInterfaces1()
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            TestImplementedInterfaces(code, IsElement("IDisposable", kind:=EnvDTE.vsCMElement.vsCMElementInterface))
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestName(code, "S")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name2()
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
        Public Sub Name3()
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
        Public Sub Parent1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestParent(code, IsFileCodeModel)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parent2()
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
        Public Sub Parent3()
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
        Public Sub Parts1()
            Dim code =
<Code>
struct $$S
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts2()
            Dim code =
<Code>
partial struct $$S
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts3()
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
        Public Sub AddAttribute1()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction1()
            Dim code =
<Code>
struct $$S { }
</Code>

            Dim expected =
<Code>
struct S
{
    void Foo()
    {

    }
}
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
        End Sub

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface1()
            Dim code =
<Code>
struct $$S { }
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface2()
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

            TestAddImplementedInterface(code, "I", -1, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface3()
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

            TestAddImplementedInterface(code, "J", -1, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface4()
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

            TestAddImplementedInterface(code, "J", 0, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface5()
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

            TestAddImplementedInterface(code, "J", 1, expected)
        End Sub

#End Region

#Region "RemoveImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface1()
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
            TestRemoveImplementedInterface(code, "I", expected)
        End Sub

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
struct $$Foo
{
}
</Code>

            Dim expected =
<Code>
struct Bar
{
}
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub
#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
            Dim code =
<Code>
struct $$S
{
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Namespace", "Bases", "Members", "Access", "Attributes",
                 "DocComment", "Comment", "DerivedTypes", "ImplementedInterfaces", "IsAbstract",
                 "IsGeneric", "DataTypeKind", "Parts"}

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
