' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.AddImport
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.AddImport
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicAddImportDiagnosticAnalyzer
        Inherits AddImportDiagnosticAnalyzerBase(Of SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IncompleteMemberSyntax)

        Private Const s_undefinedType1 As String = "BC30002"
        Private Const s_messageFormat As String = "Type '{0}' is not defined."

        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.IncompleteMember)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind)
            Get
                Return s_kindsOfInterest
            End Get
        End Property

        Protected Overrides ReadOnly Property DiagnosticDescriptor As DiagnosticDescriptor
            Get
                Return GetDiagnosticDescriptor(s_undefinedType1, s_messageFormat)
            End Get
        End Property
    End Class
End Namespace
