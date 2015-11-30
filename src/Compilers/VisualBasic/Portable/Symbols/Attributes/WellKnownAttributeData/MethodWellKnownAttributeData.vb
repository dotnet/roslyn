' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                Return _hasSTAThreadAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                _hasSTAThreadAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

#Region "MTAThreadAttribute"
        Private _hasMTAThreadAttribute As Boolean = False
        Friend Property HasMTAThreadAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return _hasMTAThreadAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                _hasMTAThreadAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

#Region "DebuggerHiddenAttribute"
        Private _isPropertyAccessorWithDebuggerHiddenAttribute As Boolean = False
        Friend Property IsPropertyAccessorWithDebuggerHiddenAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return _isPropertyAccessorWithDebuggerHiddenAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                _isPropertyAccessorWithDebuggerHiddenAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

    End Class
End Namespace
