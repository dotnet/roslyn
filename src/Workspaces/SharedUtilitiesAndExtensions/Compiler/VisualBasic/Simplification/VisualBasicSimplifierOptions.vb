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
        Implements IEquatable(Of VisualBasicSimplifierOptions)

        Public Shared ReadOnly [Default] As New VisualBasicSimplifierOptions()

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, VisualBasicSimplifierOptions))
        End Function

        Public Overloads Function Equals(other As VisualBasicSimplifierOptions) As Boolean Implements IEquatable(Of VisualBasicSimplifierOptions).Equals
            Return other IsNot Nothing AndAlso
                   Common.Equals(other.Common)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Common.GetHashCode()
        End Function
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
