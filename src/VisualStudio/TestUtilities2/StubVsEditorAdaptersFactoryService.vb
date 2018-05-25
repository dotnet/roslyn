' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    <Export(GetType(IVsEditorAdaptersFactoryService))>
    Friend Class StubVsEditorAdaptersFactoryService
        Implements IVsEditorAdaptersFactoryService

        Public Sub SetDataBuffer(bufferAdapter As IVsTextBuffer, dataBuffer As ITextBuffer) Implements IVsEditorAdaptersFactoryService.SetDataBuffer
            Throw New NotImplementedException()
        End Sub

        Public Function CreateVsCodeWindowAdapter(serviceProvider As IServiceProvider) As IVsCodeWindow Implements IVsEditorAdaptersFactoryService.CreateVsCodeWindowAdapter
            Throw New NotImplementedException()
        End Function

        Public Function CreateVsTextBufferAdapter(serviceProvider As IServiceProvider) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferAdapter
            Throw New NotImplementedException()
        End Function

        Public Function CreateVsTextBufferAdapter(serviceProvider As IServiceProvider, contentType As IContentType) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferAdapter
            Throw New NotImplementedException()
        End Function

        Public Function CreateVsTextBufferAdapterForSecondaryBuffer(serviceProvider As IServiceProvider, secondaryBuffer As ITextBuffer) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferAdapterForSecondaryBuffer
            Throw New NotImplementedException()
        End Function

        Public Function CreateVsTextBufferCoordinatorAdapter() As IVsTextBufferCoordinator Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferCoordinatorAdapter
            Throw New NotImplementedException()
        End Function

        Public Function CreateVsTextViewAdapter(serviceProvider As IServiceProvider) As IVsTextView Implements IVsEditorAdaptersFactoryService.CreateVsTextViewAdapter
            Throw New NotImplementedException()
        End Function

        Public Function CreateVsTextViewAdapter(serviceProvider As IServiceProvider, roles As ITextViewRoleSet) As IVsTextView Implements IVsEditorAdaptersFactoryService.CreateVsTextViewAdapter
            Throw New NotImplementedException()
        End Function

        Public Function GetBufferAdapter(textBuffer As ITextBuffer) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.GetBufferAdapter
            Throw New NotImplementedException()
        End Function

        Public Function GetDataBuffer(bufferAdapter As IVsTextBuffer) As ITextBuffer Implements IVsEditorAdaptersFactoryService.GetDataBuffer
            Throw New NotImplementedException()
        End Function

        Public Function GetDocumentBuffer(bufferAdapter As IVsTextBuffer) As ITextBuffer Implements IVsEditorAdaptersFactoryService.GetDocumentBuffer
            Throw New NotImplementedException()
        End Function

        Public Function GetViewAdapter(textView As ITextView) As IVsTextView Implements IVsEditorAdaptersFactoryService.GetViewAdapter
            Throw New NotImplementedException()
        End Function

        Public Function GetWpfTextView(viewAdapter As IVsTextView) As IWpfTextView Implements IVsEditorAdaptersFactoryService.GetWpfTextView
            Throw New NotImplementedException()
        End Function

        Public Function GetWpfTextViewHost(viewAdapter As IVsTextView) As IWpfTextViewHost Implements IVsEditorAdaptersFactoryService.GetWpfTextViewHost
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
