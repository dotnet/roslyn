' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend Class VisualBasicProjectShimWithServices
        Inherits VisualBasicProject

        Friend Sub New(projectTracker As VisualStudioProjectTracker,
                       compilerHost As IVbCompilerHost,
                       ProjectSystemName As String,
                       Hierarchy As IVsHierarchy,
                       ServiceProvider As IServiceProvider,
                       visualStudioWorkspaceOpt As VisualStudioWorkspaceImpl,
                       hostDiagnosticUpdateSourceOpt As HostDiagnosticUpdateSource,
                       commandLineParserServiceOpt As ICommandLineParserService)
            MyBase.New(projectTracker,
                       ProjectSystemName,
                       compilerHost,
                       Hierarchy,
                       ServiceProvider,
                       Function(id) New ProjectExternalErrorReporter(id, "BC", ServiceProvider),
                       visualStudioWorkspaceOpt,
                       hostDiagnosticUpdateSourceOpt,
                       commandLineParserServiceOpt)
        End Sub

        ' For unit testing
        Friend Sub New(projectTracker As VisualStudioProjectTracker,
               compilerHost As IVbCompilerHost,
               projectSystemName As String,
               hierarchy As IVsHierarchy,
               serviceProvider As IServiceProvider,
               commandLineParserService As ICommandLineParserService)
            MyBase.New(projectTracker,
                       projectSystemName,
                       compilerHost,
                       hierarchy,
                       serviceProvider,
                       commandLineParserServiceOpt:=commandLineParserService)
        End Sub
    End Class
End Namespace
