' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.DesignerAttributes
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.DesignerAttributes
    <ExportLanguageServiceFactory(GetType(IDesignerAttributeService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDesignerAttributeServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New BasicDesignerAttributeService(languageServices.WorkspaceServices.Workspace)
        End Function

    End Class

    Friend Class BasicDesignerAttributeService
        Inherits AbstractDesignerAttributeService(Of
            CompilationUnitSyntax,
            NamespaceBlockSyntax,
            ClassBlockSyntax)

        Public Sub New(workspace As Workspace)
            MyBase.New(workspace)
        End Sub

        Protected Overrides Function HasAttributesOrBaseTypeOrIsPartial(type As ClassBlockSyntax) As Boolean
            ' VB can't actually use any syntactic tricks to limit the types we need to look at.
            ' VB allows up to one partial declaration omit the 'Partial' keyword; so the presence
            ' or absence of attributes, base types, or the 'Partial' keyword doesn't mean anything.
            ' If this is a ClassBlockSyntax node, we're going to have to bind.
            Return type IsNot Nothing
        End Function
    End Class
End Namespace
