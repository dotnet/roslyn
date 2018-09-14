' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeEventTests
        Inherits AbstractCodeEventTests

#Region "GetStartPoint() tests"

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_SimpleEvent()
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

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_SimpleEventWithAttributes()
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

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_CustomEvent()
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

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_CustomEventWithAttributes()
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

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_SimpleEvent()
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

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_SimpleEventWithAttributes()
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

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_CustomEvent()
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

        <WorkItem(639075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639075")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_CustomEventWithAttributes()
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
        Public Sub TestAccess1()
            Dim code =
<Code>
Class C
    Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
Class C
    Private Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
Class C
    Protected Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
<Code>
Class C
    Protected Friend Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
<Code>
Class C
    Friend Event $$E As System.EventHandler
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
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
        Public Sub TestAttributes_SimpleEvent()
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
        Public Sub TestAttributes_CustomEvent()
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
        Public Sub TestIsPropertyStyleEvent1()
            Dim code =
<Code>
Class C
    Event $$Goo(i As Integer)
End Class
</Code>

            TestIsPropertyStyleEvent(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsPropertyStyleEvent2()
            Dim code =
<Code>
Class C
    Event E$$ As System.EventHandler
End Class
</Code>

            TestIsPropertyStyleEvent(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsPropertyStyleEvent3()
            Dim code =
<Code>
Class C
    Custom Event $$Goo As System.EventHandler
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
        Public Sub TestIsShared1()
            Dim code =
<Code>
Class C
    Event $$Goo(i As Integer)
End Class
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared2()
            Dim code =
<Code>
Class C
    Shared Event $$Goo(i As Integer)
End Class
</Code>

            TestIsShared(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared3()
            Dim code =
<Code>
Class C
    Custom Event $$Goo As System.EventHandler
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
        Public Sub TestIsShared4()
            Dim code =
<Code>
Class C
    Shared Custom Event $$Goo As System.EventHandler
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
        Public Sub TestName1()
            Dim code =
<Code>
Class C
    Event $$Goo As System.EventHandler
End Class
</Code>

            TestName(code, "Goo")
        End Sub
#End Region

#Region "OverrideKind tests"

        <WorkItem(150349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_SimpleEvent()
            Dim code =
<Code>
Class C
    Event $$E()
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

        <WorkItem(150349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_CustomEvent()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
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
        Public Sub TestType2()
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
        Public Sub TestType3()
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
        Public Async Function TestAddAttribute_SimpleEvent() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_CustomEvent() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_SimpleEvent_BelowDocComment() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_CustomEvent_BelowDocComment() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
Class C
    Event $$Goo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event Goo(i As Integer)
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
Class C
    Event $$Goo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Event Goo(i As Integer)
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared3() As Task
            Dim code =
<Code>
Class C
    Shared Event $$Goo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared Event Goo(i As Integer)
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared4() As Task
            Dim code =
<Code>
Class C
    Shared Event $$Goo(i As Integer)
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event Goo(i As Integer)
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared5() As Task
            Dim code =
<Code>
Class C
    Custom Event $$Goo As System.EventHandler
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
    Custom Event Goo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared6() As Task
            Dim code =
<Code>
Class C
    Custom Event $$Goo As System.EventHandler
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
    Shared Custom Event Goo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared7() As Task
            Dim code =
<Code>
Class C
    Shared Custom Event $$Goo As System.EventHandler
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
    Shared Custom Event Goo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared8() As Task
            Dim code =
<Code>
Class C
    Shared Custom Event $$Goo As System.EventHandler
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
    Custom Event Goo As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler

        RemoveHandler(value As System.EventHandler)

        End RemoveHandler

        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
Class C
    Event $$Goo As System.EventHandler
End Class
</Code>

            Dim expected =
<Code>
Class C
    Event Bar As System.EventHandler
End Class
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
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

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef), ThrowsArgumentException(Of EnvDTE.CodeTypeRef))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
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

            Await TestSetTypeProp(code, expected, "System.EventHandler")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
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

            Await TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
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

            Await TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
