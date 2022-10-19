' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities.Completion
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.ArgumentProviders
    Public MustInherit Class AbstractVisualBasicArgumentProviderTests
        Inherits AbstractArgumentProviderTests(Of VisualBasicTestWorkspaceFixture)

        Protected Overrides Function GetArgumentList(token As SyntaxToken) As (argumentList As SyntaxNode, arguments As ImmutableArray(Of SyntaxNode))
            Dim argumentList = token.GetRequiredParent().GetAncestorsOrThis(Of ArgumentListSyntax)().First()
            Return (argumentList, argumentList.Arguments.Cast(Of SyntaxNode)().ToImmutableArray())
        End Function
    End Class
End Namespace
