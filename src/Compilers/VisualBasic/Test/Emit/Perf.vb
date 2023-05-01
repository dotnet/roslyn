' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
    Public Class Perf : Inherits BasicTestBase
        <Fact()>
        Public Sub Test()
            ' This test ensures that our perf benchmark code compiles without problems.
            ' Benchmark code can be found in the following file under the 
            ' "CompilerTestResources" project that is part of Roslyn.sln -
            '      $/Roslyn/Main/Open/Compilers/Test/Resources/Core/PerfTests/VBPerfTest.vb

            ' You can also use VS's "Navigate To" feature to find the above file easily -
            ' Just hit "Ctrl + ," and type "VBPerfTest.vb" in the dialog that pops up.

            ' Please note that if this test fails, it is likely because of a bug in the
            ' *product* and not in the *test* / *benchmark code* :)
            ' The benchmark code has been verified to compile fine against Dev10.
            ' So if the test fails we should fix the product bug that is causing the failure
            ' as opposed to 'fixing' the test by updating the benchmark code.

            ' If you absolutely need to change the benchmark code - PLEASE SHOOT A MAIL TO SHYAM (GNAMBOO)
            ' so that he can apply the same changes to the copy of this benchmark code that is used in the perf test.
            CompileAndVerify(<compilation>
                                 <file name="VBPerfTest.vb">
                                     <%= TestResources.PerfTests.VBPerfTest %>
                                 </file>
                             </compilation>, references:={TestMetadata.Net40.SystemCore}).VerifyDiagnostics()
        End Sub
    End Class
End Namespace
