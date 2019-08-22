' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Analyzer.Utilities.Extensions
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicInvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsAnalyzer
        Inherits DiagnosticAnalyzer

        Private Shared ReadOnly s_localizableTitle As LocalizableString = New LocalizableResourceString(NameOf(RoslynDiagnosticsAnalyzersResources.InvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, GetType(RoslynDiagnosticsAnalyzersResources))
        Private Shared ReadOnly s_localizableMessage As LocalizableString = New LocalizableResourceString(NameOf(RoslynDiagnosticsAnalyzersResources.InvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, GetType(RoslynDiagnosticsAnalyzersResources))

        Private Shared ReadOnly s_descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(RoslynDiagnosticIds.UseSiteDiagnosticsCheckerRuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessage,
                                                                             "Usage",
                                                                             DiagnosticSeverity.Error,
                                                                             False,
                                                                             Nothing,
                                                                             Nothing,
                                                                             WellKnownDiagnosticTags.Telemetry)

        Private Shared ReadOnly s_propertiesToValidateMap As ImmutableDictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From
                {
                    {s_baseTypeString, s_typeSymbolFullyQualifiedName},
                    {s_interfacesString, s_typeSymbolFullyQualifiedName},
                    {s_allInterfacesString, s_typeSymbolFullyQualifiedName},
                    {s_typeArgumentsString, s_namedTypeSymbolFullyQualifiedName},
                    {s_constraintTypesString, s_typeParameterSymbolFullyQualifiedName}
                }.ToImmutableDictionary()

        Private Const s_baseTypeString = "BaseType"
        Private Const s_interfacesString = "Interfaces"
        Private Const s_allInterfacesString = "AllInterfaces"
        Private Const s_typeArgumentsString = "TypeArguments"
        Private Const s_constraintTypesString = "ConstraintTypes"

        Private Const s_typeSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol"
        Private Const s_namedTypeSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol"
        Private Const s_typeParameterSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeParameterSymbol"

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(s_descriptor)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.EnableConcurrentExecution()
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)

            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression)
        End Sub

        Private Shared Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim name = DirectCast(context.Node, MemberAccessExpressionSyntax).Name
            If name.Kind = SyntaxKind.IdentifierName Then
                Dim identifier = DirectCast(name, IdentifierNameSyntax)
                Dim containingTypeName As String = Nothing
                If s_propertiesToValidateMap.TryGetValue(identifier.ToString(), containingTypeName) Then
                    Dim sym As ISymbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol
                    If sym IsNot Nothing AndAlso sym.Kind = SymbolKind.Property Then
                        If containingTypeName = sym.ContainingType.ToDisplayString() Then
                            context.ReportDiagnostic(identifier.CreateDiagnostic(s_descriptor, identifier.ToString()))
                        End If
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace
