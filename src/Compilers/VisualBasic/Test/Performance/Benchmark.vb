' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.PerformanceTests

    ''' <summary>
    ''' Shadows <see cref="Xunit.Performance.Benchmark"/> to provider the <see cref="Iterate(Action)"/> method.
    ''' This is a stop-gap until we can upgrade to a version of Microsoft.DotNet.xunit.performance that has this method.
    ''' </summary>
    Friend Module Benchmark

        Public Sub Iterate(action As Action)
            For Each iteration In Xunit.Performance.Benchmark.Iterations
                Using iteration.StartMeasurement
                    action()
                End Using
            Next
        End Sub

    End Module

End Namespace
