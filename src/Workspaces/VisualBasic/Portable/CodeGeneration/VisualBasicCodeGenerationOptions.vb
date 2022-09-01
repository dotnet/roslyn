' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend NotInheritable Class VisualBasicCodeGenerationOptions
        Inherits CodeGenerationOptions

        Private ReadOnly _preferences As VisualBasicCodeGenerationPreferences

        Public Sub New(context As CodeGenerationContext, preferences As VisualBasicCodeGenerationPreferences)
            MyBase.New(context)
            _preferences = preferences
        End Sub

        Public Shadows ReadOnly Property Preferences As VisualBasicCodeGenerationPreferences
            Get
                Return _preferences
            End Get
        End Property

        Protected Overrides ReadOnly Property PreferencesImpl As CodeGenerationPreferences
            Get
                Return _preferences
            End Get
        End Property

        Protected Overrides Function WithContextImpl(value As CodeGenerationContext) As CodeGenerationOptions
            Return New VisualBasicCodeGenerationOptions(value, Preferences)
        End Function
    End Class
End Namespace
