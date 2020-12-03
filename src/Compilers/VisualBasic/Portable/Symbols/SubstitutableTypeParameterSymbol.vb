' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A SubstitutableTypeParameterSymbol represents a definition that is subject to alpha-renaming, 
    ''' which results in <see cref="SubstitutedTypeParameterSymbol"/>.
    ''' 
    ''' The main purpose of this type is to share equality implementation that ensures symmetry
    ''' across both of these types.
    ''' </summary>
    Friend MustInherit Class SubstitutableTypeParameterSymbol
        Inherits TypeParameterSymbol

        Public NotOverridable Overrides Function GetHashCode() As Integer
            Return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Me)
        End Function

        Public NotOverridable Overrides Function Equals(other As TypeSymbol, comparison As TypeCompareKind) As Boolean
            If other Is Me Then
                Return True
            End If

            If other Is Nothing OrElse (comparison And TypeCompareKind.AllIgnoreOptionsForVB) = 0 Then
                Return False
            End If

            If other.OriginalDefinition IsNot Me Then
                Return False
            End If

            ' Delegate comparison to the other type to ensure symmetry
            Debug.Assert(TypeOf other Is SubstitutedTypeParameterSymbol)
            Return other.Equals(Me, comparison)
        End Function

        Public NotOverridable Overrides ReadOnly Property OriginalDefinition As TypeParameterSymbol
            Get
                Return Me
            End Get
        End Property
    End Class
End Namespace
