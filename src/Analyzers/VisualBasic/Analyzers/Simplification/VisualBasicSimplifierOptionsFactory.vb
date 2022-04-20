﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Friend Module VisualBasicSimplifierOptionsFactory
        <Extension>
        Public Function GetVisualBasicSimplifierOptions(options As AnalyzerOptions, syntaxTree As SyntaxTree) As VisualBasicSimplifierOptions
            Dim configOptions = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree)
            Dim ideOptions = options.GetIdeOptions()

            Return VisualBasicSimplifierOptions.Create(configOptions, DirectCast(ideOptions.SimplifierOptions, VisualBasicSimplifierOptions))
        End Function
    End Module
End Namespace

