' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Roslyn.Test.Utilities.TestGenerators

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GeneratorDriverTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Single_File_Is_Added()
            Dim source = "
Public Class C

End Class
"
            Dim generatorSource = "
Public Class GeneratedClass

End Class
"
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)
            compilation.VerifyDiagnostics()
            Assert.Single(compilation.SyntaxTrees)

            Dim testGenerator As SingleFileTestGenerator = New SingleFileTestGenerator(generatorSource)
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count())
            Assert.NotEqual(compilation, outputCompilation)
        End Sub

    End Class

End Namespace
