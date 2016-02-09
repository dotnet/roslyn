' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.Xunit.Performance
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.PerformanceTests

    Public Class CalibrationBenchmarks
        <Benchmark>
        Public Sub DoNothing()
            Benchmark.Iterate(Sub()
                              End Sub)
        End Sub

        <Benchmark>
        <InlineData(100)>
        Public Sub Sleep(durationInMilliseconds As Integer)
            Benchmark.Iterate(Sub()
                                  Threading.Thread.Sleep(durationInMilliseconds)
                              End Sub)
        End Sub
    End Class
End Namespace
