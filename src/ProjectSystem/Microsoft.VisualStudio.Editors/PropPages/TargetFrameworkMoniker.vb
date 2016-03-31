Imports EnvDTE
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Collections.Generic
Imports System.Runtime.Versioning

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Represents a target framework moniker and can be placed into a control
    ''' </summary>
    Class TargetFrameworkMoniker

        ''' <summary>
        ''' Stores the target framework moniker
        ''' </summary>
        Private m_Moniker As String

        ''' <summary>
        ''' Stores the display name of the target framework moniker
        ''' </summary>
        Private m_DisplayName As String

        ''' <summary>
        ''' Constructor that uses the target framework moniker and display name provided by DTAR
        ''' </summary>
        Public Sub New(ByVal moniker As String, ByVal displayName As String)

            m_Moniker = moniker
            m_DisplayName = displayName

        End Sub

        ''' <summary>
        ''' Gets the target framework moniker
        ''' </summary>
        Public ReadOnly Property Moniker() As String
            Get
                Return m_Moniker
            End Get
        End Property

        ''' <summary>
        ''' Use the display name provided by DTAR for the string display
        ''' </summary>
        Public Overrides Function ToString() As String
            Return m_DisplayName
        End Function

        ''' <summary>
        ''' Gets the supported target framework monikers from DTAR
        ''' </summary>
        ''' <param name="vsFrameworkMultiTargeting"></param>
        Public Shared Function GetSupportedTargetFrameworkMonikers( _
            ByVal vsFrameworkMultiTargeting As IVsFrameworkMultiTargeting, _
            ByVal currentProject As Project) As IEnumerable(Of TargetFrameworkMoniker)

            Dim supportedFrameworksArray As Array = Nothing
            VSErrorHandler.ThrowOnFailure(vsFrameworkMultiTargeting.GetSupportedFrameworks(supportedFrameworksArray))

            Dim targetFrameworkMonikerProperty As [Property] = currentProject.Properties.Item(ApplicationPropPage.Const_TargetFrameworkMoniker)
            Dim currentTargetFrameworkMoniker As String = CStr(targetFrameworkMonikerProperty.Value)
            Dim currentFrameworkName As New FrameworkName(currentTargetFrameworkMoniker)

            Dim supportedTargetFrameworkMonikers As New List(Of TargetFrameworkMoniker)
            Dim hashSupportedTargetFrameworkMonikers As New HashSet(Of String)

            ' Determine if the project is a WAP (Web Application Project).
            Dim isWebProject As Boolean = False
            For i As Integer = 1 To currentProject.Properties.Count
                If currentProject.Properties.Item(i).Name.StartsWith("WebApplication.") Then
                    isWebProject = True
                    Exit For
                End If
            Next

            ' UNDONE: DTAR may currently send back duplicate monikers, so explicitly filter them out for now
            For Each moniker As String In supportedFrameworksArray
                If hashSupportedTargetFrameworkMonikers.Add(moniker) Then

                    Dim frameworkName As New FrameworkName(moniker)

                    ' Filter out frameworks with a different identifier since they are not applicable to the current project type
                    If String.Compare(frameworkName.Identifier, currentFrameworkName.Identifier, StringComparison.OrdinalIgnoreCase) = 0 Then

                        If isWebProject Then

                            ' Web projects don't support profiles when targeting below 4.0, so filter those out
                            If frameworkName.Version.Major < 4 AndAlso
                               Not String.IsNullOrEmpty(frameworkName.Profile) Then
                                Continue For
                            End If

                            ' For web projects, filter out frameworks that don't contain System.Web (e.g. client profiles).
                            Dim systemWebPath As String = Nothing
                            If VSErrorHandler.Failed(vsFrameworkMultiTargeting.ResolveAssemblyPath( _
                                  "System.Web.dll", _
                                  moniker, _
                                  systemWebPath)) OrElse _
                               String.IsNullOrEmpty(systemWebPath) Then
                                Continue For
                            End If
                        End If

                        ' Use DTAR to get the display name corresponding to the moniker
                        Dim displayName As String = ""
                        VSErrorHandler.ThrowOnFailure(vsFrameworkMultiTargeting.GetDisplayNameForTargetFx(moniker, displayName))

                        supportedTargetFrameworkMonikers.Add(New TargetFrameworkMoniker(moniker, displayName))

                    End If
                End If
            Next

            Return supportedTargetFrameworkMonikers

        End Function
    End Class

End Namespace
