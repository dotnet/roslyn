' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Private m_hasSTAThreadAttribute As Boolean = False
        Friend Property HasSTAThreadAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me.m_hasSTAThreadAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me.m_hasSTAThreadAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

#Region "MTAThreadAttribute"
        Private m_hasMTAThreadAttribute As Boolean = False
        Friend Property HasMTAThreadAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me.m_hasMTAThreadAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me.m_hasMTAThreadAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

#Region "DebuggerHiddenAttribute"
        Private m_isPropertyAccessorWithDebuggerHiddenAttribute As Boolean = False
        Friend Property IsPropertyAccessorWithDebuggerHiddenAttribute As Boolean
            Get
                VerifySealed(expected:=True)
                Return Me.m_isPropertyAccessorWithDebuggerHiddenAttribute
            End Get
            Set(value As Boolean)
                VerifySealed(expected:=False)
                Me.m_isPropertyAccessorWithDebuggerHiddenAttribute = value
                SetDataStored()
            End Set
        End Property
#End Region

    End Class
End Namespace