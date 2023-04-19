' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        <Obsolete("ClosedFileDiagnostics has been deprecated")>
        Public Property ClosedFileDiagnostics As Boolean
            Get
                Return False
            End Get
            Set(value As Boolean)
            End Set
        End Property

        <Obsolete("BasicClosedFileDiagnostics has been deprecated")>
        Public Property BasicClosedFileDiagnostics As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
