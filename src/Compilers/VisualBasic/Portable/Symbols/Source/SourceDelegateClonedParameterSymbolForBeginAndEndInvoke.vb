' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SourceDelegateClonedParameterSymbolForBeginAndEndInvoke
        Inherits SourceClonedParameterSymbol

        Public Sub New(originalParam As SourceParameterSymbol, newOwner As MethodSymbol, ordinal As Integer)
            MyBase.New(originalParam, newOwner, ordinal)
            Debug.Assert(Not originalParam.IsOptional)
        End Sub

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                ' This parameter cannot be optional. Hence, this property shouldn't be accessed.
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                ' This parameter cannot be optional. Hence, this property shouldn't be accessed.
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                ' This parameter cannot be optional. Hence, this property shouldn't be accessed.
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
            Get
                ' This parameter cannot be optional. Hence, this property shouldn't be accessed.
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property
    End Class
End Namespace
