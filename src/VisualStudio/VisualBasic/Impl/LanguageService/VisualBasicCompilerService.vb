' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    ' The object behind the legacy SID_SVisualBasicCompiler service. The property page for WinForms projects gets
    ' this service and casts it to IVBEntryPointProvider. That interface is redefined in managed code, so the object
    ' has to be a COM aggregate for the cast to go through COM QueryInterface rather than managed type identity.
    Friend NotInheritable Class VisualBasicCompilerService
        Implements IVbCompilerService
        Implements IVBEntryPointProvider

        Private ReadOnly _componentModel As IComponentModel

        Public Sub New(componentModel As IComponentModel)
            _componentModel = componentModel
        End Sub

        Public Function GetFormEntryPointsList(
                pHierarchy As Object,
                cItems As Integer,
                <Out> bstrList() As String,
                <Out> pcActualItems As IntPtr) As Integer Implements IVBEntryPointProvider.GetFormEntryPointsList

            Dim workspace = _componentModel.GetService(Of VisualStudioWorkspace)()
            Dim hierarchy = CType(pHierarchy, IVsHierarchy)

            For Each projectId In workspace.CurrentSolution.ProjectIds
                Dim projectHierarchy = workspace.GetHierarchy(projectId)
                If hierarchy Is projectHierarchy Then
                    Dim compilation = workspace.CurrentSolution.GetProject(projectId).GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)

                    VisualBasicProject.GetEntryPointsWorker(compilation, cItems, bstrList, pcActualItems, findFormsOnly:=True)
                End If
            Next

            Return VSConstants.S_OK
        End Function
    End Class
End Namespace
