' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Editor.Implementation.Organizing
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities
Imports Xunit
Imports ParseOptions = Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Organizing
    Public Class OrganizeTypeDeclarationTests
        Inherits AbstractOrganizerTests

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestFieldsWithoutInitializers1()
            Dim initial =
    <element>class C 
    dim A as Integer
    dim B as Integer
    dim C as Integer
end class</element>

            Dim final =
    <element>class C 
    dim A as Integer
    dim B as Integer
    dim C as Integer
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestFieldsWithoutInitializers2()

            Dim initial =
    <element>class C 
    dim C as Integer
    dim B as Integer
    dim A as Integer
end class</element>

            Dim final =
    <element>class C 
    dim A as Integer
    dim B as Integer
    dim C as Integer
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestFieldsWithInitializers1()

            Dim initial =
    <element>class C 
    dim C as Integer = 0
    dim B as Integer
    dim A as Integer
end class</element>

            Dim final =
    <element>class C 
    dim A as Integer
    dim B as Integer
    dim C as Integer = 0
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestFieldsWithInitializers2()

            Dim initial =
    <element>class C 
    dim C as Integer = 0
    dim B as Integer = 0
    dim A as Integer
end class</element>

            Dim final =
    <element>class C 
    dim A as Integer
    dim C as Integer = 0
    dim B as Integer = 0
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestEnumStatement()

            Dim initial =
    <element>class C 
     Enum Super
           create = 1
     End Enum
     Shared Friend Function Bar() As Integer
     End Function
end class</element>

            Dim final =
    <element>class C 
     Shared Friend Function Bar() As Integer
     End Function
     Enum Super
           create = 1
     End Enum
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestSharedInstance()

            Dim initial =
    <element>class C 
    dim A as Integer
    shared B as Integer
    dim C as Integer
    shared D as Integer
end class</element>

            Dim final =
    <element>class C 
    shared B as Integer
    shared D as Integer
    dim A as Integer
    dim C as Integer
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestAccessibility()

            Dim initial =
    <element>class C 
    dim A as Integer
    private B as Integer
    friend C as Integer
    protected D as Integer
    public E as Integer
end class</element>

            Dim final =
    <element>class C 
    public E as Integer
    protected D as Integer
    friend C as Integer
    dim A as Integer
    private B as Integer
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestSharedAccessibility()

            Dim initial =
    <element>class C 
    dim A1 as Integer
    private B1 as Integer
    friend C1 as Integer
    protected D1 as Integer
    public E1 as Integer
    shared A2 as Integer
    shared private B2 as Integer
    shared friend C2 as Integer
    shared protected D2 as Integer
    shared public E2 as Integer
end class</element>

            Dim final =
    <element>class C 
    shared public E2 as Integer
    shared protected D2 as Integer
    shared friend C2 as Integer
    shared A2 as Integer
    shared private B2 as Integer
    public E1 as Integer
    protected D1 as Integer
    friend C1 as Integer
    dim A1 as Integer
    private B1 as Integer
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestMethodsAccessModifiers()

            Dim initial =
    <element>class C 
        Shared Public Sub Main(args As String())
        End Sub
        Shared Friend Function Bar() As Integer
        End Function
        Shared Private Function Foo() As Integer
        End Function
        Function Goo() As Integer
        End Function  
        Shared Protected Function Moo() As Integer
        End Function  
End class</element>

            Dim final =
    <element>class C 
        Shared Private Function Foo() As Integer
        End Function
        Shared Public Sub Main(args As String())
        End Sub
        Shared Protected Function Moo() As Integer
        End Function  
        Shared Friend Function Bar() As Integer
        End Function
        Function Goo() As Integer
        End Function  
End class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestGenerics()

            Dim initial =
<element>class C 
    sub B(of X,Y)()
    end sub
    sub B(of Z)()
    end sub
    sub B()
    end sub
    sub A(of X,Y)()
    end sub
    sub A(of Z)()
    end sub
    sub A()
    end sub
end class</element>

            Dim final =
<element>class C 
    sub A()
    end sub
    sub A(of Z)()
    end sub
    sub A(of X,Y)()
    end sub
    sub B()
    end sub
    sub B(of Z)()
    end sub
    sub B(of X,Y)()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestInsidePPRegion()

            Dim initial =
<element>class C 
#If True Then
    dim c as Integer
    dim b as Integer
    dim a as Integer
#End If
end class</element>

            Dim final =
    <element>class C 
#If True Then
    dim a as Integer
    dim b as Integer
    dim c as Integer
#End If
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestInsidePPRegion2()

            Dim initial =
    <element>class C 
#If True Then
    dim z as Integer
    dim y as Integer
    dim x as Integer
#End If
#If True Then
    dim c as Integer
    dim b as Integer
    dim a as Integer
#End If
end class</element>

            Dim final =
    <element>class C 
#If True Then
    dim x as Integer
    dim y as Integer
    dim z as Integer
#End If
#If True Then
    dim a as Integer
    dim b as Integer
    dim c as Integer
#End If
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestInsidePPRegion3()

            Dim initial =
    <element>class C 
    dim z as Integer
    dim y as Integer
#If True Then
    dim x as Integer
    dim c as Integer
#End If
    dim b as Integer
    dim a as Integer
end class</element>

            Dim final =
    <element>class C 
    dim y as Integer
    dim z as Integer
#If True Then
    dim c as Integer
    dim x as Integer
#End If
    dim a as Integer
    dim b as Integer
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestInsidePPRegion4()

            Dim initial =
    <element>class C 
    sub c()
    end sub
    sub b()
    end sub
    sub a()
#If True Then
#End If
    end sub
end class</element>

            Dim final =
    <element>class C 
    sub a()
#If True Then
#End If
    end sub
    sub b()
    end sub
    sub c()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestInsidePPRegion5()

            Dim initial =
    <element>class C 
    sub c()
    end sub
    sub b()
    end sub
    sub a()
#If True Then
#Else
#End If
    end sub
end class</element>

            Dim final =
    <element>class C 
    sub a()
#If True Then
#Else
#End If
    end sub
    sub b()
    end sub
    sub c()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestInsidePPRegion6()

            Dim initial =
    <element>class C 
#region
    sub e()
    end sub
    sub d()
    end sub
    sub c()
#region
    end sub
#end region
    sub b()
    end sub
    sub a()
    end sub
#end region
end class</element>

            Dim final =
    <element>class C 
#region
    sub d()
    end sub
    sub e()
    end sub
    sub c()
#region
    end sub
#end region
    sub a()
    end sub
    sub b()
    end sub
#end region
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestPinned()

            Dim initial =
    <element>class C 
    sub z()
    end sub
    sub y()
    end sub
    sub x()
#If True Then
    end sub
    dim n as Integer
    dim m as Integer
    sub c()
#End If
    end sub
    sub b()
    end sub
    sub a()
    end sub
end class</element>

            Dim final =
    <element>class C 
    sub y()
    end sub
    sub z()
    end sub
    sub x()
#If True Then
    end sub
    dim m as Integer
    dim n as Integer
    sub c()
#End If
    end sub
    sub a()
    end sub
    sub b()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestSensitivity()
            Dim initial =
<element>class 
    dim Bb as Integer
    dim B as Integer
    dim bB as Integer
    dim b as Integer
    dim Aa as Integer
    dim a as Integer
    dim A as Integer
    dim aa as Integer
    dim aA as Integer
    dim AA as Integer
    dim bb as Integer
    dim BB as Integer
    dim bBb as Integer
    dim bbB as Integer
    dim あ as Integer
    dim ア as Integer
    dim ｱ as Integer
    dim ああ as Integer
    dim あア as Integer
    dim あｱ as Integer
    dim アあ as Integer
    dim cC as Integer
    dim Cc as Integer
    dim アア as Integer
    dim アｱ as Integer
    dim ｱあ as Integer
    dim ｱア as Integer
    dim ｱｱ as Integer
    dim BBb as Integer
    dim BbB as Integer
    dim bBB as Integer
    dim BBB as Integer
    dim c as Integer
    dim C as Integer
    dim bbb as Integer
    dim Bbb as Integer
    dim cc as Integer
    dim cC as Integer
    dim CC as Integer
end class</element>

            Dim final =
<element>class 
    dim a as Integer
    dim A as Integer
    dim aa as Integer
    dim aA as Integer
    dim Aa as Integer
    dim AA as Integer
    dim b as Integer
    dim B as Integer
    dim bb as Integer
    dim bB as Integer
    dim Bb as Integer
    dim BB as Integer
    dim bbb as Integer
    dim bbB as Integer
    dim bBb as Integer
    dim bBB as Integer
    dim Bbb as Integer
    dim BbB as Integer
    dim BBb as Integer
    dim BBB as Integer
    dim c as Integer
    dim C as Integer
    dim cc as Integer
    dim cC as Integer
    dim cC as Integer
    dim Cc as Integer
    dim CC as Integer
    dim ア as Integer
    dim ｱ as Integer
    dim あ as Integer
    dim アア as Integer
    dim アｱ as Integer
    dim ｱア as Integer
    dim ｱｱ as Integer
    dim アあ as Integer
    dim ｱあ as Integer
    dim あア as Integer
    dim あｱ as Integer
    dim ああ as Integer
end class</element>

            Check(initial, final)
        End Sub



        <WorkItem(537614)>
        <Fact>
        Public Sub TestWhitespaceBetweenMethods1()
            Dim initial =
<element>class Program
    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    sub A()
    end sub

    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestWhitespaceBetweenMethods2()
            Dim initial =
<element>class Program
    sub B()
    end sub


    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    sub A()
    end sub


    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestWhitespaceBetweenMethods3()
            Dim initial =
<element>class Program

    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program

    sub A()
    end sub

    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestWhitespaceBetweenMethods4()
            Dim initial =
<element>class Program


    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program


    sub A()
    end sub

    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestWhitespaceBetweenMethods5()
            Dim initial =
<element>class Program


    sub B()
    end sub


    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program


    sub A()
    end sub


    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestWhitespaceBetweenMethods6()
            Dim initial =
<element>class Program


    sub B()
    end sub



    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program


    sub A()
    end sub



    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestMoveComments1()
            Dim initial =
<element>class Program
    ' B
    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    sub A()
    end sub

    ' B
    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestMoveComments2()
            Dim initial =
<element>class Program
    ' B
    sub B()
    end sub

    ' A
    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    ' A
    sub A()
    end sub

    ' B
    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestMoveDocComments1()
            Dim initial =
<element>class Program
    ''' B
    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    sub A()
    end sub

    ''' B
    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestMoveDocComments2()
            Dim initial =
<element>class Program
    ''' B

    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    sub A()
    end sub

    ''' B

    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestDontMoveBanner()
            Dim initial =
<element>class Program
    ' Banner

    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    ' Banner

    sub A()
    end sub

    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub TestDontMoveBanner2()
            Dim initial =
<element>class Program
    ' Banner

    ' More banner
    ' Bannery stuff

    sub B()
    end sub

    sub A()
    end sub
end class</element>

            Dim final =
<element>class Program
    ' Banner

    ' More banner
    ' Bannery stuff

    sub A()
    end sub

    sub B()
    end sub
end class</element>
            Check(initial, final)
        End Sub

        <WorkItem(537614)>
        <Fact>
        Public Sub Bug2592()
            Dim initial =
<element>Namespace Acme
    Public Class Foo
        
        
        Shared Public Sub Main(args As String())
        End Sub
        
        Public Shared Function Bar() As Integer
        End Function
    End Class
End Namespace</element>

            Dim final =
<element>Namespace Acme
    Public Class Foo
        
        
        Public Shared Function Bar() As Integer
        End Function
        
        Shared Public Sub Main(args As String())
        End Sub
    End Class
End Namespace</element>
            Check(initial, final)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Organizing)>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub OrganizingCommandsDisabledInSubmission()
            Dim exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(InteractiveDocumentSupportsCodeFixService)))

            Using workspace = TestWorkspaceFactory.CreateWorkspace(
                <Workspace>
                    <Submission Language="Visual Basic" CommonReferences="true">  
                        Class C
                            Private $foo As Object
                        End Class
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                exportProvider:=exportProvider)

                ' Force initialization.
                workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = New OrganizeDocumentCommandHandler(workspace.GetService(Of Host.IWaitIndicator))
                Dim delegatedToNext = False
                Dim nextHandler =
                    Function()
                        delegatedToNext = True
                        Return CommandState.Unavailable
                    End Function

                Dim state = handler.GetCommandState(New Commands.SortImportsCommandArgs(textView, textView.TextBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)

                delegatedToNext = False
                state = handler.GetCommandState(New Commands.SortAndRemoveUnnecessaryImportsCommandArgs(textView, textView.TextBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)

                delegatedToNext = False
                state = handler.GetCommandState(New Commands.RemoveUnnecessaryImportsCommandArgs(textView, textView.TextBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)

                delegatedToNext = False
                state = handler.GetCommandState(New Commands.OrganizeDocumentCommandArgs(textView, textView.TextBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)
            End Using
        End Sub
    End Class
End Namespace
