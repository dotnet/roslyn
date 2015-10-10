' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeEventTests
        Inherits AbstractCodeEventTests

#Region "GetStartPoint() tests"

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_SimpleEvent()
            Dim code =
<Code>
Class C
    Event E$$(i As Integer)
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=11, absoluteOffset:=19, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=25)))
        End Sub

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_SimpleEventWithAttributes()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Event E$$(i As Integer)
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=11, absoluteOffset:=51, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Sub

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_CustomEvent()
            Dim code =
<Code>
Class C
    Custom Event $$E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler

        RemoveHandler(value As System.EventHandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=51, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=51, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=18, absoluteOffset:=26, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=59, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=41)))
        End Sub

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_CustomEventWithAttributes()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Custom Event $$E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler

        RemoveHandler(value As System.EventHandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=83, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=83, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=58, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=9, absoluteOffset:=91, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_SimpleEvent()
            Dim code =
<Code>
Class C
    Event E$$(i As Integer)
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=26, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=26, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=26, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=26, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=12, absoluteOffset:=20, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=26, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=26, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=26, absoluteOffset:=34, lineLength:=25)))
        End Sub

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_SimpleEventWithAttributes()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Event E$$(i As Integer)
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=66, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=66, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=66, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=66, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=12, absoluteOffset:=52, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=66, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=66, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=66, lineLength:=25)))
        End Sub

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_CustomEvent()
            Dim code =
<Code>
Class C
    Custom Event $$E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler

        RemoveHandler(value As System.EventHandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=11, lineOffset:=5, absoluteOffset:=290, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=11, lineOffset:=5, absoluteOffset:=290, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=42, absoluteOffset:=50, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=42, absoluteOffset:=50, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=19, absoluteOffset:=27, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=11, lineOffset:=5, absoluteOffset:=290, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=11, lineOffset:=14, absoluteOffset:=299, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=11, lineOffset:=14, absoluteOffset:=299, lineLength:=13)))
        End Sub

        <WorkItem(639075)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_CustomEventWithAttributes()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Custom Event $$E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler

        RemoveHandler(value As System.EventHandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=12, lineOffset:=5, absoluteOffset:=322, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=12, lineOffset:=5, absoluteOffset:=322, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=42, absoluteOffset:=82, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=42, absoluteOffset:=82, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=19, absoluteOffset:=59, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=12, lineOffset:=5, absoluteOffset:=322, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=12, lineOffset:=14, absoluteOffset:=331, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=12, lineOffset:=14, absoluteOffset:=331, lineLength:=13)))
        End Sub

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
<Code>
Class C
    Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
Class C
    Private Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
Class C
    Protected Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access4()
            Dim code =
<Code>
Class C
    Protected Friend Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access5()
            Dim code =
<Code>
Class C
    Friend Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access6()
            Dim code =
<Code>
Class C
    Public Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attributes"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes_SimpleEvent()
            Dim code =
<Code>
Imports System

Class C1
    &lt;CLSCompliant(False)&gt;
    Public Event $$E1()
End Class
</Code>

            TestAttributes(code, IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes_CustomEvent()
            Dim code =
<Code>
Imports System

Class C1
    &lt;CLSCompliant(False)&gt;
    Public Custom Event $$E2 As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
     End Event
End Class
</Code>

            TestAttributes(code, IsElement("CLSCompliant"))
        End Sub

#End Region

#Region "IsPropertyStyleEvent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsPropertyStyleEvent1()
            Dim code =
<Code>
Class C
    Event $$Foo(i As Integer)
End Class
</Code>

            TestIsPropertyStyleEvent(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsPropertyStyleEvent2()
            Dim code =
<Code>
Class C
    Event E$$ As System.EventHandler
End Class
</Code>

            TestIsPropertyStyleEvent(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsPropertyStyleEvent3()
            Dim code =
<Code>
Class C
    Custom Event $$Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            TestIsPropertyStyleEvent(code, True)
        End Sub

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared1()
            Dim code =
<Code>
Class C
    Event $$Foo(i As Integer)
End Class
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared2()
            Dim code =
<Code>
Class C
    Shared Event $$Foo(i As Integer)
End Class
</Code>

            TestIsShared(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared3()
            Dim code =
<Code>
Class C
    Custom Event $$Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared4()
            Dim code =
<Code>
Class C
    Shared Custom Event $$Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            TestIsShared(code, True)
        End Sub

#End Region

#Region "Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
            Dim code =
<Code>
Class C
    Event $$Foo As System.EventHandler
End Class
</Code>

            TestName(code, "Foo")
        End Sub
#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
            Dim code =
<Code>
Class C
    Event $$E(i As Integer)
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "C.EEventHandler",
                             .AsFullName = "C.EEventHandler",
                             .CodeTypeFullName = "C.EEventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type2()
            Dim code =
<Code>
Class C
    Event $$E As System.EventHandler
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type3()
            Dim code =
<Code>
Class C
    Custom Event $$E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler

        RemoveHandler(value As System.EventHandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_SimpleEvent()
            Dim code =
<Code>
Imports System

Class C
    Public Event $$E()
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Public Event E()
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_CustomEvent()
            Dim code =
<Code>
Imports System

Class C
    Public Custom Event $$E As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
     End Event
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Public Custom Event E As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
     End Event
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_SimpleEvent_BelowDocComment()
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Public Event $$E()
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;CLSCompliant(true)&gt;
    Public Event E()
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_CustomEvent_BelowDocComment()
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Public Custom Event $$E As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
     End Event
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;CLSCompliant(true)&gt;
    Public Custom Event E As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
     End Event
End Class
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
            Dim code =
<Code>
Class C
    Event $$Foo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event Foo(i As Integer)
End Class
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
            Dim code =
<Code>
Class C
    Event $$Foo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Event Foo(i As Integer)
End Class
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared3()
            Dim code =
<Code>
Class C
    Shared Event $$Foo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Event Foo(i As Integer)
End Class
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared4()
            Dim code =
<Code>
Class C
    Shared Event $$Foo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event Foo(i As Integer)
End Class
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared5()
            Dim code =
<Code>
Class C
    Custom Event $$Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Dim expected =
<Code>
Class C
    Custom Event Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared6()
            Dim code =
<Code>
Class C
    Custom Event $$Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Custom Event Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared7()
            Dim code =
<Code>
Class C
    Shared Custom Event $$Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Custom Event Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared8()
            Dim code =
<Code>
Class C
    Shared Custom Event $$Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Dim expected =
<Code>
Class C
    Custom Event Foo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
Class C
    Event $$Foo As System.EventHandler
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event Bar As System.EventHandler
End Class
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
            Dim code =
<Code>
Class C
    Event $$E(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event E(i As Integer)
End Class
</Code>

            TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef), ThrowsArgumentException(Of EnvDTE.CodeTypeRef))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
            Dim code =
<Code>
Class C
    Event $$E(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event E As System.EventHandler
End Class
</Code>

            TestSetTypeProp(code, expected, "System.EventHandler")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType3()
            Dim code =
<Code>
Class C
    Event $$E As System.EventHandler
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event E As System.ConsoleCancelEventHandler
End Class
</Code>

            TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType4()
            Dim code =
<Code>
Class C
    Custom Event $$E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler

        RemoveHandler(value As System.EventHandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            Dim expected =
<Code>
Class C
    Custom Event E As System.ConsoleCancelEventHandler
        AddHandler(value As System.ConsoleCancelEventHandler)
        End AddHandler

        RemoveHandler(value As System.ConsoleCancelEventHandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As System.ConsoleCancelEventArgs)
        End RaiseEvent
    End Event
End Class
</Code>

            TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
