' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports System.Collections.Concurrent

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend Class VisualBasicProjectShimWithServices
        Inherits VisualBasicProject

        Friend Sub New(projectTracker As VisualStudioProjectTracker,
                       compilerHost As IVbCompilerHost,
                       ProjectSystemName As String,
                       Hierarchy As IVsHierarchy,
                       ServiceProvider As IServiceProvider,
                       visualStudioWorkspaceOpt As VisualStudioWorkspaceImpl,
                       hostDiagnosticUpdateSourceOpt As HostDiagnosticUpdateSource)
            MyBase.New(projectTracker,
                       ProjectSystemName,
                       compilerHost,
                       Hierarchy,
                       ServiceProvider,
                       Function(id) New ProjectExternalErrorReporter(id, "BC", ServiceProvider),
                       visualStudioWorkspaceOpt,
                       hostDiagnosticUpdateSourceOpt)
        End Sub

        ' For unit testing
        Friend Sub New(projectTracker As VisualStudioProjectTracker,
               compilerHost As IVbCompilerHost,
               projectSystemName As String,
               hierarchy As IVsHierarchy,
               serviceProvider As IServiceProvider)
            MyBase.New(projectTracker,
                       projectSystemName,
                       compilerHost,
                       hierarchy,
                       serviceProvider,
                       Nothing,
                       Nothing,
                       Nothing)
        End Sub
    End Class
End Namespace
