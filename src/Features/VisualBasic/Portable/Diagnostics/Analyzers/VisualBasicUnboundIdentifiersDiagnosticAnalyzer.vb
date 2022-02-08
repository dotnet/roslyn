' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.AddImport
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicUnboundIdentifiersDiagnosticAnalyzer
        Inherits UnboundIdentifiersDiagnosticAnalyzerBase(Of SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IncompleteMemberSyntax)

        Private ReadOnly _messageFormat As LocalizableString = New LocalizableResourceString(NameOf(VBFeaturesResources.Type_0_is_not_defined), VBFeaturesResources.ResourceManager, GetType(VBFeaturesResources))

        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.IncompleteMember)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) = s_kindsOfInterest

        Protected Overrides ReadOnly Property DiagnosticDescriptor As DiagnosticDescriptor
            Get
                Return GetDiagnosticDescriptor(IDEDiagnosticIds.UnboundIdentifierId, _messageFormat)
            End Get
        End Property

        Protected Overrides Function IsNameOf(node As SyntaxNode) As Boolean
            Return node.Kind() = SyntaxKind.NameOfKeyword
        End Function
    End Class
End Namespace
