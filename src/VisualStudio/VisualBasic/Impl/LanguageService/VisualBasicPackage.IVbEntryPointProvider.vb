' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    Partial Friend Class VisualBasicPackage
        Implements IVBEntryPointProvider

        Public Function GetFormEntryPointsList(
                                         <[In]> pHierarchy As Object,
                                                cItems As Integer,
                                          <Out> bstrList() As String,
                                          <Out> pcActualItems As IntPtr
                                              ) As Integer Implements IVBEntryPointProvider.GetFormEntryPointsList

            Dim visualStudioWorkspace = ComponentModel.GetService(Of VisualStudioWorkspaceImpl)()
            Dim hierarchy = CType(pHierarchy, IVsHierarchy)

            Dim projects = visualStudioWorkspace.CurrentSolution.ProjectIds
            For Each project In projects
                Dim hostProject = visualStudioWorkspace.GetHostProject(project)
                If hostProject?.Hierarchy IsNot hierarchy Then Continue For
                Dim vbProject = TryCast(hostProject, VisualBasicProject)

                If vbProject IsNot Nothing Then vbProject.GetEntryPointsWorker(cItems, bstrList, pcActualItems, findFormsOnly:=True)
            Next

            Return VSConstants.S_OK
        End Function
    End Class
End Namespace
