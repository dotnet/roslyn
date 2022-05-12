' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.CodeStyle
Imports System.Runtime.Serialization
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    <DataContract>
    Friend NotInheritable Class VisualBasicSimplifierOptions
        Inherits SimplifierOptions

        Public Shared ReadOnly [Default] As New VisualBasicSimplifierOptions()
    End Class

    Friend Module VisualBasicSimplifierOptionsProviders
        <Extension>
        Friend Function GetVisualBasicSimplifierOptions(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicSimplifierOptions) As VisualBasicSimplifierOptions
            fallbackOptions = If(fallbackOptions, VisualBasicSimplifierOptions.Default)
            Return New VisualBasicSimplifierOptions() With
            {
                .Common = options.GetCommonSimplifierOptions(fallbackOptions.Common)
            }
        End Function
    End Module
End Namespace
