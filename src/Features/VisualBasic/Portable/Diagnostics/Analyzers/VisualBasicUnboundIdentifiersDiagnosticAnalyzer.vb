' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.AddImport
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicUnboundIdentifiersDiagnosticAnalyzer
        Inherits UnboundIdentifiersDiagnosticAnalyzerBase(Of SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IncompleteMemberSyntax, LambdaExpressionSyntax)

        Private Const s_undefinedType1 As String = "BC30002"
        Private ReadOnly _messageFormat As LocalizableString = New LocalizableResourceString(NameOf(VBFeaturesResources.ERR_UndefinedType1), VBFeaturesResources.ResourceManager, GetType(VBFeaturesResources.VBFeaturesResources))
        Private Const s_undefinedType2 As String = "BC30057"
        Private ReadOnly _messageFormat2 As LocalizableString = New LocalizableResourceString(NameOf(VBFeaturesResources.ERR_TooManyArgs1), VBFeaturesResources.ResourceManager, GetType(VBFeaturesResources.VBFeaturesResources))


        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
            SyntaxKind.IncompleteMember,
            SyntaxKind.MultiLineFunctionLambdaExpression,
            SyntaxKind.MultiLineSubLambdaExpression,
            SyntaxKind.SingleLineFunctionLambdaExpression,
            SyntaxKind.SingleLineSubLambdaExpression)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind)
            Get
                Return s_kindsOfInterest
            End Get
        End Property

        Protected Overrides ReadOnly Property DiagnosticDescriptor As DiagnosticDescriptor
            Get
                Return GetDiagnosticDescriptor(s_undefinedType1, _messageFormat)
            End Get
        End Property

        Protected Overrides ReadOnly Property DiagnosticDescriptor2 As DiagnosticDescriptor
            Get
                Return GetDiagnosticDescriptor(s_undefinedType2, _messageFormat2)
            End Get
        End Property

        Protected Overrides Function ConstructorDoesNotExist(node As SyntaxNode, info As SymbolInfo, semanticModel As SemanticModel) As Boolean
            Dim arguments = (TryCast(node.Parent, ObjectCreationExpressionSyntax)?.ArgumentList?.Arguments)
            If Not arguments.HasValue Then
                Return False
            End If

            Dim args = arguments.Value

            Dim constructors = TryCast(info.Symbol?.OriginalDefinition, INamedTypeSymbol)?.Constructors
            If constructors Is Nothing Then
                Return False
            End If

            Dim count = constructors.Value _
                .WhereAsArray(Function(constructor) constructor.Parameters.Length = args.Count) _
                .WhereAsArray(Function(constructor)
                                  For index = 0 To constructor.Parameters.Length - 1
                                      Dim typeInfo = semanticModel.GetTypeInfo(args(index).GetExpression)
                                      If Not constructor.Parameters(index).Type.Equals(typeInfo.ConvertedType) Then
                                          Return False
                                      End If
                                  Next
                                  Return True
                              End Function) _
                              .Length

            If count = 0 Then
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
