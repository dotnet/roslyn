' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <DataContract>
    Friend NotInheritable Class VisualBasicCodeGenerationOptions
        Inherits CodeGenerationOptions

        Public Sub New()
            MyBase.New()
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicCodeGenerationOptions()

        Public Overrides Function GetInfo(context As CodeGenerationContext, parseOptions As ParseOptions) As CodeGenerationContextInfo
            Return New VisualBasicCodeGenerationContextInfo(context, Me)
        End Function
    End Class
End Namespace
