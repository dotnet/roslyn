' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend NotInheritable Class VisualBasicCodeGenerationContextInfo
        Inherits CodeGenerationContextInfo

        Private ReadOnly _options As VisualBasicCodeGenerationOptions

        Public Sub New(context As CodeGenerationContext, options As VisualBasicCodeGenerationOptions)
            MyBase.New(context)
            _options = options
        End Sub

        Public Shadows ReadOnly Property Options As VisualBasicCodeGenerationOptions
            Get
                Return _options
            End Get
        End Property

        Protected Overrides ReadOnly Property OptionsImpl As CodeGenerationOptions
            Get
                Return _options
            End Get
        End Property

        Protected Overrides Function WithContextImpl(value As CodeGenerationContext) As CodeGenerationContextInfo
            Return New VisualBasicCodeGenerationContextInfo(value, Options)
        End Function
    End Class
End Namespace
