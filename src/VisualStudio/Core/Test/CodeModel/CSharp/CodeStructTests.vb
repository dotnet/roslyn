' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeStructTests
        Inherits AbstractCodeStructTests

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes1() As Task
            Dim code =
<Code>
struct $$C { }
</Code>

            Await TestAttributes(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes2() As Task
            Dim code =
<Code>
using System;

[Serializable]
struct $$C { }
</Code>

            Await TestAttributes(code, IsElement("Serializable"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes3() As Task
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
struct $$C { }
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes4() As Task
            Dim code =
<Code>using System;

[Serializable, CLSCompliant(true)]
struct $$C { }
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Function
#End Region

#Region "Bases tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBase1() As Task
            Dim code =
<Code>
struct $$S { }
</Code>

            Await TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBase2() As Task
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            Await TestBases(code, IsElement("ValueType", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Function


#End Region

#Region "DataTypeKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDataTypeKind1() As Task
            Dim code =
<Code>
struct $$S { }
</Code>

            Await TestDataTypeKind(code, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDataTypeKind2() As Task
            Dim code =
<Code>
partial struct $$S { }

partial struct S { }
</Code>

            Await TestDataTypeKind(code, EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial)
        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
struct $$S { }
</Code>

            Await TestFullName(code, "S")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName2() As Task
            Dim code =
<Code>
namespace N
{
    struct $$S { }
}
</Code>

            Await TestFullName(code, "N.S")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName3() As Task
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

            Await TestFullName(code, "N.C.S")
        End Function

#End Region

#Region "ImplementedInterfaces tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestImplementedInterfaces1() As Task
            Dim code =
<Code>
struct $$S : System.IDisposable { }
</Code>

            Await TestImplementedInterfaces(code, IsElement("IDisposable", kind:=EnvDTE.vsCMElement.vsCMElementInterface))
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
struct $$S { }
</Code>

            Await TestName(code, "S")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName2() As Task
            Dim code =
<Code>
namespace N
{
    struct $$S { }
}
</Code>

            Await TestName(code, "S")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName3() As Task
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

            Await TestName(code, "S")
        End Function

#End Region

#Region "Parent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParent1() As Task
            Dim code =
<Code>
struct $$S { }
</Code>

            Await TestParent(code, IsFileCodeModel)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParent2() As Task
            Dim code =
<Code>
namespace N
{
    struct $$S { }
}
</Code>

            Await TestParent(code, IsElement("N", kind:=EnvDTE.vsCMElement.vsCMElementNamespace))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParent3() As Task
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

            Await TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Function

#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts1() As Task
            Dim code =
<Code>
struct $$S
{
}
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts2() As Task
            Dim code =
<Code>
partial struct $$S
{
}
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts3() As Task
            Dim code =
<Code>
partial struct $$S
{
}

partial struct S
{
}
</Code>

            Await TestParts(code, 2)
        End Function
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
    void Foo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
        End Function

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface1() As Task
            Dim code =
<Code>
struct $$S { }
</Code>

            Await TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", Nothing)
        End Function

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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
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

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
