' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.Xunit.Performance
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests

Namespace Microsoft.CodeAnalysis.VisualBasic.PerformanceTests
    Public Class CompilationBenchmarks
        Inherits BasicTestBase

        <Benchmark>
        Public Sub EmptyCompilation()
            Benchmark.Iterate(Sub()
                                  CreateVisualBasicCompilation(code:=String.Empty)
                              End Sub)
        End Sub

        <Benchmark>
        Public Sub CompileHelloWorld()
            Const helloWorldBasicSource = "Imports System.Console

Module Hello
    Sub Main()
        WriteLine(""Hello, world!"")
    End Sub
End Module
"

            Benchmark.Iterate(Sub()
                                  Dim compilation = CreateVisualBasicCompilation(helloWorldBasicSource)
                                  Dim errors = compilation.GetDiagnostics()
                              End Sub)
        End Sub

    End Class

End Namespace
