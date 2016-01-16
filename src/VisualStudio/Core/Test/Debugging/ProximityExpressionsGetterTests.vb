' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Debugging
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports System.Threading.Tasks

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging
    Partial Public Class ProximityExpressionsGetterTests

        Public Function GetTree() As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(Resources.ProximityExpressionsGetterTestFile)
        End Function

        ''' <summary>
        ''' This is used to generate a baseline (ProximityExpressionsGetterTests.Statements.vb and
        ''' ProximityExpressionsGetterTests.Expressions.vb) The test file comes from the TestFiles
        ''' folder in this project.
        ''' </summary>
        Public Async Function GenerateBaselineAsync() As Task
            Dim text = Resources.ProximityExpressionsGetterTestFile
            Using workspace = Await VisualBasicWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(text)
                Dim languageDebugInfo = New VisualBasicLanguageDebugInfoService()

                Dim hostdoc = workspace.Documents.First()
                Dim snapshot = hostdoc.TextBuffer.CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)

                Dim builder = New StringBuilder()

                ' There are two types of tests: Expressions and Statements.
                ' Depending on which code is 'falsified', expressions or statements are generated.
#If False Then
                // Try to get proximity expressions at every token position and the start of every
                // line.
                foreach (var line in snapshot.Lines)
                {
                    builder.AppendLine("[Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]");
                    builder.AppendLine("public void TestAtStartOfLine_" + (line.LineNumber + 1) + "()");
                    builder.AppendLine("{");

                    if (line.LineNumber > 0)
                    {
                        builder.AppendLine("    //// " + snapshot.GetLineFromLineNumber(line.LineNumber - 1).GetText());
                    }

                    builder.AppendLine("    //// " + line.GetText());
                    builder.AppendLine("    //// ^");
                    builder.AppendLine("    var tree = GetTree(\"ProximityExpressionsGetterTestFile.cs\");");
                    builder.AppendLine("    IList<string> terms;");
                    builder.AppendLine("    var result = ProximityExpressionsGetter.TryDo(tree, " + line.Start.Position + ", out terms);");

                    var proximityExpressionsGetter = new ProximityExpressionsGetter(languageDebugInfo);
                    IList<string> terms;
                    var result = proximityExpressionsGetter.TryDo(line.Start, out terms);
                    if (!result)
                    {
                        builder.AppendLine("    Assert.False(result);");
                    }
                    else
                    {
                        builder.AppendLine("    Assert.True(result);");

                        var termsString = terms.Select(t => "\"" + t + "\"").Join(", ");
                        builder.AppendLine("    AssertEx.Equal(new[] { " + termsString + " }, terms);");
                    }

                    builder.AppendLine("}");
                    builder.AppendLine();
                }
#Else
                ' Try to get proximity expressions at every token position and the start of every
                ' line.
                Dim index = 0
                Dim statements = (Await document.GetSyntaxRootAsync()).DescendantTokens().Select(Function(t) t.GetAncestor(Of StatementSyntax)()).Distinct().WhereNotNull()
                For Each statement In statements
                    builder.AppendLine("<WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>")
                    builder.AppendLine("Public Sub TestAtStartOfStatement_" & index & "()")

                    Dim token = statement.GetFirstToken()
                    Dim line = snapshot.GetLineFromPosition(token.SpanStart)

                    builder.AppendLine("    ' Line " & (line.LineNumber + 1))
                    builder.AppendLine()
                    If line.LineNumber > 0 Then
                        builder.AppendLine("    ' " + snapshot.GetLineFromLineNumber(line.LineNumber - 1).GetText())
                    End If

                    builder.AppendLine("    ' " + line.GetText())
                    Dim charIndex = token.SpanStart - line.Start.Position
                    builder.AppendLine("    ' " + New String(" "c, charIndex) + "^")
                    builder.AppendLine("    dim tree = GetTree(""ProximityExpressionsGetterTestFile.vb"")")
                    builder.AppendLine("    dim terms = VisualBasicProximityExpressionsService.Do(tree, " & token.SpanStart & ")")

                    Dim proximityExpressionsGetter = New VisualBasicProximityExpressionsService()
                    Dim terms = Await proximityExpressionsGetter.GetProximityExpressionsAsync(document, token.SpanStart, CancellationToken.None)

                    If terms Is Nothing Then
                        builder.AppendLine("    Assert.Null(terms)")
                    Else
                        builder.AppendLine("    Assert.NotNull(terms)")

                        Dim termsString = terms.Select(Function(t) """" & t & """").Join(", ")
                        builder.AppendLine("    AssertEx.Equal({ " + termsString + " }, terms)")
                    End If

                    builder.AppendLine("End Sub")
                    builder.AppendLine()
                    index = index + 1
                Next

                Dim str = builder.ToString()
                Clipboard.SetText(str)
            End Using
#End If

        End Function

        Private Async Function TestProximityExpressionsGetterAsync(markup As String,
                                                   continuation As Func(Of VisualBasicProximityExpressionsService, Document, Integer, Task)) As Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(markup)
                Dim testDocument = workspace.Documents.Single()
                Dim snapshot = testDocument.TextBuffer.CurrentSnapshot
                Dim caretPosition = testDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(testDocument.Id)

                Dim proximityExpressionsGetter = New VisualBasicProximityExpressionsService()
                Await continuation(proximityExpressionsGetter, document, caretPosition)
            End Using
        End Function

        Public Async Function TestTryDoAsync(input As String,
                             ParamArray expectedTerms As String()) As Task
            Await TestProximityExpressionsGetterAsync(input,
                                                      Async Function(getter, semanticSnapshot, point)
                                                          Dim terms = Await getter.GetProximityExpressionsAsync(semanticSnapshot, point, CancellationToken.None)

                                                          If expectedTerms.Length = 0 Then
                                                              Assert.Null(terms)
                                                          Else
                                                              AssertEx.Equal(expectedTerms, terms)
                                                          End If
                                                      End Function)
        End Function

        Public Async Function TestIsValidAsync(input As String, expression As String, expectedValid As Boolean) As Task
            Await TestProximityExpressionsGetterAsync(input,
                                                      Async Function(getter, semanticSnapshot, point)
                                                          Dim actualValid = Await getter.IsValidAsync(semanticSnapshot, point, expression, CancellationToken.None)
                                                          Assert.Equal(expectedValid, actualValid)
                                                      End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(538819)>
        <WorkItem(527754)>
        Public Async Function TestIsValid1() As Task
            Await TestIsValidAsync(<text>Module M
    Sub S
        Dim local As String
        $$
    End Sub
End Module</text>.Value, "local", True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Async Function TestIsValidWithDiagnostics() As Task
            ' local doesn't exist in this context
            Await TestIsValidAsync("class Class { void Method() { string local; } $$}", "local", False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Async Function TestIsValidReferencingLocalBeforeDeclaration() As Task
            Await TestIsValidAsync("class Class { void Method() { $$int i; int j; } }", "j", False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Async Function TestIsValidReferencingUndefinedVariable() As Task
            Await TestIsValidAsync("class Class { void Method() { $$int i; int j; } }", "k", False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Async Function TestIsValidNoTypeSymbol() As Task
            Await TestIsValidAsync("namespace Namespace$$ { }", "foo", False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Async Function TestIsValidLocalAfterPosition() As Task
            Await TestIsValidAsync("class Class { void Method() { $$ int i; string local; } }", "local", False)
        End Function

        Private Async Function TestLanguageDebugInfoTryGetProximityExpressionsAsync(input As String, expectedResults As IEnumerable(Of String), expectedResult As Boolean) As Task
            Dim parsedInput As String = input
            Dim caretPosition As Integer
            MarkupTestFile.GetPosition(input, parsedInput, caretPosition)

            Using workspace = Await VisualBasicWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(parsedInput)
                Dim service = New VisualBasicProximityExpressionsService()
                Dim hostdoc = workspace.Documents.First()
                Dim snapshot = hostdoc.TextBuffer.CurrentSnapshot
                Dim snapshotPoint = New SnapshotPoint(snapshot, caretPosition)
                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)

                Dim result = Await service.GetProximityExpressionsAsync(document, caretPosition, CancellationToken.None)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(538819)>
        Public Async Function TestDebugInfo1() As Task
            Await TestLanguageDebugInfoTryGetProximityExpressionsAsync("$$Module M : End Module", Array.Empty(Of String)(), False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Async Function TestTryDo1() As Task
            Await TestTryDoAsync(<text>Module M
    Sub S
        Dim local As String
        $$
    End Sub
End Module</text>.NormalizedValue, "local")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Async Function TestStatementTerminatorToken() As Task
            Await TestTryDoAsync(<text>Module M$$
</text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Async Function TestNoParentToken() As Task
            Await TestTryDoAsync(<text>$$</text>.NormalizedValue)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Async Function TestCatchParameters() As Task
            Await TestTryDoAsync(<text>
Module M
    Sub S
        Dim x As Integer
        Try
            Console.Write(x)
        Catch e As Exception
            $$x = 5
        End Try
   End Sub
End Module
                      </text>.NormalizedValue, "x", "e")
        End Function

        <WorkItem(538847)>
        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Async Function TestMultipleStatementsOnSameLine() As Task
            Await TestTryDoAsync(<text>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        dim i = 4
        dim j = 5
        dim k = 6
        dim m = 6
        dim n = 6
        $$Foo(i) : Bar(j)
    End Sub
End Module</text>.NormalizedValue, "Foo", "i", "n", "Bar", "j")

        End Function

    End Class
End Namespace
