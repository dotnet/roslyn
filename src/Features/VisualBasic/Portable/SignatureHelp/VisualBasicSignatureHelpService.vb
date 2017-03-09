' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportLanguageServiceFactory(GetType(SignatureHelpService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSignatureHelpServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicSignatureHelpService(languageServices.WorkspaceServices.Workspace)
        End Function
    End Class

    Friend Class VisualBasicSignatureHelpService
        Inherits CommonSignatureHelpService

        Private ReadOnly _defaultProviders As ImmutableArray(Of SignatureHelpProvider) = ImmutableArray.Create(Of SignatureHelpProvider)(
            New AddRemoveHandlerSignatureHelpProvider(),
            New AttributeSignatureHelpProvider(),
            New BinaryConditionalExpressionSignatureHelpProvider(),
            New CastExpressionSignatureHelpProvider(),
            New FunctionAggregationSignatureHelpProvider(),
            New GenericNameSignatureHelpProvider(),
            New GetTypeExpressionSignatureHelpProvider(),
            New GetXmlNamespaceExpressionSignatureHelpProvider(),
            New InvocationExpressionSignatureHelpProvider(),
            New MidAssignmentSignatureHelpProvider(),
            New NameOfExpressionSignatureHelpProvider(),
            New ObjectCreationExpressionSignatureHelpProvider(),
            New PredefinedCastExpressionSignatureHelpProvider(),
            New RaiseEventStatementSignatureHelpProvider(),
            New TernaryConditionalExpressionSignatureHelpProvider()
        )

        Public Sub New(workspace As Workspace)
            MyBase.New(workspace, LanguageNames.VisualBasic)
        End Sub

        Protected Overrides Function GetBuiltInProviders() As ImmutableArray(Of SignatureHelpProvider)
            Return _defaultProviders
        End Function
    End Class
End Namespace