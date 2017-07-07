﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeInterfaceTests
        Inherits AbstractCodeInterfaceTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
interface $$I { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
internal interface $$I { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
public interface $$I { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes1()
            Dim code =
<Code>
interface $$C { }
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes2()
            Dim code =
<Code>
using System;

[Serializable]
interface $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes3()
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
interface $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes4()
            Dim code =
<Code>using System;

[Serializable, CLSCompliant(true)]
interface $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub
#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts1()
            Dim code =
<Code>
interface $$I
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts2()
            Dim code =
<Code>
partial interface $$I
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts3()
            Dim code =
<Code>
partial interface $$I
{
}

partial interface I
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

interface $$I { }
</Code>

            Dim expected =
<Code>
using System;

[Serializable()]
interface I { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
using System;

[Serializable]
interface $$I { }
</Code>

            Dim expected =
<Code>
using System;

[Serializable]
[CLSCompliant(true)]
interface I { }
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
interface $$I { }
</Code>

            Dim expected =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[CLSCompliant(true)]
interface I { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

#Region "AddBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase1() As Task
            Dim code =
<Code>
interface $$I { }
</Code>

            Dim expected =
<Code>
interface I : B { }
</Code>
            Await TestAddBase(code, "B", Nothing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase2() As Task
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : A, B { }
</Code>
            Await TestAddBase(code, "A", Nothing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase3() As Task
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : B, A { }
</Code>
            Await TestAddBase(code, "A", Type.Missing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase4() As Task
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : B, A { }
</Code>
            Await TestAddBase(code, "A", -1, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase5() As Task
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : A, B { }
</Code>
            Await TestAddBase(code, "A", 0, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase6() As Task
            Dim code =
<Code>
interface $$I
{
}
</Code>

            Dim expected =
<Code>
interface I : B
{
}
</Code>
            Await TestAddBase(code, "B", Nothing, expected)
        End Function

#End Region

#Region "AddEvent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddEvent1() As Task
            Dim code =
<Code>
interface $$I
{
}
</Code>

            Dim expected =
<Code>
interface I
{
    event System.EventHandler E;
}
</Code>

            Await TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddEvent2() As Task
            Dim code =
<Code>
interface $$I
{
}
</Code>

            Dim expected =
<Code>
interface I
{
    event System.EventHandler E;
}
</Code>

            ' Note: C# Code Model apparently ignore CreatePropertyStyleEvent for interfaces in Dev10.
            Await TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler", .CreatePropertyStyleEvent = True})
        End Function

#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction1() As Task
            Dim code =
<Code>
interface $$I { }
</Code>

            Dim expected =
<Code>
interface I
{
    void Foo();
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
        End Function

#End Region

#Region "RemoveBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase1() As Task
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I { }
</Code>
            Await TestRemoveBase(code, "B", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase2() As Task
            Dim code =
<Code>
interface $$I : A, B { }
</Code>

            Dim expected =
<Code>
interface I : B { }
</Code>
            Await TestRemoveBase(code, "A", expected)
        End Function

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
interface $$Foo
{
}
</Code>

            Dim expected =
<Code>
interface Bar
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
interface $$I
{
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeInterface2)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
