' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend NotInheritable Class VisualBasicCodeGenerationContextInfo
        Inherits CodeGenerationContextInfo

        Private ReadOnly _options As VisualBasicCodeGenerationOptions
        Private ReadOnly _service As VisualBasicCodeGenerationService

        Public Sub New(context As CodeGenerationContext, options As VisualBasicCodeGenerationOptions, service As VisualBasicCodeGenerationService)
            MyBase.New(context)
            _options = options
            _service = service
        End Sub

        Public Shadows ReadOnly Property Options As VisualBasicCodeGenerationOptions
            Get
                Return _options
            End Get
        End Property

        Public Shadows ReadOnly Property Service As VisualBasicCodeGenerationService
            Get
                Return _service
            End Get
        End Property

        Protected Overrides ReadOnly Property GeneratorImpl As SyntaxGenerator
            Get
                Return _service.LanguageServices.GetRequiredService(Of SyntaxGenerator)
            End Get
        End Property

        Protected Overrides ReadOnly Property OptionsImpl As CodeGenerationOptions
            Get
                Return _options
            End Get
        End Property

        Protected Overrides ReadOnly Property ServiceImpl As ICodeGenerationService
            Get
                Return _service
            End Get
        End Property

        Protected Overrides Function WithContextImpl(value As CodeGenerationContext) As CodeGenerationContextInfo
            Return New VisualBasicCodeGenerationContextInfo(value, _options, _service)
        End Function
    End Class
End Namespace
