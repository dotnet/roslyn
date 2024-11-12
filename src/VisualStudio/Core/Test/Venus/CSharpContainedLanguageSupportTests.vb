' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus
    <Trait(Traits.Feature, Traits.Features.Venus)>
    Public Class CSharpContainedLanguageCodeSupportTests
        Inherits AbstractContainedLanguageCodeSupportTests

#Region "IsValidId Tests"

        <WpfFact>
        Public Sub TestIsValidId_1()
            AssertValidId("field")
        End Sub

        <WpfFact>
        Public Sub TestIsValidId_Escaped()
            AssertValidId("@field")
        End Sub

        <WpfFact>
        Public Sub TestIsValidId_EscapedKeyword()
            AssertValidId("@class")
        End Sub

        <WpfFact>
        Public Sub TestIsValidId_ContainsNumbers()
            AssertValidId("abc123")
        End Sub

        <WpfFact>
        Public Sub TestIsValidId_Keyword()
            AssertNotValidId("class")
        End Sub

        <WpfFact>
        Public Sub TestIsValidId_StartsWithNumber()
            AssertNotValidId("123abc")
        End Sub

        <WpfFact>
        Public Sub TestIsValidId_Punctuation()
            AssertNotValidId("abc.abc")
        End Sub

        ' TODO: Does Dev10 cover more here, like conflicts with existing members?
#End Region

#Region "GetBaseClassName Tests"

        <WpfFact>
        Public Sub TestGetBaseClassName_NonexistingClass()
            Dim code As String = "class C { }"
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.False(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "A",
                    CancellationToken.None, baseClassName))
                Assert.Null(baseClassName)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetBaseClassName_DerivedFromObject()
            Dim code As String = "class C { }"
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("object", baseClassName)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetBaseClassName_DerivedFromFrameworkType()
            Dim code As String = "class C : Exception { }"
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("Exception", baseClassName)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetBaseClassName_DerivedFromUserDefinedType()
            Dim code As String = "class B { } class C : B { }"
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("B", baseClassName)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetBaseClassName_FullyQualifiedNames()
            Dim code As String = "namespace N { class B { } class C : B { } }"
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "N.C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("N.B", baseClassName)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetBaseClassName_MinimallyQualifiedNames()
            Dim code As String = "namespace N { class B { } class C : B { } }"
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "N.C",
                    CancellationToken.None, baseClassName))
                Assert.Equal("N.B", baseClassName)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetBaseClassName_EscapedKeyword()
            Dim code As String = "class @class { } class Derived : @class { }"
            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim baseClassName As String = Nothing
                Assert.True(ContainedLanguageCodeSupport.TryGetBaseClassName(document, "Derived",
                    CancellationToken.None, baseClassName))
                Assert.Equal("@class", baseClassName)
            End Using
        End Sub
#End Region

#Region "CreateUniqueEventName Tests"

        <WpfFact>
        Public Sub TestCreateUniqueEventName_ButtonClick()
            Dim code As String = <text>
public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestCreateUniqueEventName_NameCollisionWithEventHandler()
            Dim code As String = <text>
public class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void Button1_Click(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestCreateUniqueEventName_NameCollisionWithOtherMembers()
            Dim code As String = <text>
public class _Default : System.Web.UI.Page
{
    public int Button1_Click { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestCreateUniqueEventName_NameCollisionFromPartialClass()
            Dim code As String = <text>
public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}
public partial class _Default
{
    public int Button1_Click { get; set; }
}</text>.Value

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

        <WpfFact>
        Public Sub TestCreateUniqueEventName_NameCollisionFromBaseClass()
            Dim code As String = <text>
public class _Default : MyBaseClass
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}
public class MyBaseClass
{
    protected void Button1_Click(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestGetCompatibleEventHandlers_EventDoesntExist()
            Dim code As String = <text>
using System;
public class Button
{
}

public class _Default
{
    Button button;

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestGetCompatibleEventHandlers_ObjTypeNameIsWrong()
            Dim code As String = <text>
using System;
namespace Test
{
    public class Button
    {
        public event EventHandler Click;
    }

    public class _Default
    {
        Button button;

        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Assert.Throws(Of InvalidOperationException)(
                    Sub()
                        ContainedLanguageCodeSupport.GetCompatibleEventHandlers(
                            document:=document,
                            className:="_Default",
                            objectTypeName:="Form",
                            nameOfEvent:="Click",
                            cancellationToken:=Nothing)
                    End Sub)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetCompatibleEventHandlers_MatchExists()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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
                Assert.Equal("Page_Load(object,System.EventArgs)", eventHandlers.Single().Item2)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetCompatibleEventHandlers_MatchesExist()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void Button1_Click(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestGetEventHandlerMemberId_HandlerExists()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void Button1_Click(object sender, EventArgs e)
    {

    }
}</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim eventHandlerId = ContainedLanguageCodeSupport.GetEventHandlerMemberId(
                    document:=document,
                    className:="_Default",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    eventHandlerName:="Button1_Click",
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click(object,System.EventArgs)", eventHandlerId)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetEventHandlerMemberId_CantFindHandler()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;
}</text>.Value

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

        ' TODO: log a bug, Kevin doesn't use uint itemidInsertionPoint thats sent in.
        <WpfFact>
        Public Sub TestEnsureEventHandler_HandlerExists()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void Button1_Click(object sender, EventArgs e)
    {

    }
}</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)

                Dim eventHandlerIdTextPosition = ContainedLanguageCodeSupport.EnsureEventHandler(
                    thisDocument:=document,
                    targetDocument:=document,
                    className:="_Default",
                    objectName:="",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    eventHandlerName:="Button1_Click",
                    itemidInsertionPoint:=0,
                    useHandlesClause:=False,
                    additionalFormattingRule:=BlankLineInGeneratedMethodFormattingRule.Instance,
                    cancellationToken:=Nothing)

                ' Since a valid handler exists, item2 and item3 of the tuple returned must be nothing
                Assert.Equal("Button1_Click(object,System.EventArgs)", eventHandlerIdTextPosition.Item1)
                Assert.Equal(Nothing, eventHandlerIdTextPosition.Item2)
                Assert.Equal(New TextSpan(), eventHandlerIdTextPosition.Item3)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestEnsureEventHandler_GenerateNewHandler()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.NormalizedValue

            Dim generatedCode As String = <text>
protected void Button1_Click(object sender, EventArgs e)
{
                  
}
</text>.NormalizedValue

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)

                Dim eventHandlerIdTextPosition = ContainedLanguageCodeSupport.EnsureEventHandler(
                    thisDocument:=document,
                    targetDocument:=document,
                    className:="_Default",
                    objectName:="",
                    objectTypeName:="Button",
                    nameOfEvent:="Click",
                    eventHandlerName:="Button1_Click",
                    itemidInsertionPoint:=0,
                    useHandlesClause:=False,
                    additionalFormattingRule:=BlankLineInGeneratedMethodFormattingRule.Instance,
                    cancellationToken:=Nothing)

                Assert.Equal("Button1_Click(object,System.EventArgs)", eventHandlerIdTextPosition.Item1)
                TokenUtilities.AssertTokensEqual(generatedCode, eventHandlerIdTextPosition.Item2, Language)
                Assert.Equal(New TextSpan With {.iStartLine = 15, .iEndLine = 15}, eventHandlerIdTextPosition.Item3)
            End Using
        End Sub
#End Region

#Region "GetMemberNavigationPoint"
        <WpfFact>
        Public Sub TestGetMemberNavigationPoint()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void Button1_Click(object sender, EventArgs e)
    {

    }
}</text>.Value

            ' Expect the cursor to be inside the method body of Button1_Click, line 18 column 8
            Dim expectedSpan As New Microsoft.VisualStudio.TextManager.Interop.TextSpan() With
            {
                .iStartLine = 18,
                .iStartIndex = 8,
                .iEndLine = 18,
                .iEndIndex = 8
            }

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim targetDocument As Document = Nothing

                Dim actualSpan As TextSpan = Nothing
                If Not ContainedLanguageCodeSupport.TryGetMemberNavigationPoint(
                    thisDocument:=document,
                    className:="_Default",
                    uniqueMemberID:="Button1_Click(object,System.EventArgs)",
                    textSpan:=actualSpan,
                    targetDocument:=targetDocument,
                    cancellationToken:=Nothing) Then

                    Assert.True(False, "Should have succeeded")
                End If

                Assert.Equal(expectedSpan, actualSpan)
            End Using
        End Sub
#End Region

#Region "GetMembers"
        <WpfFact>
        Public Sub TestGetMembers_EventHandlersWrongParamType()
            Dim code As String = <text>
using System;
public partial class _Default
{
    protected void Page_Load(object sender, object e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestGetMembers_EventHandlersWrongParamCount()
            Dim code As String = <text>
using System;
public partial class _Default
{
    protected void Page_Load(object sender)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestGetMembers_EventHandlersWrongReturnType()
            Dim code As String = <text>
using System;
public partial class _Default
{
    protected int Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestGetMembers_EventHandlers()
            Dim code As String = <text>
using System;
public partial class _Default
{
    int a;
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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
                Assert.Equal("Page_Load(object,System.EventArgs)", userFunction.Item2)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetMembers_UserFunctions()
            Dim code As String = <text>
using System;
public partial class _Default
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

            Using workspace = GetWorkspace(code)
                Dim document = GetDocument(workspace)
                Dim members = ContainedLanguageCodeSupport.GetMembers(
                    document:=document,
                    className:="_Default",
                    codeMemberType:=CODEMEMBERTYPE.CODEMEMBERTYPE_USER_FUNCTIONS,
                    cancellationToken:=Nothing)

                Assert.Equal(1, members.Count())

                Dim userFunction = members.First()
                Assert.Equal("Page_Load", userFunction.Item1)
                Assert.Equal("Page_Load(object,System.EventArgs)", userFunction.Item2)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetMembers_Events()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}</text>.Value

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

        <WpfFact>
        Public Sub TestTryRenameElement_ResolvableMembers()
            Dim code As String = <text>
using System;
public partial class _Default
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        ' TODO: Who tests the fully qualified names and their absence?
        <WpfFact>
        Public Sub TestTryRenameElement_UnresolvableMembers()
            Dim code As String = <text>
using System;
public partial class _Default
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        <WpfFact>
        Public Sub TestTryRenameElement_ResolvableClass()
            Dim code As String = <text>public partial class Goo { }</text>.Value

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

        <WpfFact>
        Public Sub TestTryRenameElement_ResolvableNamespace()
            Dim code As String = <text>namespace Goo { }</text>.Value

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

        <WpfFact>
        Public Sub TestTryRenameElement_Button()
            Dim code As String = <text>
using System;
public class Button
{
    public event EventHandler Click;
}

public class _Default
{
    Button button;

    protected void Button_Click(object sender, EventArgs e)
    {

    }
}</text>.Value

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

        Protected Overrides ReadOnly Property Language As String
            Get
                Return "C#"
            End Get
        End Property

        Protected Overrides ReadOnly Property DefaultCode As String
            Get
                Return "class C { }"
            End Get
        End Property
    End Class
End Namespace
