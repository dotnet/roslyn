' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus

    Public Class VisualBasicContainedLanguageCodeSupportTests
        Inherits AbstractContainedLanguageCodeSupportTests

#Region "IsValid Tests"
        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_1()
            AssertValidId("field")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_Escaped()
            AssertValidId("[field]")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_EscapedKeyword()
            AssertValidId("[Class]")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_ContainsNumbers()
            AssertValidId("abc123")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_Keyword()
            AssertNotValidId("Class")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_StartsWithNumber()
            AssertNotValidId("123abc")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_Punctuation()
            AssertNotValidId("abc.abc")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_TypeChar()
            AssertValidId("abc$")
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestIsValidId_TypeCharInMiddle()
            AssertNotValidId("abc$abc")
        End Sub
#End Region

#Region "GetBaseClassName Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetBaseClassName_NonexistingClass()
            Dim code As String = <text>Class c
End Class</text>.Value
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.False(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "A",
                    CancellationToken.None, baseClassName))
                Assert.Null(baseClassName)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetBaseClassName_DerivedFromObject()
            Dim code As String = <text>Class C
End Class</text>.Value
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("Object", baseClassName)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetBaseClassName_DerivedFromFrameworkType()
            Dim code As String = <text>
Imports System
Class C
    Inherits Exception
End Class</text>.Value
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("System.Exception", baseClassName)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetBaseClassName_DerivedFromUserDefinedType()
            Dim code As String = <text>
Class B
End Class
Class C
    Inherits B
End Class</text>.Value
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("B", baseClassName)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetBaseClassName_FullyQualifiedNames()
            Dim code As String = <text>
Namespace N
Class B
End Class
Class C
    Inherits B
End Class
End Namespace</text>.Value
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "N.C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("N.B", baseClassName)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetBaseClassName_MinimallyQualifiedNames()
            Dim code As String = <text>
Namespace N
Class B
End Class
Class C
    Inherits B
End Class
End Namespace</text>.Value
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "N.C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("N.B", baseClassName)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetBaseClassName_EscapedKeyword()
            Dim code As String = <text>
Class [Class]
End Class
Class Derived
    Inherits [Class]
End Class
</text>.Value
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "Derived",
                    CancellationToken.None, baseClassName))
                Assert.Equal("[Class]", baseClassName)
            End Using
        End Sub
#End Region

#Region "CreateUniqueEventName Tests"

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestCreateUniqueEventName_ButtonClick()
            Dim code As String = <text>
Public Partial Class _Default
	Inherits System.Web.UI.Page
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class
</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventName = ContainedLanguageCodeSupport.CreateUniqueEventName(
                    document:=document,
                    className:="_Default",
                    objectName:="Button1",
                    nameOfEvent:="Click",
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click", eventName)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestCreateUniqueEventName_NameCollisionWithEventHandler()
            Dim code As String = <text>
Public Partial Class _Default
	Inherits System.Web.UI.Page
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub

	Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

	End Sub
End Class
</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventName = ContainedLanguageCodeSupport.CreateUniqueEventName(
                    document:=document,
                    className:="_Default",
                    objectName:="Button1",
                    nameOfEvent:="Click",
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click1", eventName)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestCreateUniqueEventName_NameCollisionWithOtherMembers()
            Dim code As String = <text>
Public Partial Class _Default
	Inherits System.Web.UI.Page

    Public Property Button1_Click As String

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventName = ContainedLanguageCodeSupport.CreateUniqueEventName(
                    document:=document,
                    className:="_Default",
                    objectName:="Button1",
                    nameOfEvent:="Click",
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click1", eventName)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestCreateUniqueEventName_NameCollisionFromPartialClass()
            Dim code As String = <text>
Public Partial Class _Default
	Inherits System.Web.UI.Page
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class

Public Partial Class _Default
	Public Property Button1_Click As String
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventName = ContainedLanguageCodeSupport.CreateUniqueEventName(
                    document:=document,
                    className:="_Default",
                    objectName:="Button1",
                    nameOfEvent:="Click",
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click1", eventName)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestCreateUniqueEventName_NameCollisionFromBaseClass()
            Dim code As String = <text>
Public Partial Class _Default
	Inherits MyBaseClass
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class

Public Class MyBaseClass
	Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventName = ContainedLanguageCodeSupport.CreateUniqueEventName(
                    document:=document,
                    className:="_Default",
                    objectName:="Button1",
                    nameOfEvent:="Click",
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click1", eventName)
            End Using
        End Sub

#End Region

#Region "GetCompatibleEventHandlers"

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetCompatibleEventHandlers_EventDoesntExist()
            Dim code As String = <text>
Imports System
Public Class Button
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Assert.Throws(Of InvalidOperationException)(
                    Sub()
                        ContainedLanguageCodeSupport.GetCompatibleEventHandlers(
                            document:=document,
                            className:="_Default",
                            objectTypeName:="Button",
                            nameOfEvent:="Click",
                            cancellationToken:=Nothing)
                    End Sub)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetCompatibleEventHandlers_ObjTypeNameIsWrong()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Assert.Throws(Of InvalidOperationException)(
                    Sub()
                        ContainedLanguageCodeSupport.GetCompatibleEventHandlers(
                            document:=document,
                            className:="_Default",
                            objectTypeName:="CheckBox",
                            nameOfEvent:="Click",
                            cancellationToken:=Nothing)
                    End Sub)
            End Using
        End Sub

        ' To Do: Investigate - this feels wrong. when Handles Clause exists
        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetCompatibleEventHandlers_MatchExists()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventHandlers = ContainedLanguageCodeSupport.GetCompatibleEventHandlers(
                    document:=document,
                    className:="_Default",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    cancellationToken:=Nothing)

                Assert.Equal(1, eventHandlers.Count())
                Assert.Equal("Page_Load", eventHandlers.Single().Item1)
                Assert.Equal("Page_Load(Object,System.EventArgs)", eventHandlers.Single().Item2)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetCompatibleEventHandlers_MatchesExist()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub

	Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventHandlers = ContainedLanguageCodeSupport.GetCompatibleEventHandlers(
                    document:=document,
                    className:="_Default",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    cancellationToken:=Nothing)

                Assert.Equal(2, eventHandlers.Count())
                ' It has to be page_load and button click, but are they always ordered in the same way?
            End Using
        End Sub

        ' add tests for CompatibleSignatureToDelegate (#params, return type)
#End Region

#Region "GetEventHandlerMemberId"

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetEventHandlerMemberId_HandlerExists()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub

	Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventHandlerId = ContainedLanguageCodeSupport.GetEventHandlerMemberId(
                    document:=document,
                    className:="_Default",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    eventHandlerName:="Button1_Click",
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click(Object,System.EventArgs)", eventHandlerId)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetEventHandlerMemberId_CantFindHandler()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventHandlerId = ContainedLanguageCodeSupport.GetEventHandlerMemberId(
                    document:=document,
                    className:="_Default",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    eventHandlerName:="Button1_Click",
                    cancellationToken:=Nothing)

                Assert.Equal(Nothing, eventHandlerId)
            End Using
        End Sub

#End Region

#Region "EnsureEventHandler"

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestEnsureEventHandler_HandlerExists()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs)

	End Sub

	Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)

                Dim eventHandlerIdTextPosition = ContainedLanguageCodeSupport.EnsureEventHandler(
                    thisDocument:=document,
                    targetDocument:=document,
                    className:="_Default",
                    objectName:="Button1",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    eventHandlerName:="Button1_Click",
                    itemidInsertionPoint:=0,
                    useHandlesClause:=True,
                    additionalFormattingRule:=LineAdjustmentFormattingRule.Instance,
                    cancellationToken:=Nothing)

                ' Since a valid handler exists, item2 and item3 of the tuple returned must be nothing
                Assert.Equal("Button1_Click(Object,System.EventArgs)", eventHandlerIdTextPosition.Item1)
                Assert.Equal(Nothing, eventHandlerIdTextPosition.Item2)
                Assert.Equal(New TextSpan(), eventHandlerIdTextPosition.Item3)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestEnsureEventHandler_GenerateNewHandler()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs)

	End Sub
End Class</text>.NormalizedValue

            Dim generatedCode As String = <text>
Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

End Sub</text>.NormalizedValue


            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)

                Dim eventHandlerIdTextPosition = ContainedLanguageCodeSupport.EnsureEventHandler(
                    thisDocument:=document,
                    targetDocument:=document,
                    className:="_Default",
                    objectName:="Button1",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    eventHandlerName:="Button1_Click",
                    itemidInsertionPoint:=0,
                    useHandlesClause:=True,
                    additionalFormattingRule:=LineAdjustmentFormattingRule.Instance,
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click(Object,System.EventArgs)", eventHandlerIdTextPosition.Item1)
                TokenUtilities.AssertTokensEqual(generatedCode, eventHandlerIdTextPosition.Item2, Language)
                Assert.Equal(New TextSpan With {.iStartLine = 12, .iEndLine = 12}, eventHandlerIdTextPosition.Item3)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(850035, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850035")>
        Public Sub TestEnsureEventHandler_WithHandlesAndNullObjectName()
            Dim code As String = "
Imports System

Namespace System.Web.UI
    Public Class Page
        Public Event Load as EventHandler
    End Class
End Namespace

Public Class _Default
    Inherits System.Web.UI.Page

End Class"

            Dim generatedCode As String = "
Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

End Sub"

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)

                Dim eventHandlerIdTextPosition = ContainedLanguageCodeSupport.EnsureEventHandler(
                    thisDocument:=document,
                    targetDocument:=document,
                    className:="_Default",
                    objectName:=Nothing,
                    objectTypeName:="System.Web.UI.Page",
                    nameOfEvent:="Load",
                    eventHandlerName:="Page_Load",
                    itemidInsertionPoint:=0,
                    useHandlesClause:=True,
                    additionalFormattingRule:=LineAdjustmentFormattingRule.Instance,
                    cancellationToken:=Nothing)

                Assert.Equal("Page_Load(Object,System.EventArgs)", eventHandlerIdTextPosition.Item1)
                TokenUtilities.AssertTokensEqual(generatedCode, eventHandlerIdTextPosition.Item2, Language)
                Assert.Equal(New TextSpan With {.iStartLine = 12, .iEndLine = 12}, eventHandlerIdTextPosition.Item3)
            End Using
        End Sub
#End Region

#Region "GetMemberNavigationPoint"
        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetMemberNavigationPoint()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub

	Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

	End Sub
End Class</text>.Value

            ' Expect the cursor to be inside the method body of Button1_Click, line 14 column 8
            Dim expectedSpan As New Microsoft.VisualStudio.TextManager.Interop.TextSpan() With
            {
                .iStartLine = 14,
                .iStartIndex = 8,
                .iEndLine = 14,
                .iEndIndex = 8
            }

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim targetDocument As Document = Nothing

                Dim actualSpan As TextSpan = Nothing
                If Not ContainedLanguageCodeSupport.TryGetMemberNavigationPoint(
                    thisDocument:=document,
                    className:="_Default",
                    uniqueMemberID:="Button1_Click(Object,System.EventArgs)",
                    textSpan:=actualSpan,
                    targetDocument:=targetDocument,
                    cancellationToken:=Nothing) Then

                    Assert.True(False, "should have succeeded")
                End If

                Assert.Equal(expectedSpan, actualSpan)
            End Using
        End Sub
#End Region

#Region "GetMembers"
        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetMembers_EventHandlersWrongParamType()
            Dim code As String = <text>
Imports System
Public Partial Class _Default
	Protected Sub Page_Load(sender As Object, e As Object)

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim members = ContainedLanguageCodeSupport.GetMembers(
                    document:=document,
                    className:="_Default",
                    codeMemberType:=CODEMEMBERTYPE.CODEMEMBERTYPE_EVENT_HANDLERS,
                    cancellationToken:=Nothing)

                Assert.Equal(0, members.Count())
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetMembers_EventHandlersWrongParamCount()
            Dim code As String = <text>
Imports System
Public Partial Class _Default
	Protected Sub Page_Load(sender As Object)

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim members = ContainedLanguageCodeSupport.GetMembers(
                    document:=document,
                    className:="_Default",
                    codeMemberType:=CODEMEMBERTYPE.CODEMEMBERTYPE_EVENT_HANDLERS,
                    cancellationToken:=Nothing)

                Assert.Equal(0, members.Count())
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetMembers_EventHandlersWrongReturnType()
            Dim code As String = <text>
Imports System
Public Partial Class _Default
	Protected Function Page_Load(sender As Object, e As EventArgs) As Integer

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim members = ContainedLanguageCodeSupport.GetMembers(
                    document:=document,
                    className:="_Default",
                    codeMemberType:=CODEMEMBERTYPE.CODEMEMBERTYPE_EVENT_HANDLERS,
                    cancellationToken:=Nothing)

                Assert.Equal(0, members.Count())
            End Using
        End Sub

        ' To Do: Investigate, this returns the method even if handles is missing. that ok?
        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetMembers_EventHandlers()
            Dim code As String = <text>
Imports System
Public Partial Class _Default
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim members = ContainedLanguageCodeSupport.GetMembers(
                    document:=document,
                    className:="_Default",
                    codeMemberType:=CODEMEMBERTYPE.CODEMEMBERTYPE_EVENT_HANDLERS,
                    cancellationToken:=Nothing)

                Assert.Equal(1, members.Count())

                Dim userFunction = members.First()
                Assert.Equal("Page_Load", userFunction.Item1)
                Assert.Equal("Page_Load(Object,System.EventArgs)", userFunction.Item2)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetMembers_UserFunctions()
            Dim code As String = <text>
Imports System
Public Partial Class _Default
	Protected Sub Test(x as String)

	End Sub
End Class
</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim members = ContainedLanguageCodeSupport.GetMembers(
                    document:=document,
                    className:="_Default",
                    codeMemberType:=CODEMEMBERTYPE.CODEMEMBERTYPE_USER_FUNCTIONS,
                    cancellationToken:=Nothing)

                Assert.Equal(1, members.Count())

                Dim userFunction = members.First()
                Assert.Equal("Test", userFunction.Item1)
                Assert.Equal("Test(String)", userFunction.Item2)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestGetMembers_Events()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim members = ContainedLanguageCodeSupport.GetMembers(
                    document:=document,
                    className:="Button",
                    codeMemberType:=CODEMEMBERTYPE.CODEMEMBERTYPE_EVENTS,
                    cancellationToken:=Nothing)

                Assert.Equal(1, members.Count())

                Dim userFunction = members.First()
                Assert.Equal("Click", userFunction.Item1)
                Assert.Equal("Click(EVENT)", userFunction.Item2)
            End Using
        End Sub
#End Region

#Region "OnRenamed (TryRenameElement)"

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestTryRenameElement_ResolvableMembers()
            Dim code As String = <text>
Imports System
Public Partial Class _Default
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim renameSucceeded = ContainedLanguageCodeSupport.TryRenameElement(
                    document:=document,
                    clrt:=ContainedLanguageRenameType.CLRT_CLASSMEMBER,
                    oldFullyQualifiedName:="_Default.Page_Load",
                    newFullyQualifiedName:="_Default.Page_Load1",
                    refactorNotifyServices:=SpecializedCollections.EmptyEnumerable(Of IRefactorNotifyService),
                    cancellationToken:=Nothing)

                Assert.True(renameSucceeded)
            End Using
        End Sub

        ' To Do: Who tests the fully qualified names and their absence?
        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestTryRenameElement_UnresolvableMembers()
            Dim code As String = <text>
Imports System
Public Partial Class _Default
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim renameSucceeded = ContainedLanguageCodeSupport.TryRenameElement(
                    document:=document,
                    clrt:=ContainedLanguageRenameType.CLRT_CLASSMEMBER,
                    oldFullyQualifiedName:="_Default.Fictional",
                    newFullyQualifiedName:="_Default.Fictional1",
                    refactorNotifyServices:=SpecializedCollections.EmptyEnumerable(Of IRefactorNotifyService),
                    cancellationToken:=Nothing)

                Assert.False(renameSucceeded)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestTryRenameElement_ResolvableClass()
            Dim code As String = <text>Public Partial Class Goo

End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim renameSucceeded = ContainedLanguageCodeSupport.TryRenameElement(
                    document:=document,
                    clrt:=ContainedLanguageRenameType.CLRT_CLASS,
                    oldFullyQualifiedName:="Goo",
                    newFullyQualifiedName:="Bar",
                    refactorNotifyServices:=SpecializedCollections.EmptyEnumerable(Of IRefactorNotifyService),
                    cancellationToken:=Nothing)

                Assert.True(renameSucceeded)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestTryRenameElement_ResolvableNamespace()
            Dim code As String = <text>Namespace Goo
End Namespace</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim renameSucceeded = ContainedLanguageCodeSupport.TryRenameElement(
                    document:=document,
                    clrt:=ContainedLanguageRenameType.CLRT_NAMESPACE,
                    oldFullyQualifiedName:="Goo",
                    newFullyQualifiedName:="Bar",
                    refactorNotifyServices:=SpecializedCollections.EmptyEnumerable(Of IRefactorNotifyService),
                    cancellationToken:=Nothing)

                Assert.True(renameSucceeded)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestTryRenameElement_Button()
            Dim code As String = <text>
Imports System
Public Class Button
	Public Event Click As EventHandler
End Class

Public Class _Default
	Private button As Button

	Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

	End Sub
End Class</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim renameSucceeded = ContainedLanguageCodeSupport.TryRenameElement(
                    document:=document,
                    clrt:=ContainedLanguageRenameType.CLRT_CLASSMEMBER,
                    oldFullyQualifiedName:="_Default.button",
                    newFullyQualifiedName:="_Default.button1",
                    refactorNotifyServices:=SpecializedCollections.EmptyEnumerable(Of IRefactorNotifyService),
                    cancellationToken:=Nothing)

                Assert.True(renameSucceeded)
            End Using
        End Sub
#End Region

        ' TODO: Does Dev10 cover more here, like conflicts with existing members?

        Protected Overrides ReadOnly Property DefaultCode As String
            Get
                Return <text>
Class C

End Class
                       </text>.Value
            End Get
        End Property

        Protected Overrides ReadOnly Property Language As String
            Get
                Return Microsoft.CodeAnalysis.LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
