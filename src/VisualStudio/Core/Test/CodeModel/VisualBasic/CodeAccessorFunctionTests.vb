' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeAccessorFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "GetStartPoint() Tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_PropertyGet() As Task
            Dim code =
<Code>
Public Class C1
    Public Property P As String
        $$Get
        End Get
        Set(value As String)
        End Set
    End Property
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=61, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=61, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=57, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=57, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=37, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=9, absoluteOffset:=69, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=57, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=57, lineLength:=11)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_PropertySet() As Task
            Dim code =
<Code>
Public Class C1
    Public Property P As String
        Get
        End Get
        $$Set(value As String)
        End Set
    End Property
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=106, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=106, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=9, absoluteOffset:=85, lineLength:=28)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=9, absoluteOffset:=85, lineLength:=28)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=37, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=6, lineOffset:=9, absoluteOffset:=114, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=5, lineOffset:=9, absoluteOffset:=85, lineLength:=28)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=9, absoluteOffset:=85, lineLength:=28)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_EventAddHandler() As Task
            Dim code =
<Code>
Imports System

Public Class C1
   Public Custom Event E1 As EventHandler
      $$AddHandler(ByVal value As EventHandler)
      End AddHandler
      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler
      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent
   End Event
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=121, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=121, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=7, absoluteOffset:=81, lineLength:=45)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=7, absoluteOffset:=81, lineLength:=45)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=4, lineOffset:=24, absoluteOffset:=56, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=6, lineOffset:=7, absoluteOffset:=127, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=5, lineOffset:=7, absoluteOffset:=81, lineLength:=45)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=7, absoluteOffset:=81, lineLength:=45)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_EventRemoveHandler() As Task
            Dim code =
<Code>
Imports System

Public Class C1
   Public Custom Event E1 As EventHandler
      AddHandler(ByVal value As EventHandler)
      End AddHandler
      $$RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler
      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent
   End Event
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=8, lineOffset:=1, absoluteOffset:=191, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=8, lineOffset:=1, absoluteOffset:=191, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=7, lineOffset:=7, absoluteOffset:=148, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=7, lineOffset:=7, absoluteOffset:=148, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=4, lineOffset:=24, absoluteOffset:=56, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=7, absoluteOffset:=197, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=7, lineOffset:=7, absoluteOffset:=148, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=7, lineOffset:=7, absoluteOffset:=148, lineLength:=48)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_EventRaiseEvent() As Task
            Dim code =
<Code>
Imports System

Public Class C1
   Public Custom Event E1 As EventHandler
      AddHandler(ByVal value As EventHandler)
      End AddHandler
      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler
      $$RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent
   End Event
End Class
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=10, lineOffset:=1, absoluteOffset:=278, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=10, lineOffset:=1, absoluteOffset:=278, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=9, lineOffset:=7, absoluteOffset:=221, lineLength:=62)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=9, lineOffset:=7, absoluteOffset:=221, lineLength:=62)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=4, lineOffset:=24, absoluteOffset:=56, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=10, lineOffset:=7, absoluteOffset:=284, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=9, lineOffset:=7, absoluteOffset:=221, lineLength:=62)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=9, lineOffset:=7, absoluteOffset:=221, lineLength:=62)))
        End Function

#End Region

#Region "GetEndPoint() Tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_PropertyGet() As Task
            Dim code =
<Code>
Public Class C1
    Public Property P As String
        $$Get
        End Get
        Set(value As String)
        End Set
    End Property
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=9, absoluteOffset:=69, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=9, absoluteOffset:=69, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=12, absoluteOffset:=60, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=12, absoluteOffset:=60, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=38, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=9, absoluteOffset:=69, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=4, lineOffset:=16, absoluteOffset:=76, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=4, lineOffset:=16, absoluteOffset:=76, lineLength:=15)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_PropertySet() As Task
            Dim code =
<Code>
Public Class C1
    Public Property P As String
        Get
        End Get
        $$Set(value As String)
        End Set
    End Property
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=9, absoluteOffset:=114, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=6, lineOffset:=9, absoluteOffset:=114, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=29, absoluteOffset:=105, lineLength:=28)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=29, absoluteOffset:=105, lineLength:=28)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=38, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=6, lineOffset:=9, absoluteOffset:=114, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=6, lineOffset:=16, absoluteOffset:=121, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=6, lineOffset:=16, absoluteOffset:=121, lineLength:=15)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_EventAddHandler() As Task
            Dim code =
<Code>
Imports System

Public Class C1
   Public Custom Event E1 As EventHandler
      $$AddHandler(ByVal value As EventHandler)
      End AddHandler
      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler
      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent
   End Event
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=7, absoluteOffset:=127, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=6, lineOffset:=7, absoluteOffset:=127, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=5, lineOffset:=46, absoluteOffset:=120, lineLength:=45)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=5, lineOffset:=46, absoluteOffset:=120, lineLength:=45)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=4, lineOffset:=26, absoluteOffset:=58, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=6, lineOffset:=7, absoluteOffset:=127, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=6, lineOffset:=21, absoluteOffset:=141, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=6, lineOffset:=21, absoluteOffset:=141, lineLength:=20)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_EventRemoveHandler() As Task
            Dim code =
<Code>
Imports System

Public Class C1
   Public Custom Event E1 As EventHandler
      AddHandler(ByVal value As EventHandler)
      End AddHandler
      $$RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler
      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent
   End Event
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=8, lineOffset:=7, absoluteOffset:=197, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=8, lineOffset:=7, absoluteOffset:=197, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=7, lineOffset:=49, absoluteOffset:=190, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=7, lineOffset:=49, absoluteOffset:=190, lineLength:=48)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=4, lineOffset:=26, absoluteOffset:=58, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=7, absoluteOffset:=197, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=8, lineOffset:=24, absoluteOffset:=214, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=8, lineOffset:=24, absoluteOffset:=214, lineLength:=23)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_EventRaiseEvent() As Task
            Dim code =
<Code>
Imports System

Public Class C1
   Public Custom Event E1 As EventHandler
      AddHandler(ByVal value As EventHandler)
      End AddHandler
      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler
      $$RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent
   End Event
End Class
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=10, lineOffset:=7, absoluteOffset:=284, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=10, lineOffset:=7, absoluteOffset:=284, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=9, lineOffset:=63, absoluteOffset:=277, lineLength:=62)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=9, lineOffset:=63, absoluteOffset:=277, lineLength:=62)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=4, lineOffset:=26, absoluteOffset:=58, lineLength:=41)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=10, lineOffset:=7, absoluteOffset:=284, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=10, lineOffset:=21, absoluteOffset:=298, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=10, lineOffset:=21, absoluteOffset:=298, lineLength:=20)))
        End Function

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
    <Code>
Class C
    Public Property P As Integer
        Get
            Return 0
        End Get
        $$Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
    <Code>
Class C
    Public Property P As Integer
        Get
            Return 0
        End Get
        Private $$Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

#End Region

#Region "FunctionKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Get() As Task
            Dim code =
<Code>
Public Class C1
    Public Property P As String
        $$Get
        End Get
        Set(value As String)
        End Set
    End Property
End Class
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionPropertyGet)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_Set() As Task
            Dim code =
<Code>
Public Class C1
    Public Property P As String
        Get
        End Get
        $$Set(value As String)
        End Set
    End Property
End Class
</Code>

            Await TestFunctionKind(code, EnvDTE.vsCMFunction.vsCMFunctionPropertySet)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_AddHandler() As Task
            Dim code =
<Code>
Imports System

Public Class C1

   Public Custom Event E1 As EventHandler

      $$AddHandler(ByVal value As EventHandler)
      End AddHandler

      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler

      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent

   End Event

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE80.vsCMFunction2.vsCMFunctionAddHandler)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_RemoveHandler() As Task
            Dim code =
<Code>
Imports System

Public Class C1

   Public Custom Event E1 As EventHandler

      AddHandler(ByVal value As EventHandler)
      End AddHandler

      $$RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler

      RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent

   End Event

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE80.vsCMFunction2.vsCMFunctionRemoveHandler)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFunctionKind_RaiseEvent() As Task
            Dim code =
<Code>
Imports System

Public Class C1

   Public Custom Event E1 As EventHandler

      AddHandler(ByVal value As EventHandler)
      End AddHandler

      RemoveHandler(ByVal value As EventHandler)
      End RemoveHandler

      $$RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
      End RaiseEvent

   End Event

End Clas
</Code>

            Await TestFunctionKind(code, EnvDTE80.vsCMFunction2.vsCMFunctionRaiseEvent)
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace


