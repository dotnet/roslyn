' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SplitComment
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitComment
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.SplitComment)>
    Public Class SplitCommentCommandHandlerTests
        Inherits AbstractSplitCommentCommandHandlerTests

        Protected Overrides Function CreateWorkspace(markup As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(markup)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitStartOfComment()
            TestHandled(
"Module Program
    Sub Main(args As String())
        '[||]Test Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        '
        'Test Comment
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitStartOfDoubleComment1()
            TestHandled(
"Module Program
    Sub Main(args As String())
        ''[||]Test Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        ''
        ''Test Comment
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitStartOfDoubleComment2()
            TestHandled(
"Module Program
    Sub Main(args As String())
        '' [||]Test Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        ''
        '' Test Comment
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitStartOfDoubleComment3()
            TestHandled(
"Module Program
    Sub Main(args As String())
        ''[||] Test Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        ''
        ''Test Comment
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitStartOfCommentWithLeadingSpace1()
            TestHandled(
"Module Program
    Sub Main(args As String())
        ' [||]Test Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        '
        ' Test Comment
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitStartOfCommentWithLeadingSpace2()
            TestHandled(
"Module Program
    Sub Main(args As String())
        '[||] Test Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        '
        'Test Comment
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitMiddleOfComment()
            TestHandled(
"Module Program
    Sub Main(args As String())
        ' Test [||]Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        ' Test
        ' Comment
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitEndOfComment()
            TestNotHandled(
"Module Program
    Sub Main(args As String())
        ' Test Comment[||]
    End Sub
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestNotAtEndOfFile()
            TestNotHandled(
"Module Program
    Sub Main(args As String())
        ' Test Comment[||]")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitCommentOutOfMethod()
            TestHandled(
"Module Program
    Sub Main(args As String())
        
    End Sub
    ' Test [||]Comment
End Module
",
"Module Program
    Sub Main(args As String())
        
    End Sub
    ' Test
    ' Comment
End Module
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitCommentOutOfModule()
            TestHandled(
"Module Program
    Sub Main(args As String())
        
    End Sub
End Module
' Test [||]Comment
",
"Module Program
    Sub Main(args As String())
        
    End Sub
End Module
' Test
' Comment
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitCommentOutOfClass()
            TestHandled(
"Class Program
    Public Shared Sub Main(args As String())
        
    End Sub
End Class
' Test [||]Comment
",
"Class Program
    Public Shared Sub Main(args As String())
        
    End Sub
End Class
' Test
' Comment
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitCommentOutOfNamespace()
            TestHandled(
"Namespace TestNamespace
    Module Program
        Sub Main(args As String())

        End Sub
    End Module
End Namespace
' Test [||]Comment
",
"Namespace TestNamespace
    Module Program
        Sub Main(args As String())

        End Sub
    End Module
End Namespace
' Test
' Comment
")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        Public Sub TestSplitCommentWithLineContinuation()
            TestNotHandled(
"Module Program
    Sub Main(args As String())
        Dim X As Integer _ ' Comment [||]is here
                       = 4
    End Sub
End Module
")
        End Sub

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        <InlineData("X[||]Test Comment")>
        <InlineData("X [||]Test Comment")>
        <InlineData("X[||] Test Comment")>
        <InlineData("X [||] Test Comment")>
        Public Sub TestCommentWithMultipleLeadingSpaces(commentValue As String)
            TestHandled(
$"public class Program
    public sub Goo()
        '    {commentValue}
    end sub
end class",
"public class Program
    public sub Goo()
        '    X
        '    Test Comment
    end sub
end class")
        End Sub

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/38516")>
        <InlineData("X[||]Test Comment")>
        <InlineData("X [||]Test Comment")>
        <InlineData("X[||] Test Comment")>
        <InlineData("X [||] Test Comment")>
        Public Sub TestQuadCommentWithMultipleLeadingSpaces(commentValue As String)
            TestHandled(
$"public class Program
    public sub Goo()
        ''''    {commentValue}
    end sub
end class",
"public class Program
    public sub Goo()
        ''''    X
        ''''    Test Comment
    end sub
end class")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48547")>
        Public Sub TestSplitWithCommentAfterwards1()
            TestNotHandled(
"public class Program
    public sub Goo()
        ' goo[||]  'Test Comment
    end sub
end class")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48547")>
        Public Sub TestSplitWithCommentAfterwards2()
            TestNotHandled(
"public class Program
{
    public sub Goo()
    { 
        ' goo [||] 'Test Comment
    end sub
end class")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48547")>
        Public Sub TestSplitWithCommentAfterwards3()
            TestNotHandled(
"public class Program
{
    public sub Goo()
        ' goo  [||]'Test Comment
    end sub
end class")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48547")>
        Public Sub TestSplitWithCommentAfterwards4()
            TestNotHandled(
"public class Program
    public sub Goo()
        // [|goo|] 'Test Comment
    end sub
end class")
        End Sub
    End Class
End Namespace
