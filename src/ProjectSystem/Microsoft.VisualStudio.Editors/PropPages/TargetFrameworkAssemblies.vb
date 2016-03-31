Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.Windows.Forms
Imports System.ComponentModel
Imports VSLangProj80
Imports VSLangProj90

Imports Microsoft.VisualStudio.Editors.Common

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Support for retrieving and working with the available target framework assemblies
    '''   for a project.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class TargetFrameworkAssemblies

        ''' <summary>
        ''' Represents a supported target framework assembly.  Can be placed directly into 
        '''   a listbox or combobox (it will show the Description text in the listbox)
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class TargetFramework
            Private m_version As UInteger
            Private m_description As String

            Public Sub New(ByVal version As UInteger, ByVal description As String)
                If description Is Nothing Then
                    Throw New ArgumentNullException("description")
                End If

                m_version = version
                m_description = description
            End Sub

            Public ReadOnly Property Version() As UInteger
                Get
                    Return m_version
                End Get
            End Property

            Public ReadOnly Property Description() As String
                Get
                    Return m_description
                End Get
            End Property

            ''' <summary>
            ''' Provides the text to show inside of a combobox/listbox
            ''' </summary>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Overrides Function ToString() As String
                Return m_description
            End Function
        End Class


        ''' <summary>
        ''' Retrieves the set of target framework assemblies that are supported
        ''' </summary>
        ''' <param name="vsTargetFrameworkAssemblies"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetSupportedTargetFrameworkAssemblies(ByVal vsTargetFrameworkAssemblies As IVsTargetFrameworkAssemblies) As IEnumerable(Of TargetFramework)
            Dim versions As UInteger() = GetSupportedTargetFrameworkAssemblyVersions(vsTargetFrameworkAssemblies)
            Dim targetFrameworks As New List(Of TargetFramework)
            For Each version As UInteger In versions
                targetFrameworks.Add(New TargetFramework(version, GetTargetFrameworkDescriptionFromVersion(vsTargetFrameworkAssemblies, version)))
            Next

            Return targetFrameworks.ToArray()
        End Function

        ''' <summary>
        ''' Retrieve the localized description string for a given target framework
        '''   version number.
        ''' </summary>
        ''' <param name="vsTargetFrameworkAssemblies"></param>
        ''' <param name="version"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GetTargetFrameworkDescriptionFromVersion(ByVal vsTargetFrameworkAssemblies As IVsTargetFrameworkAssemblies, ByVal version As UInteger) As String
            Dim pszDescription As String = Nothing
            VSErrorHandler.ThrowOnFailure(vsTargetFrameworkAssemblies.GetTargetFrameworkDescription(version, pszDescription))
            Return pszDescription
        End Function

        ''' <summary>
        ''' Retrieve the list of assemblies versions (as uint) that are supported
        ''' </summary>
        ''' <param name="vsTargetFrameworkAssemblies"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GetSupportedTargetFrameworkAssemblyVersions(ByVal vsTargetFrameworkAssemblies As IVsTargetFrameworkAssemblies) As UInteger()
            Dim targetFrameworkEnumerator As IEnumTargetFrameworks = Nothing
            VSErrorHandler.ThrowOnFailure(vsTargetFrameworkAssemblies.GetSupportedFrameworks(targetFrameworkEnumerator))
            Dim supportedFrameworks As New List(Of UInteger)
            While True
                Dim rgFrameworks(0) As UInteger
                Dim cReturned As UInteger
                If VSErrorHandler.Failed(targetFrameworkEnumerator.Next(1, rgFrameworks, cReturned)) OrElse cReturned = 0 Then
                    Exit While
                Else
                    supportedFrameworks.Add(rgFrameworks(0))
                End If
            End While

            Return supportedFrameworks.ToArray()
        End Function

    End Class

End Namespace
