' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicUseSiteDiagnosticsCheckEnforcerAnalyzer
        Inherits AbstractSyntaxNodeAnalyzer(Of SyntaxKind)

        Private Shared s_localizableTitle As LocalizableString = New LocalizableResourceString(NameOf(RoslynDiagnosticsResources.UseSiteDiagnosticsCheckerDescription), RoslynDiagnosticsResources.ResourceManager, GetType(RoslynDiagnosticsResources))
        Private Shared s_localizableMessage As LocalizableString = New LocalizableResourceString(NameOf(RoslynDiagnosticsResources.UseSiteDiagnosticsCheckerMessage), RoslynDiagnosticsResources.ResourceManager, GetType(RoslynDiagnosticsResources))

        Private Shared s_descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(RoslynDiagnosticIds.UseSiteDiagnosticsCheckerRuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessage,
                                                                             "Usage",
                                                                             DiagnosticSeverity.Error,
                                                                             False,
                                                                             WellKnownDiagnosticTags.Telemetry)

        Private Shared s_propertiesToValidateMap As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From
                {
                    {s_baseTypeString, s_typeSymbolFullyQualifiedName},
                    {s_interfacesString, s_typeSymbolFullyQualifiedName},
                    {s_allInterfacesString, s_typeSymbolFullyQualifiedName},
                    {s_typeArgumentsString, s_namedTypeSymbolFullyQualifiedName},
                    {s_constraintTypesString, s_typeParameterSymbolFullyQualifiedName}
                }

        Private Const s_baseTypeString = "BaseType"
        Private Const s_interfacesString = "Interfaces"
        Private Const s_allInterfacesString = "AllInterfaces"
        Private Const s_typeArgumentsString = "TypeArguments"
        Private Const s_constraintTypesString = "ConstraintTypes"

        Private Const s_typeSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol"
        Private Const s_namedTypeSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol"
        Private Const s_typeParameterSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeParameterSymbol"

        Protected Overrides ReadOnly Property Descriptor As DiagnosticDescriptor
            Get
                Return s_descriptor
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As SyntaxKind()
            Get
                Return {SyntaxKind.SimpleMemberAccessExpression}
            End Get
        End Property

        Protected Overrides Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim name = DirectCast(context.Node, MemberAccessExpressionSyntax).Name
            If name.Kind = SyntaxKind.IdentifierName Then
                Dim identifier = DirectCast(name, IdentifierNameSyntax)
                Dim containingTypeName As String = Nothing
                If s_propertiesToValidateMap.TryGetValue(identifier.ToString(), containingTypeName) Then
                    Dim sym As ISymbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol
                    If sym IsNot Nothing AndAlso sym.Kind = SymbolKind.Property Then
                        If containingTypeName = sym.ContainingType.ToDisplayString() Then
                            ReportDiagnostic(context, identifier, identifier.ToString())
                        End If
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace
