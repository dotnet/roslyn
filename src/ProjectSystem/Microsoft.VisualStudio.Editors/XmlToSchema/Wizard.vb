Option Infer On
Imports System.IO
Imports System.Windows.Forms.Design
Imports EnvDTE
Imports Microsoft.VisualStudio.TemplateWizard

Namespace Microsoft.VisualStudio.Editors.XmlToSchema
    Public NotInheritable Class Wizard
        Implements IWizard

        Public Sub BeforeOpeningFile(ByVal projectItem As ProjectItem) Implements IWizard.BeforeOpeningFile
        End Sub

        Public Sub ProjectFinishedGenerating(ByVal project As Project) Implements IWizard.ProjectFinishedGenerating
        End Sub

        Public Sub ProjectItemFinishedGenerating(ByVal projectItem As ProjectItem) Implements IWizard.ProjectItemFinishedGenerating
        End Sub

        Public Sub RunFinished() Implements IWizard.RunFinished
        End Sub

        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")> _
        Public Sub RunStarted(ByVal automationObject As Object, _
                              ByVal replacementsDictionary As Dictionary(Of String, String), _
                              ByVal runKind As WizardRunKind, _
                              ByVal customParams() As Object) Implements IWizard.RunStarted
            If automationObject Is Nothing OrElse replacementsDictionary Is Nothing Then
                Return
            End If
            Try
                Dim dte = CType(automationObject, DTE)

                Dim activeProjects As Array = TryCast(dte.ActiveSolutionProjects, Array)
                If activeProjects Is Nothing OrElse activeProjects.Length = 0 Then
                    ShowWarning(SR.XmlToSchema_NoProjectSelected)
                    Return
                End If

                Dim acitveProject = TryCast(activeProjects.GetValue(0), Project)
                If acitveProject Is Nothing Then
                    ShowWarning(SR.XmlToSchema_NoProjectSelected)
                    Return
                End If

                Dim savePath = Path.GetDirectoryName(acitveProject.FullName)
                If Not Directory.Exists(savePath) Then
                    'For Website projects targeting IIS/IIS Express activeProject.FullName returns http path which is not a Valid Directory.
                    'Instead we will use acitveProject.Properties.Item(FullPath).Value to give a chance for Website projects(IIS/IIS Express)
                    'to see if valid directory exists before showing warning. We will keep this logic inside try/catch block
                    'since for projects which dont support "FullPath" property exception can be thrown.
                    Try
                        savePath = acitveProject.Properties.Item("FullPath").Value.ToString()
                    Catch 'Eat any exception
                    End Try
                    If Not Directory.Exists(savePath) Then
                        ShowWarning(String.Format(SR.XmlToSchema_InvalidProjectPath, savePath))
                        Return
                    End If
                End If

                Dim fileName = replacementsDictionary("$rootname$")
                If String.IsNullOrEmpty(fileName) Then
                    ShowWarning(SR.XmlToSchema_InvalidEmptyItemName)
                    Return
                End If

                Dim inputForm As New InputXmlForm(acitveProject, savePath, fileName)
                inputForm.ServiceProvider = Common.ShellUtil.GetServiceProvider(dte)
                Dim uiService As IUIService = CType(inputForm.ServiceProvider.GetService(GetType(IUIService)), IUIService)
                uiService.ShowDialog(inputForm)

            Catch ex As Exception
                If FilterException(ex) Then
                    ShowWarning(ex)
                Else
                    Throw
                End If
            End Try
        End Sub

        Public Function ShouldAddProjectItem(ByVal filePath As String) As Boolean Implements IWizard.ShouldAddProjectItem
            Return False
        End Function
    End Class
End Namespace
