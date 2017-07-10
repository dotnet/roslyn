' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.CallHierarchy

    <ExportCommandHandler("CallHierarchy", ContentTypeNames.VisualBasicContentType)>
    <Order(After:=PredefinedCommandHandlerNames.DocumentationComments)>
    Friend Class CallHierarchyCommandHandler
        Inherits AbstractCallHierarchyCommandHandler

        <ImportingConstructor>
        Protected Sub New(<ImportMany> presenters As IEnumerable(Of ICallHierarchyPresenter), provider As CallHierarchyProvider, waitIndicator As IWaitIndicator)
            MyBase.New(presenters, provider, waitIndicator)
        End Sub
    End Class
End Namespace
