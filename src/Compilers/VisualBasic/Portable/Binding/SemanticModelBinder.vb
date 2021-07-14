' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Returns True for <see cref="Binder.IsSemanticModelBinder"/>
    ''' </summary>
    Friend Class SemanticModelBinder
        Inherits Binder

        Private ReadOnly _ignoreAccessibility As Boolean = False

        Protected Sub New(containingBinder As Binder, Optional ignoreAccessibility As Boolean = False)
            MyBase.New(containingBinder)

            _ignoreAccessibility = ignoreAccessibility
        End Sub

        Public Shared Function Mark(binder As Binder, Optional ignoreAccessibility As Boolean = False) As Binder
            Return If(
                binder.IsSemanticModelBinder AndAlso binder.IgnoresAccessibility = ignoreAccessibility,
                binder,
                New SemanticModelBinder(binder, ignoreAccessibility))
        End Function

        Friend Overrides Function BinderSpecificLookupOptions(ByVal options As LookupOptions) As LookupOptions
            If (_ignoreAccessibility) Then
                Return MyBase.BinderSpecificLookupOptions(options) Or LookupOptions.IgnoreAccessibility
            Else
                Return MyBase.BinderSpecificLookupOptions(options)
            End If
        End Function

        Public NotOverridable Overrides ReadOnly Property IsSemanticModelBinder As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace
