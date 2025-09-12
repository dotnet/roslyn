' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Threading

Namespace Microsoft.CodeAnalysis.Editor.UnitTests
    <Export(GetType(IVsService(Of)))>
    <PartCreationPolicy(CreationPolicy.NonShared)>
    <PartNotDiscoverable>
    Friend NotInheritable Class StubVsServiceExporter(Of T As Class)
        Inherits StubVsServiceExporter(Of T, T)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            <Import(GetType(SAsyncServiceProvider))> asyncServiceProvider As IAsyncServiceProvider2, joinableTaskContext As JoinableTaskContext)
            MyBase.New(asyncServiceProvider, joinableTaskContext)
        End Sub
    End Class
End Namespace
