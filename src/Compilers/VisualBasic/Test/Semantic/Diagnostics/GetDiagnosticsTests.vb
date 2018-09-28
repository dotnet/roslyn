' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            Dim compilation = CreateCompilationWithMscorlib40(source)
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

            Dim compilation = CreateCompilationWithMscorlib40(source)
            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single())

            Dim sourceText = compilation.SyntaxTrees.Single().GetText().ToString()
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "X", "BC30451")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "Y", "BC30451")
        End Sub

        <Fact>
        Public Sub DiagnosticsFilteredForIntersectingIntervals()
            Dim source = <project><file>
Class C
    Inherits Abracadabra
End Class
</file></project>

            Dim compilation = CreateCompilationWithMscorlib40(source)
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
    Sub Goo()
        Dim x
    End Sub
End Class
</file></project>
            Dim compilation = CreateCompilationWithMscorlib40(source)
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
            Assert.Equal(1, hidden.WarningLevel)

            Dim info = warning.WithSeverity(DiagnosticSeverity.Info)
            Assert.Equal(DiagnosticSeverity.Info, info.Severity)
            Assert.Equal(DiagnosticSeverity.Warning, info.DefaultSeverity)
            Assert.Equal(1, info.WarningLevel)
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

        <Fact>
        Public Sub CompilingCodeWithInvalidPreProcessorSymbolsShouldProvideDiagnostics()
            Dim dict = New Dictionary(Of String, Object)
            dict.Add("1", Nothing)
            Dim compilation = CreateEmptyCompilation(String.Empty, parseOptions:=New VisualBasicParseOptions().WithPreprocessorSymbols(dict))

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors>
BC31030: Conditional compilation constant '1' is not valid: Identifier expected.

~
</errors>)
        End Sub

        <Fact>
        Public Sub CompilingCodeWithInvalidSourceCodeKindShouldProvideDiagnostics()
#Disable Warning BC40000 ' Type or member is obsolete
            Dim compilation = CreateCompilationWithMscorlib45(String.Empty, parseOptions:=New VisualBasicParseOptions().WithKind(SourceCodeKind.Interactive))
#Enable Warning BC40000 ' Type or member is obsolete

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors>
BC37285: Provided source code kind is unsupported or invalid: 'Interactive'

~
</errors>)
        End Sub

        <Fact>
        Public Sub CompilingCodeWithInvalidLanguageVersionShouldProvideDiagnostics()
            Dim compilation = CreateEmptyCompilation(String.Empty, parseOptions:=New VisualBasicParseOptions().WithLanguageVersion(DirectCast(10000, LanguageVersion)))

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors>
BC37287: Provided language version is unsupported or invalid: '10000'.

~
</errors>)
        End Sub

        <Fact>
        Public Sub CompilingCodeWithInvalidDocumentationModeShouldProvideDiagnostics()
            Dim compilation = CreateEmptyCompilation(String.Empty, parseOptions:=New VisualBasicParseOptions().WithDocumentationMode(CType(100, DocumentationMode)))

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors>
BC37286: Provided documentation mode is unsupported or invalid: '100'.

~
</errors>)
        End Sub

        <Fact>
        Public Sub CompilingCodeWithInvalidParseOptionsInMultipleSyntaxTreesShouldReportThemAll()
            Dim dict1 = New Dictionary(Of String, Object)
            dict1.Add("1", Nothing)
            Dim dict2 = New Dictionary(Of String, Object)
            dict2.Add("2", Nothing)
            Dim dict3 = New Dictionary(Of String, Object)
            dict3.Add("3", Nothing)

            Dim syntaxTree1 = Parse(String.Empty, options:=New VisualBasicParseOptions().WithPreprocessorSymbols(dict1))
            Dim syntaxTree2 = Parse(String.Empty, options:=New VisualBasicParseOptions().WithPreprocessorSymbols(dict2))
            Dim syntaxTree3 = Parse(String.Empty, options:=New VisualBasicParseOptions().WithPreprocessorSymbols(dict3))

            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            Dim compilation = CreateCompilationWithMscorlib40({syntaxTree1, syntaxTree2, syntaxTree3}, options:=options)
            Dim diagnostics = compilation.GetDiagnostics()

            CompilationUtils.AssertTheseDiagnostics(diagnostics,
<errors>
BC31030: Conditional compilation constant '1' is not valid: Identifier expected.

~
BC31030: Conditional compilation constant '2' is not valid: Identifier expected.

~
BC31030: Conditional compilation constant '3' is not valid: Identifier expected.

~
</errors>)

            Assert.True(diagnostics(0).Location.SourceTree.Equals(syntaxTree1))
            Assert.True(diagnostics(1).Location.SourceTree.Equals(syntaxTree2))
            Assert.True(diagnostics(2).Location.SourceTree.Equals(syntaxTree3))
        End Sub

        <Fact>
        Public Sub CompilingCodeWithSameParseOptionsInMultipleSyntaxTreesShouldReportOnlyNonDuplicates()
            Dim dict1 = New Dictionary(Of String, Object)
            dict1.Add("1", Nothing)
            Dim dict2 = New Dictionary(Of String, Object)
            dict2.Add("2", Nothing)

            Dim parseOptions1 = New VisualBasicParseOptions().WithPreprocessorSymbols(dict1)
            Dim parseOptions2 = New VisualBasicParseOptions().WithPreprocessorSymbols(dict2)

            Dim syntaxTree1 = Parse(String.Empty, options:=parseOptions1)
            Dim syntaxTree2 = Parse(String.Empty, options:=parseOptions2)
            Dim syntaxTree3 = Parse(String.Empty, options:=parseOptions2)

            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            Dim compilation = CreateCompilationWithMscorlib40({syntaxTree1, syntaxTree2, syntaxTree3}, options:=options)
            Dim diagnostics = compilation.GetDiagnostics()

            CompilationUtils.AssertTheseDiagnostics(diagnostics,
<errors>
BC31030: Conditional compilation constant '1' is not valid: Identifier expected.

~
BC31030: Conditional compilation constant '2' is not valid: Identifier expected.

~
</errors>)

            Assert.True(diagnostics(0).Location.SourceTree.Equals(syntaxTree1))
            Assert.True(diagnostics(1).Location.SourceTree.Equals(syntaxTree2))
        End Sub

        <Fact>
        Public Sub DiagnosticsInCompilationOptionsParseOptionsAreDedupedWithParseTreesParseOptions()
            Dim dict1 = New Dictionary(Of String, Object)
            dict1.Add("1", Nothing)
            Dim dict2 = New Dictionary(Of String, Object)
            dict2.Add("2", Nothing)

            Dim parseOptions1 = New VisualBasicParseOptions().WithPreprocessorSymbols(dict1)
            Dim parseOptions2 = New VisualBasicParseOptions().WithPreprocessorSymbols(dict2)

            Dim syntaxTree1 = Parse(String.Empty, options:=parseOptions1)
            Dim syntaxTree2 = Parse(String.Empty, options:=parseOptions2)

            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, parseOptions:=parseOptions1)
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime({syntaxTree1, syntaxTree2}, options:=options)
            Dim diagnostics = compilation.GetDiagnostics()

            CompilationUtils.AssertTheseDiagnostics(diagnostics,
<errors>
BC31030: Conditional compilation constant '1' is not valid: Identifier expected.
BC31030: Conditional compilation constant '2' is not valid: Identifier expected.

~
</errors>)

            Assert.Equal("2", diagnostics(0).Arguments(1))
            Assert.True(diagnostics(0).Location.SourceTree.Equals(syntaxTree2)) ' Syntax tree parse options are reported in CompilationStage.Parse

            Assert.Equal("1", diagnostics(1).Arguments(1))
            Assert.Null(diagnostics(1).Location.SourceTree) ' Compilation parse options are reported in CompilationStage.Declare
        End Sub

        <Fact>
        Public Sub DiagnosticsInCompilationOptionsParseOptionsAreReportedSeparately()
            Dim dict1 = New Dictionary(Of String, Object)
            dict1.Add("1", Nothing)
            Dim dict2 = New Dictionary(Of String, Object)
            dict2.Add("2", Nothing)

            Dim parseOptions1 = New VisualBasicParseOptions().WithPreprocessorSymbols(dict1)
            Dim parseOptions2 = New VisualBasicParseOptions().WithPreprocessorSymbols(dict2)

            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, parseOptions:=parseOptions1)

            CompilationUtils.AssertTheseDiagnostics(options.Errors,
<errors>
BC31030: Conditional compilation constant '1' is not valid: Identifier expected.
</errors>)

            Dim syntaxTree = Parse(String.Empty, options:=parseOptions2)
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime({syntaxTree}, options:=options)
            Dim diagnostics = compilation.GetDiagnostics()

            CompilationUtils.AssertTheseDiagnostics(diagnostics,
<errors>
BC31030: Conditional compilation constant '1' is not valid: Identifier expected.
BC31030: Conditional compilation constant '2' is not valid: Identifier expected.

~
</errors>)

            Assert.Equal("2", diagnostics(0).Arguments(1))
            Assert.True(diagnostics(0).Location.SourceTree.Equals(syntaxTree)) ' Syntax tree parse options are reported in CompilationStage.Parse

            Assert.Equal("1", diagnostics(1).Arguments(1))
            Assert.Null(diagnostics(1).Location.SourceTree) ' Compilation parse options are reported in CompilationStage.Declare
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
                        Dim added = declaredSymbolNames.Add(symbol.Name)
                        If Not added Then
                            Dim method = TryCast(symbol, Symbols.MethodSymbol)
                            Assert.NotNull(method)

                            Dim isPartialMethod = method.PartialDefinitionPart IsNot Nothing OrElse
                                                  method.PartialImplementationPart IsNot Nothing
                            Assert.True(isPartialMethod, "Unexpected multiple symbol declared events for same symbol " + symbol.Name)
                        End If
                    Else
                        Dim compilationCompletedEvent = TryCast(compEvent, CompilationUnitCompletedEvent)
                        If compilationCompletedEvent IsNot Nothing Then
                            Assert.True(completedCompilationUnits.Add(compilationCompletedEvent.CompilationUnit.FilePath))
                        End If
                    End If
                End If
            End While

            Return True
        End Function

        <Fact>
        Public Sub TestEventQueueCompletionForEmptyCompilation()
            Dim compilation = CreateCompilationWithMscorlib45(source:=Nothing).WithEventQueue(New AsyncQueue(Of CompilationEvent)())

            ' Force complete compilation event queue
            Dim unused = compilation.GetDiagnostics()

            Assert.True(compilation.EventQueue.IsCompleted)
        End Sub
    End Class
End Namespace
