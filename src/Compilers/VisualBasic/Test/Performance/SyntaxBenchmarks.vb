Imports Microsoft.Xunit.Performance

Namespace Microsoft.CodeAnalysis.VisualBasic.PerformanceTests
    Public Class SyntaxBenchmarks
        <Benchmark>
        Public Sub EmptyParse()
            Benchmark.Iterate(Sub()
                                  Dim tree = VisualBasicSyntaxTree.ParseText("")
                              End Sub)
        End Sub

        <Benchmark>
        Public Sub HelloWorldParse()
            Const helloVb = "Imports System.Console

Module Hello
    Sub Main()
        WriteLine(""Hello, world!"")
    End Sub
End Module
"
            Benchmark.Iterate(Sub()
                                  Dim tree = VisualBasicSyntaxTree.ParseText(helloVb)
                              End Sub)
        End Sub
    End Class
End Namespace
