' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Friend Class XmlElementTagCorrector
        Implements ICorrector

        Private _waitIndicator As IWaitIndicator
        Private b As ITextBuffer

        Public Sub New(b As ITextBuffer, _waitIndicator As IWaitIndicator)
            Me.b = b
            Me._waitIndicator = _waitIndicator
        End Sub

        Public ReadOnly Property IsDisconnected As Boolean Implements ICorrector.IsDisconnected
            Get
                Return True
            End Get
        End Property

        Public Sub Connect() Implements ICorrector.Connect
        End Sub

        Public Sub Disconnect() Implements ICorrector.Disconnect
        End Sub
    End Class
End Namespace
