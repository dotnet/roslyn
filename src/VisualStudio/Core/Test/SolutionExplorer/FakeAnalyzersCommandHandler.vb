' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
