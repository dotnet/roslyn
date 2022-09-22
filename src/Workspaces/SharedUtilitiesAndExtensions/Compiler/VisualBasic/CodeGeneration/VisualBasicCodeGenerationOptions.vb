' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <DataContract>
    Friend NotInheritable Class VisualBasicCodeGenerationOptions
        Inherits CodeGenerationOptions
        Implements IEquatable(Of VisualBasicCodeGenerationOptions)

        Public Shared ReadOnly [Default] As New VisualBasicCodeGenerationOptions()

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, VisualBasicCodeGenerationOptions))
        End Function

        Public Overloads Function Equals(other As VisualBasicCodeGenerationOptions) As Boolean Implements IEquatable(Of VisualBasicCodeGenerationOptions).Equals
            Return other IsNot Nothing AndAlso
                   Common.Equals(other.Common)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Common.GetHashCode()
        End Function

#If Not CODE_STYLE Then
        Public Overrides Function GetInfo(context As CodeGenerationContext, parseOptions As ParseOptions) As CodeGenerationContextInfo
            Return New VisualBasicCodeGenerationContextInfo(context, Me)
        End Function
#End If
    End Class

    Friend Module VisualBasicCodeGenerationOptionsProviders
        <Extension>
        Public Function GetVisualBasicCodeGenerationOptions(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicCodeGenerationOptions) As VisualBasicCodeGenerationOptions
            Return New VisualBasicCodeGenerationOptions() With
            {
                .Common = options.GetCommonCodeGenerationOptions(fallbackOptions.Common)
            }
        End Function
    End Module
End Namespace
