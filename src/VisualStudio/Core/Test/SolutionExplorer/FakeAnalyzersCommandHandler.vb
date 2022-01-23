' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Public Class FakeAnalyzersCommandHandler
    Implements IAnalyzersCommandHandler

    Public ReadOnly Property AnalyzerContextMenuController As IContextMenuController Implements IAnalyzersCommandHandler.AnalyzerContextMenuController
        Get
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property AnalyzerFolderContextMenuController As IContextMenuController Implements IAnalyzersCommandHandler.AnalyzerFolderContextMenuController
        Get
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property DiagnosticContextMenuController As IContextMenuController Implements IAnalyzersCommandHandler.DiagnosticContextMenuController
        Get
            Return Nothing
        End Get
    End Property
End Class
