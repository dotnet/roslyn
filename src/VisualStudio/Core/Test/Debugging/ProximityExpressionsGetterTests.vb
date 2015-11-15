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
        Public Sub GenerateBaseline()
            Dim text = Resources.ProximityExpressionsGetterTestFile
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(text)
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
                Dim statements = document.GetSyntaxRootAsync(CancellationToken.None).Result.DescendantTokens().Select(Function(t) t.GetAncestor(Of StatementSyntax)()).Distinct().WhereNotNull()
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
                    Dim terms = proximityExpressionsGetter.GetProximityExpressionsAsync(document, token.SpanStart, CancellationToken.None).Result

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

        End Sub

        Private Sub TestProximityExpressionsGetter(markup As String,
                                                   continuation As Action(Of VisualBasicProximityExpressionsService, Document, Integer))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(markup)
                Dim testDocument = workspace.Documents.Single()
                Dim snapshot = testDocument.TextBuffer.CurrentSnapshot
                Dim caretPosition = testDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(testDocument.Id)

                Dim proximityExpressionsGetter = New VisualBasicProximityExpressionsService()
                continuation(proximityExpressionsGetter, document, caretPosition)
            End Using
        End Sub

        Public Sub TestTryDo(input As String,
                             ParamArray expectedTerms As String())
            TestProximityExpressionsGetter(input, Sub(getter, semanticSnapshot, point)
                                                      Dim terms = getter.GetProximityExpressionsAsync(semanticSnapshot, point, CancellationToken.None).Result

                                                      If expectedTerms.Length = 0 Then
                                                          Assert.Null(terms)
                                                      Else
                                                          AssertEx.Equal(expectedTerms, terms)
                                                      End If
                                                  End Sub)
        End Sub

        Public Sub TestIsValid(input As String, expression As String, expectedValid As Boolean)
            TestProximityExpressionsGetter(input, Sub(getter, semanticSnapshot, point)
                                                      Dim actualValid = getter.IsValidAsync(semanticSnapshot, point, expression, CancellationToken.None).Result
                                                      Assert.Equal(expectedValid, actualValid)
                                                  End Sub)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(538819)>
        <WorkItem(527754)>
        Public Sub TestIsValid1()
            TestIsValid(<text>Module M
    Sub S
        Dim local As String
        $$
    End Sub
End Module</text>.Value, "local", True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Sub TestIsValidWithDiagnostics()
            ' local doesn't exist in this context
            TestIsValid("class Class { void Method() { string local; } $$}", "local", False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Sub TestIsValidReferencingLocalBeforeDeclaration()
            TestIsValid("class Class { void Method() { $$int i; int j; } }", "j", False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Sub TestIsValidReferencingUndefinedVariable()
            TestIsValid("class Class { void Method() { $$int i; int j; } }", "k", False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Sub TestIsValidNoTypeSymbol()
            TestIsValid("namespace Namespace$$ { }", "foo", False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(527754)>
        Public Sub TestIsValidLocalAfterPosition()
            TestIsValid("class Class { void Method() { $$ int i; string local; } }", "local", False)
        End Sub

        Private Sub TestLanguageDebugInfoTryGetProximityExpressions(input As String, expectedResults As IEnumerable(Of String), expectedResult As Boolean)
            Dim parsedInput As String = input
            Dim caretPosition As Integer
            MarkupTestFile.GetPosition(input, parsedInput, caretPosition)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(parsedInput)
                Dim service = New VisualBasicProximityExpressionsService()
                Dim hostdoc = workspace.Documents.First()
                Dim snapshot = hostdoc.TextBuffer.CurrentSnapshot
                Dim snapshotPoint = New SnapshotPoint(snapshot, caretPosition)
                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)

                Dim result = service.GetProximityExpressionsAsync(document, caretPosition, CancellationToken.None).Result
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        <WorkItem(538819)>
        Public Sub TestDebugInfo1()
            TestLanguageDebugInfoTryGetProximityExpressions("$$Module M : End Module", Array.Empty(Of String)(), False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestTryDo1()
            TestTryDo(<text>Module M
    Sub S
        Dim local As String
        $$
    End Sub
End Module</text>.NormalizedValue, "local")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestStatementTerminatorToken()
            TestTryDo(<text>Module M$$
</text>.Value)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestNoParentToken()
            TestTryDo(<text>$$</text>.NormalizedValue)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestCatchParameters()
            TestTryDo(<text>
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
        End Sub

        <WorkItem(538847)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestMultipleStatementsOnSameLine()
            TestTryDo(<text>
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

        End Sub

    End Class
End Namespace
