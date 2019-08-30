' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Venus
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Guid(Guids.VisualBasicLanguageServiceIdString)>
    Partial Friend Class VisualBasicLanguageService
        Inherits AbstractLanguageService(Of VisualBasicPackage, VisualBasicLanguageService)

        Public Sub New(package As VisualBasicPackage)
            MyBase.New(package)
        End Sub

        Protected Overrides ReadOnly Property DebuggerLanguageId As Guid
            Get
                Return Guids.VisualBasicDebuggerLanguageId
            End Get
        End Property

        Public Overrides ReadOnly Property LanguageServiceId As Guid
            Get
                Return Guids.VisualBasicLanguageServiceId
            End Get
        End Property

        Protected Overrides ReadOnly Property ContentTypeName As String
            Get
                Return ContentTypeNames.VisualBasicContentType
            End Get
        End Property

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return BasicVSResources.Microsoft_Visual_Basic
            End Get
        End Property

        Protected Overrides ReadOnly Property RoslynLanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Function CreateContext(
            view As IWpfTextView,
            vsTextView As IVsTextView,
            debuggerBuffer As IVsTextLines,
            subjectBuffer As ITextBuffer,
            currentStatementSpan() As Microsoft.VisualStudio.TextManager.Interop.TextSpan
        ) As AbstractDebuggerIntelliSenseContext

            Return New VisualBasicDebuggerIntelliSenseContext(
                view,
                vsTextView,
                debuggerBuffer,
                subjectBuffer,
                currentStatementSpan,
                Me.Package.ComponentModel,
                Me.SystemServiceProvider)
        End Function

        Protected Overrides Function CreateContainedLanguage(
            bufferCoordinator As IVsTextBufferCoordinator,
            project As VisualStudioProject,
            hierarchy As IVsHierarchy,
            itemid As UInteger
        ) As IVsContainedLanguage

            Return New VisualBasicContainedLanguage(
                bufferCoordinator,
                Me.Package.ComponentModel,
                project,
                hierarchy,
                itemid,
                Me, SourceCodeKind.Regular)
        End Function
    End Class
End Namespace
