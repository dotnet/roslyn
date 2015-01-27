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

        Private Const UndefinedType1 As String = "BC30002"
        Private Const MessageFormat As String = "Type '{0}' is not defined."

        Private Shared ReadOnly _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.IncompleteMember)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind)
            Get
                Return _kindsOfInterest
            End Get
        End Property

        Protected Overrides ReadOnly Property DiagnosticDescriptor As DiagnosticDescriptor
            Get
                Return GetDiagnosticDescriptor(UndefinedType1, MessageFormat)
            End Get
        End Property
    End Class
End Namespace
