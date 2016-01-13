' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Xunit

Public Class Compilations

    <Fact>
    Sub EndToEndCompileAndRun()
        Dim expression = "6 * 7"
        Dim code =
<code>
Public Module Calculator
    Public Function Evaluate() As Object
        Return $
    End Function
End Module
</code>.GetCode().Replace("$", expression)

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim comp = VisualBasicCompilation.Create(
            "calc.dll",
            options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees:={tree},
            references:={MetadataReference.CreateFromFile(GetType(Object).Assembly.Location),
                        MetadataReference.CreateFromFile(GetType(CompilerServices.StandardModuleAttribute).Assembly.Location)})

        Dim compiledAssembly As Assembly
        Using stream = New MemoryStream()
            Dim compileResult = comp.Emit(stream)
            Assert.True(compileResult.Success)
            compiledAssembly = Assembly.Load(stream.GetBuffer())
        End Using

        Dim calculator = compiledAssembly.GetType("Calculator")
        Dim evaluate = calculator.GetMethod("Evaluate")
        Dim answer = evaluate.Invoke(Nothing, Nothing).ToString()
        Assert.Equal("42", answer)
    End Sub

    <Fact>
    Sub GetErrorsAndWarnings()
        Dim code =
<code>
Module Module1
    Function Main() As Integer
    End Function
End Module
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim comp = VisualBasicCompilation.Create(
            "program.exe",
            syntaxTrees:={tree},
            references:={MetadataReference.CreateFromFile(GetType(Object).Assembly.Location),
                        MetadataReference.CreateFromFile(GetType(CompilerServices.StandardModuleAttribute).Assembly.Location)})

        Dim errorsAndWarnings = comp.GetDiagnostics()
        Assert.Equal(1, errorsAndWarnings.Count())

        Dim err As Diagnostic = errorsAndWarnings.First()
        Assert.Equal("Function 'Main' doesn't return a value on all code paths. Are you missing a 'Return' statement?", err.GetMessage(CultureInfo.InvariantCulture))

        Dim errorLocation = err.Location
        Assert.Equal(12, errorLocation.SourceSpan.Length)

        Dim programText = errorLocation.SourceTree.GetText()
        Assert.Equal("End Function", programText.ToString(errorLocation.SourceSpan))

        Dim span = err.Location.GetLineSpan()
        Assert.Equal(4, span.StartLinePosition.Character)
        Assert.Equal(2, span.StartLinePosition.Line)
    End Sub
End Class
