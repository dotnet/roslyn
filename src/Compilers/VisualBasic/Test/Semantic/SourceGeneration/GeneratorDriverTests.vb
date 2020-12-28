' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities.TestGenerators

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GeneratorDriverTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Single_File_Is_Added()

            Dim generatorSource = "
Public Class GeneratedClass

End Class
"
            Dim parseOptions = TestOptions.Regular
            Dim compilation = GetCompilation(parseOptions)
            Dim testGenerator As SingleFileTestGenerator = New SingleFileTestGenerator(generatorSource)
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count())
            Assert.NotEqual(compilation, outputCompilation)
        End Sub

        <Fact>
        Public Sub Can_Access_Additional_Files()

            Dim additionalText = New InMemoryAdditionalText("a\\file1.cs", "Hello World")

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As CallbackGenerator = New CallbackGenerator(Sub(i)
                                                                           End Sub,
                                                                           Sub(e) Assert.Equal("Hello World", e.AdditionalFiles.First().GetText().ToString()))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator),
                                                                              additionalTexts:=ImmutableArray.Create(Of AdditionalText)(additionalText),
                                                                              parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()
        End Sub

        <Fact>
        Public Sub Generator_Can_Be_Written_In_Visual_Basic()

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As VBGenerator = New VBGenerator()
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count())
            Assert.NotEqual(compilation, outputCompilation)
        End Sub

        <Fact>
        Public Sub Generator_Can_See_Syntax()

            Dim source = "
Imports System
Namespace ANamespace
    Public Class AClass
        Public Sub AMethod(p as String)
            Throw New InvalidOperationException()
        End Sub
    End Class
End Namespace
"

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions, source)
            Dim testGenerator As VBGenerator = New VBGenerator()
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(23, testGenerator._receiver._nodes.Count)
            Assert.IsType(GetType(CompilationUnitSyntax), testGenerator._receiver._nodes(0))
            Assert.IsType(GetType(ClassStatementSyntax), testGenerator._receiver._nodes(8))
            Assert.IsType(GetType(ThrowStatementSyntax), testGenerator._receiver._nodes(16))
            Assert.IsType(GetType(EndBlockStatementSyntax), testGenerator._receiver._nodes(22))

        End Sub

        <Fact>
        Public Sub Exception_During_Init()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As CallbackGenerator = New CallbackGenerator(Sub(i) Throw New Exception("Init Exception"),
                                                                           Sub(e)
                                                                           End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify(
                    Diagnostic("BC42501").WithArguments("CallbackGenerator", "Exception", "Init Exception").WithLocation(1, 1)
            )
        End Sub

        <Fact>
        Public Sub Exception_During_Execute()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As CallbackGenerator = New CallbackGenerator(Sub(i)
                                                                           End Sub,
                                                                           Sub(e) Throw New Exception("Generate Exception"))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify(
                    Diagnostic("BC42502").WithArguments("CallbackGenerator", "Exception", "Generate Exception").WithLocation(1, 1)
            )

        End Sub

        <Fact>
        Public Sub Exception_During_SyntaxWalk()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As VBGenerator = New VBGenerator()
            testGenerator._receiver._throw = True

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify(
                Diagnostic("BC42502").WithArguments("VBGenerator", "Exception", "Syntax Walk").WithLocation(1, 1)
            )
        End Sub

        Shared Function GetCompilation(parseOptions As VisualBasicParseOptions, Optional source As String = "") As Compilation
            If (String.IsNullOrWhiteSpace(source)) Then
                source = "
Public Class C
End Class
"
            End If

            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)
            compilation.VerifyDiagnostics()
            Assert.Single(compilation.SyntaxTrees)

            Return compilation
        End Function

    End Class


    <Generator(LanguageNames.VisualBasic)>
    Friend Class VBGenerator
        Implements ISourceGenerator

        Public _receiver As Receiver = New Receiver()

        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            context.RegisterForSyntaxNotifications(Function() _receiver)
        End Sub

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
            context.AddSource("source.vb", "
Public Class D
End Class
")
        End Sub

        Class Receiver
            Implements ISyntaxReceiver

            Public _throw As Boolean

            Public _nodes As List(Of SyntaxNode) = New List(Of SyntaxNode)()

            Public Sub OnVisitSyntaxNode(syntaxNode As SyntaxNode) Implements ISyntaxReceiver.OnVisitSyntaxNode
                If (_throw) Then
                    Throw New Exception("Syntax Walk")
                End If
                _nodes.Add(syntaxNode)
            End Sub
        End Class

    End Class


End Namespace
