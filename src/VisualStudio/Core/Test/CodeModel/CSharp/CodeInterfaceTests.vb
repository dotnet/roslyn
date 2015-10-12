' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeInterfaceTests
        Inherits AbstractCodeInterfaceTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
<Code>
interface $$I { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
internal interface $$I { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
public interface $$I { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes1()
            Dim code =
<Code>
interface $$C { }
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes2()
            Dim code =
<Code>
using System;

[Serializable]
interface $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes3()
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
interface $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes4()
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
        Public Sub Parts1()
            Dim code =
<Code>
interface $$I
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts2()
            Dim code =
<Code>
partial interface $$I
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts3()
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
        Public Sub AddAttribute1()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

#End Region

#Region "AddBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase1()
            Dim code =
<Code>
interface $$I { }
</Code>

            Dim expected =
<Code>
interface I : B { }
</Code>
            TestAddBase(code, "B", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase2()
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : A, B { }
</Code>
            TestAddBase(code, "A", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase3()
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : B, A { }
</Code>
            TestAddBase(code, "A", Type.Missing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase4()
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : B, A { }
</Code>
            TestAddBase(code, "A", -1, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase5()
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I : A, B { }
</Code>
            TestAddBase(code, "A", 0, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase6()
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
            TestAddBase(code, "B", Nothing, expected)
        End Sub

#End Region

#Region "AddEvent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddEvent1()
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

            TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddEvent2()
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
            TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler", .CreatePropertyStyleEvent = True})
        End Sub

#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction1()
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

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
        End Sub

#End Region

#Region "RemoveBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase1()
            Dim code =
<Code>
interface $$I : B { }
</Code>

            Dim expected =
<Code>
interface I { }
</Code>
            TestRemoveBase(code, "B", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase2()
            Dim code =
<Code>
interface $$I : A, B { }
</Code>

            Dim expected =
<Code>
interface I : B { }
</Code>
            TestRemoveBase(code, "A", expected)
        End Sub

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
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

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub
#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
            Dim code =
<Code>
interface $$I
{
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Namespace", "Bases", "Members", "Access", "Attributes",
                 "DocComment", "Comment", "DerivedTypes", "IsGeneric", "DataTypeKind", "Parts"}

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
