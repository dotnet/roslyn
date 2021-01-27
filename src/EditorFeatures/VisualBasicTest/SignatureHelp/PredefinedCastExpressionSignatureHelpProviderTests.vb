' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class PredefinedCastExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(PredefinedCastExpressionSignatureHelpProvider)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForCBool() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = CBool($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"CBool({VBWorkspaceResources.expression}) As Boolean",
                                     String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Boolean"),
                                     VBWorkspaceResources.The_expression_to_be_evaluated_and_converted,
                                     currentParameterIndex:=0))
            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace
