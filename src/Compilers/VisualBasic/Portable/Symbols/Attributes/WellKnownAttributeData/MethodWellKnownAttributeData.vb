' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Information decoded from well-known custom attributes applied on a method.
    ''' </summary>
    Friend Class MethodWellKnownAttributeData
        Inherits CommonMethodWellKnownAttributeData

        Public Sub New()
            MyBase.New(preserveSigFirstWriteWins:=True)
        End Sub

#Region "STAThreadAttribute"
        Private _hasSTAThreadAttribute As Boolean = False
        Friend Property HasSTAThreadAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._hasSTAThreadAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._hasSTAThreadAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

#Region "MTAThreadAttribute"
        Private _hasMTAThreadAttribute As Boolean = False
        Friend Property HasMTAThreadAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._hasMTAThreadAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._hasMTAThreadAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

#Region "DebuggerHiddenAttribute"
        Private _isPropertyAccessorWithDebuggerHiddenAttribute As Boolean = False
        Friend Property IsPropertyAccessorWithDebuggerHiddenAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me._isPropertyAccessorWithDebuggerHiddenAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me._isPropertyAccessorWithDebuggerHiddenAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

    End Class
End Namespace
