' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class GetDiagnosticsTests
        Inherits BasicTestBase

        <Fact>
        Public Sub DiagnosticsFilteredInMethodBody()
            Dim source = <project><file>
Class C
    Sub S()
        @
        #
        !
    End Sub
End Class
</file></project>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single())

            ' CreateCompilation... method normalizes EOL chars of xml literals,
            ' so we cannot verify against spans in the original xml here as positions would differ
            Dim sourceText = compilation.SyntaxTrees.Single().GetText().ToString()
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "(?s)^.*$", "BC30035", "BC30248", "BC30203", "BC30157")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "@", "BC30035")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "#", "BC30248")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "(?<=\!)", "BC30203", "BC30157")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "!", "BC30203", "BC30157")
        End Sub

        <Fact>
        Public Sub DiagnosticsFilteredInMethodBodyInsideNamespace()
            Dim source = <project><file>
Namespace N
    Class C
        Sub S()
            X
        End Sub
    End Class
End Namespace

Class D
    ReadOnly Property P As Integer
        Get
            Return Y
        End Get
    End Property
End Class
</file></project>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single())

            Dim sourceText = compilation.SyntaxTrees.Single().GetText().ToString()
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "X", "BC30451")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "Y", "BC30451")
        End Sub

        <Fact>
        Public Sub DiagnosticsFilteredForInsersectingIntervals()
            Dim source = <project><file>
Class C
    Inherits Abracadabra
End Class
</file></project>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single())

            ' CreateCompilation... method normalizes EOL chars of xml literals,
            ' so we cannot verify against spans in the original xml here as positions would differ
            Dim sourceText = compilation.SyntaxTrees.Single().GetText().ToString()
            Const ErrorId = "BC30002"
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "(?s)^.*$", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "Abracadabra", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "ts Abracadabra", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "ts Abracadabr", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "Abracadabra[\r\n]+", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "bracadabra[\r\n]+", ErrorId)
        End Sub

        <Fact, WorkItem(1066483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066483")>
        Public Sub TestDiagnosticWithSeverity()
            Dim source = <project><file>
Class C
    Sub Foo()
        Dim x
    End Sub
End Class
</file></project>
            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim diag = compilation.GetDiagnostics().Single()

            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity)
            Assert.Equal(1, diag.WarningLevel)

            Dim [error] = diag.WithSeverity(DiagnosticSeverity.Error)
            Assert.Equal(DiagnosticSeverity.Error, [error].Severity)
            Assert.Equal(DiagnosticSeverity.Warning, [error].DefaultSeverity)
            Assert.Equal(0, [error].WarningLevel)

            Dim warning = [error].WithSeverity(DiagnosticSeverity.Warning)
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity)
            Assert.Equal(DiagnosticSeverity.Warning, warning.DefaultSeverity)
            Assert.Equal(1, warning.WarningLevel)

            Dim hidden = warning.WithSeverity(DiagnosticSeverity.Hidden)
            Assert.Equal(DiagnosticSeverity.Hidden, hidden.Severity)
            Assert.Equal(DiagnosticSeverity.Warning, hidden.DefaultSeverity)
            Assert.Equal(4, hidden.WarningLevel)

            Dim info = warning.WithSeverity(DiagnosticSeverity.Info)
            Assert.Equal(DiagnosticSeverity.Info, info.Severity)
            Assert.Equal(DiagnosticSeverity.Warning, info.DefaultSeverity)
            Assert.Equal(4, info.WarningLevel)
        End Sub

        <Fact, WorkItem(7446, "https://github.com/dotnet/roslyn/issues/7446")>
        Public Sub TestCompilationEventQueueWithSemanticModelGetDiagnostics()
            Dim source1 = <file>
Namespace N1
    Partial Class C
        Private Sub NonPartialMethod1()
        End Sub
    End Class
End Namespace
</file>.Value

            Dim source2 =
               <file>
Namespace N1
    Partial Class C
        Private Sub NonPartialMethod2()
        End Sub
    End Class
End Namespace
</file>.Value

            Dim tree1 = VisualBasicSyntaxTree.ParseText(source1, path:="file1")
            Dim tree2 = VisualBasicSyntaxTree.ParseText(source2, path:="file2")
            Dim eventQueue = New AsyncQueue(Of CompilationEvent)()
            Dim compilation = CreateCompilationWithMscorlib45({tree1, tree2}).WithEventQueue(eventQueue)

            ' Invoke SemanticModel.GetDiagnostics to force populate the event queue for symbols in the first source file.
            Dim tree = compilation.SyntaxTrees.[Single](Function(t) t Is tree1)
            Dim root = tree.GetRoot()
            Dim model = compilation.GetSemanticModel(tree)
            model.GetDiagnostics(root.FullSpan)

            Assert.True(eventQueue.Count > 0)
            Dim compilationStartedFired As Boolean
            Dim declaredSymbolNames As HashSet(Of String) = Nothing, completedCompilationUnits As HashSet(Of String) = Nothing
            Assert.True(DequeueCompilationEvents(eventQueue, compilationStartedFired, declaredSymbolNames, completedCompilationUnits))

            ' Verify symbol declared events fired for all symbols declared in the first source file.
            Assert.True(compilationStartedFired)
            Assert.True(declaredSymbolNames.Contains(compilation.GlobalNamespace.Name))
            Assert.True(declaredSymbolNames.Contains("N1"))
            Assert.True(declaredSymbolNames.Contains("C"))
            Assert.True(declaredSymbolNames.Contains("NonPartialMethod1"))
            Assert.True(completedCompilationUnits.Contains(tree.FilePath))
        End Sub

        <Fact, WorkItem(7477, "https://github.com/dotnet/roslyn/issues/7477")>
        Public Sub TestCompilationEventsForPartialMethod()
            Dim source1 = <file>
Namespace N1
    Partial Class C
        Private Sub NonPartialMethod1()
        End Sub

        Private Partial Sub PartialMethod() ' Declaration
        End Sub

        Private Partial Sub ImpartialMethod() ' Declaration
        End Sub
    End Class
End Namespace
</file>.Value

            Dim source2 =
               <file>
Namespace N1
    Partial Class C
        Private Sub NonPartialMethod2()
        End Sub

        Private Sub PartialMethod() ' Implementation
        End Sub
    End Class
End Namespace
</file>.Value

            Dim tree1 = VisualBasicSyntaxTree.ParseText(source1, path:="file1")
            Dim tree2 = VisualBasicSyntaxTree.ParseText(source2, path:="file2")
            Dim eventQueue = New AsyncQueue(Of CompilationEvent)()
            Dim compilation = CreateCompilationWithMscorlib45({tree1, tree2}).WithEventQueue(eventQueue)

            ' Invoke SemanticModel.GetDiagnostics to force populate the event queue for symbols in the first source file.
            Dim tree = compilation.SyntaxTrees.[Single](Function(t) t Is tree1)
            Dim root = tree.GetRoot()
            Dim model = compilation.GetSemanticModel(tree)
            model.GetDiagnostics(root.FullSpan)

            Assert.True(eventQueue.Count > 0)
            Dim compilationStartedFired As Boolean
            Dim declaredSymbolNames As HashSet(Of String) = Nothing, completedCompilationUnits As HashSet(Of String) = Nothing
            Assert.True(DequeueCompilationEvents(eventQueue, compilationStartedFired, declaredSymbolNames, completedCompilationUnits))

            ' Verify symbol declared events fired for all symbols declared in the first source file.
            Assert.True(compilationStartedFired)
            Assert.True(declaredSymbolNames.Contains(compilation.GlobalNamespace.Name))
            Assert.True(declaredSymbolNames.Contains("N1"))
            Assert.True(declaredSymbolNames.Contains("C"))
            Assert.True(declaredSymbolNames.Contains("NonPartialMethod1"))
            Assert.True(declaredSymbolNames.Contains("ImpartialMethod"))
            Assert.True(declaredSymbolNames.Contains("PartialMethod"))
            Assert.True(completedCompilationUnits.Contains(tree.FilePath))
        End Sub

        Private Shared Function DequeueCompilationEvents(eventQueue As AsyncQueue(Of CompilationEvent), ByRef compilationStartedFired As Boolean, ByRef declaredSymbolNames As HashSet(Of String), ByRef completedCompilationUnits As HashSet(Of String)) As Boolean
            compilationStartedFired = False
            declaredSymbolNames = New HashSet(Of String)()
            completedCompilationUnits = New HashSet(Of String)()
            If eventQueue.Count = 0 Then
                Return False
            End If

            Dim compEvent As CompilationEvent = Nothing
            While eventQueue.TryDequeue(compEvent)
                If TypeOf compEvent Is CompilationStartedEvent Then
                    Assert.[False](compilationStartedFired, "Unexpected multiple compilation stated events")
                    compilationStartedFired = True
                Else
                    Dim symbolDeclaredEvent = TryCast(compEvent, SymbolDeclaredCompilationEvent)
                    If symbolDeclaredEvent IsNot Nothing Then
                        Dim symbol = symbolDeclaredEvent.Symbol
                        Assert.True(declaredSymbolNames.Add(symbol.Name), "Unexpected multiple symbol declared events for same symbol")
                        Dim method = TryCast(symbol, Symbols.MethodSymbol)
                        Assert.Null(method?.PartialDefinitionPart) ' we should never get a partial method's implementation part
                    Else
                        Dim compilationCompeletedEvent = TryCast(compEvent, CompilationUnitCompletedEvent)
                        If compilationCompeletedEvent IsNot Nothing Then
                            Assert.True(completedCompilationUnits.Add(compilationCompeletedEvent.CompilationUnit.FilePath))
                        End If
                    End If
                End If
            End While

            Return True
        End Function
    End Class
End Namespace