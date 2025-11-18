' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
    <UseExportProvider>
    Public Class SyntacticChangeRangeComputerTests
        Private Shared Function TestCSharp(markup As String, newText As String) As Task
            Return Test(markup, newText, LanguageNames.CSharp)
        End Function

        Private Shared Async Function Test(markup As String, newText As String, language As String) As Task
            Using workspace = EditorTestWorkspace.Create(language, compilationOptions:=Nothing, parseOptions:=Nothing, markup)
                Dim testDocument = workspace.Documents(0)
                Dim startingDocument = workspace.CurrentSolution.GetDocument(testDocument.Id)

                Dim spans = testDocument.SelectedSpans
                Assert.True(1 = spans.Count, "Test should have one spans in it representing the span to replace")

                Dim annotatedSpans = testDocument.AnnotatedSpans
                Assert.True(1 = annotatedSpans.Count, "Test should have a single {||} span representing the change span in the final document")
                Dim annotatedSpan = annotatedSpans.Single().Value.Single()

                Dim startingText = Await startingDocument.GetTextAsync()
                Dim startingTree = Await startingDocument.GetSyntaxTreeAsync()
                Dim startingRoot = Await startingTree.GetRootAsync()

                Dim endingText = startingText.Replace(spans(0), newText)
                Dim endingTree = startingTree.WithChangedText(endingText)
                Dim endingRoot = Await endingTree.GetRootAsync()

                Dim actualChange = SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(startingRoot, endingRoot, TimeSpan.MaxValue, Nothing)
                Dim expectedChange = New TextChangeRange(
                    annotatedSpan,
                    annotatedSpan.Length + newText.Length - spans(0).Length)
                Assert.True(expectedChange = actualChange, expectedChange.ToString() & " != " & actualChange.ToString() & vbCrLf & "Changed span was" & vbCrLf & startingText.ToString(actualChange.Span))
            End Using
        End Function

        <Fact>
        Public Async Function TestIdentifierChangeInMethod1() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
{|changed:        Con[||]|}.WriteLine(0);
    }

    void M3()
    {
    }
}
", "sole")
        End Function

        <Fact>
        Public Async Function TestIdentifierChangeInMethod1_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
{|changed:            Con[||]|}.WriteLine(0);
        }

        void M3()
        {
        }
    }
}
", "sole")
        End Function

        <Fact>
        Public Async Function TestIdentifierChangeInMethod2() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
{|changed:        Con[|sole|]|}.WriteLine(0);
    }

    void M3()
    {
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestIdentifierChangeInMethod2_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
{|changed:            Con[|sole|]|}.WriteLine(0);
        }

        void M3()
        {
        }
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestSplitClass1() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }
{|changed:
[||]

    void |}M2()
    {
        Console.WriteLine(0);
    }

    void M3()
    {
    }
}
", "} class C2 {")
        End Function

        <Fact>
        Public Async Function TestSplitClass1_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }
{|changed:
    [||]

        void |}M2()
        {
            Console.WriteLine(0);
        }

        void M3()
        {
        }
    }
}
", "} class C2 {")
        End Function

        <Fact>
        Public Async Function TestMergeClass() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }
{|changed:

[|} class C2 {|]

    void |}M2()
    {
        Console.WriteLine(0);
    }

    void M3()
    {
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestMergeClass_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }
{|changed:

    [|} class C2 {|]

        void |}M2()
        {
            Console.WriteLine(0);
        }

        void M3()
        {
        }
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestExtendComment() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
{|changed:        [||]
    }

    void M3()
    {
        Console.WriteLine(""*/ Console.WriteLine("")
|}    }

    void M4()
    {
    }
}
", "/*")
        End Function

        <Fact>
        Public Async Function TestExtendComment_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
{|changed:            [||]
        }

        void M3()
        {
            Console.WriteLine(""*/ Console.WriteLine("")
|}        }

        void M4()
        {
        }
    }
}
", "/*")
        End Function

        <Fact>
        Public Async Function TestRemoveComment() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
{|changed:        [|/*|]
    }

    void M3()
    {
        Console.WriteLine(""*/ Console.WriteLine("")
|}    }

    void M4()
    {
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestRemoveComment_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
{|changed:            [|/*|]
        }

        void M3()
        {
            Console.WriteLine(""*/ Console.WriteLine("")
|}        }

        void M4()
        {
        }
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestExtendCommentToEndOfFile() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
{|changed:        [||]
    }

    void M3()
    {
    }

    void M4()
    {
    }
}
|}", "/*")
        End Function

        <Fact>
        Public Async Function TestExtendCommentToEndOfFile_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
{|changed:            [||]
        }

        void M3()
        {
        }

        void M4()
        {
        }
    }
}
|}", "/*")
        End Function

        <Fact>
        Public Async Function TestDeleteFullFile() As Task
            Await TestCSharp(
"{|changed:[|
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
    }

    void M3()
    {
    }

    void M4()
    {
    }
}
|]|}", "")
        End Function

        <Fact>
        Public Async Function TestDeleteFullFile_A() As Task
            Await TestCSharp(
"{|changed:[|
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
        }

        void M3()
        {
        }

        void M4()
        {
        }
    }
}
|]|}", "")
        End Function

        <Fact>
        Public Async Function InsertFullFile() As Task
            Await TestCSharp(
"{|changed:[||]|}", "
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
    }

    void M3()
    {
    }

    void M4()
    {
    }
}
")
        End Function

        <Fact>
        Public Async Function InsertFullFile_A() As Task
            Await TestCSharp(
"{|changed:[||]|}", "
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
        }

        void M3()
        {
        }

        void M4()
        {
        }
    }
}
")
        End Function

        <Fact>
        Public Async Function TestInsertDuplicateLineBelow() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
        throw new NotImplementedException();[||]
{|changed:|}    }

    void M3()
    {
    }
}
", "
        throw new NotImplementedException();")
        End Function

        <Fact>
        Public Async Function TestInsertDuplicateLineBelow_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
            throw new NotImplementedException();[||]
{|changed:|}        }

        void M3()
        {
        }
    }
}
", "
        throw new NotImplementedException();")
        End Function

        <Fact>
        Public Async Function TestInsertDuplicateLineAbove() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {[||]
        throw new NotImplementedException();
{|changed:|}    }

    void M3()
    {
    }
}
", "
        throw new NotImplementedException();")
        End Function

        <Fact>
        Public Async Function TestInsertDuplicateLineAbove_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {[||]
{|changed:|}            throw new NotImplementedException();
        }

        void M3()
        {
        }
    }
}
", "
        throw new NotImplementedException();")
        End Function

        <Fact>
        Public Async Function TestDeleteDuplicateLineBelow() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
        throw new NotImplementedException();
{|changed:        [|throw new NotImplementedException();|]
    }
|}
    void M3()
    {
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestDeleteDuplicateLineBelow_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
            throw new NotImplementedException();
{|changed:            [|throw new NotImplementedException();|]
        }
|}
        void M3()
        {
        }
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestDeleteDuplicateLineAbove() As Task
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
{|changed:        [|throw new NotImplementedException();|]
        throw |}new NotImplementedException();
    }

    void M3()
    {
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestDeleteDuplicateLineAbove_A() As Task
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
{|changed:            [|throw new NotImplementedException();|]
            throw |}new NotImplementedException();
        }

        void M3()
        {
        }
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestDeleteInDeeplyNestedExpression() As Task
            Dim binaryExp As New StringBuilder()
            For j = 0 To 10000
                binaryExp.Append(j.ToString(Globalization.CultureInfo.InvariantCulture))
                binaryExp.Append(" + ")
            Next
            Await TestCSharp(
"
using X;

public class C
{
    void M1()
    {
    }

    void M2()
    {
        var x = {|changed:" + binaryExp.ToString() + "[|1 +|] |}" + binaryExp.ToString() + "1;
    }

    void M3()
    {
    }
}
", "")
        End Function

        <Fact>
        Public Async Function TestDeleteInDeeplyNestedExpression_A() As Task
            Dim binaryExp As New StringBuilder()
            For j = 0 To 10000
                binaryExp.Append(j.ToString(Globalization.CultureInfo.InvariantCulture))
                binaryExp.Append(" + ")
            Next
            Await TestCSharp(
"
using X;

namespace N
{
    public class C
    {
        void M1()
        {
        }

        void M2()
        {
            var x = {|changed:" + binaryExp.ToString() + "[|1 +|] |}" + binaryExp.ToString() + "1;
        }

        void M3()
        {
        }
    }
}
", "")
        End Function
    End Class
End Namespace
