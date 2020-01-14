' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Backstop that forms the end of the binder chain. Does nothing, and should never actually get hit. Provides
    ''' asserts that methods never get called.
    ''' </summary>
    Friend NotInheritable Class BackstopBinder
        Inherits Binder

        Public Sub New()
            MyBase.New(Nothing)
        End Sub

        Public Overrides Function CheckAccessibility(sym As Symbol,
                                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                     Optional accessThroughType As TypeSymbol = Nothing,
                                                     Optional basesBeingResolved As BasesBeingResolved = Nothing) As AccessCheckResult
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function GetBinder(node As SyntaxNode) As Binder
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function GetBinder(stmtList As SyntaxList(Of StatementSyntax)) As Binder
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property ImplicitlyTypedLocalsBeingBound As ConsList(Of LocalSymbol)
            Get
                Return ConsList(Of LocalSymbol).Empty
            End Get

        End Property

        ''' <summary>
        ''' Returns true if the node is in a position where an unbound type
        ''' such as (C(of)) is allowed.
        ''' </summary>
        Public Overrides Function IsUnboundTypeAllowed(syntax As GenericNameSyntax) As Boolean
            Return False
        End Function

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsInQuery As Boolean
            Get
                ' we should stop at the method or type binder.
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingNamespaceOrType As NamespaceOrTypeSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides Function GetSyntaxReference(node As VisualBasicSyntaxNode) As SyntaxReference
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function CreateBoundWithBlock(node As WithBlockSyntax, boundBlockBinder As Binder, diagnostics As DiagnosticBag) As BoundStatement
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function BindInsideCrefAttributeValue(name As TypeSyntax, preserveAliases As Boolean, diagnosticBag As DiagnosticBag, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Friend Overrides Function BindInsideCrefAttributeValue(reference As CrefReferenceSyntax, preserveAliases As Boolean, diagnosticBag As DiagnosticBag, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Friend Overrides Function BindXmlNameAttributeValue(identifier As IdentifierNameSyntax, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Protected Friend Overrides Function TryBindOmittedLeftForMemberAccess(node As MemberAccessExpressionSyntax,
                                                                              diagnostics As DiagnosticBag,
                                                                              accessingBinder As Binder,
                                                                              <Out> ByRef wholeMemberAccessExpressionBound As Boolean) As BoundExpression
            Return Nothing
        End Function

        Protected Overrides Function TryBindOmittedLeftForDictionaryAccess(node As MemberAccessExpressionSyntax,
                                                                           accessingBinder As Binder,
                                                                           diagnostics As DiagnosticBag) As BoundExpression
            Return Nothing
        End Function

        Protected Overrides Function TryBindOmittedLeftForConditionalAccess(node As ConditionalAccessExpressionSyntax, accessingBinder As Binder, diagnostics As DiagnosticBag) As BoundExpression
            Return Nothing
        End Function

        Protected Friend Overrides Function TryBindOmittedLeftForXmlMemberAccess(node As XmlMemberAccessExpressionSyntax,
                                                                                 diagnostics As DiagnosticBag,
                                                                                 accessingBinder As Binder) As BoundExpression
            Return Nothing
        End Function

        Protected Overrides Function TryGetConditionalAccessReceiver(node As ConditionalAccessExpressionSyntax) As BoundExpression
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property ConstantFieldsInProgress As SymbolsInProgress(Of FieldSymbol)
            Get
                Return SymbolsInProgress(Of FieldSymbol).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultParametersInProgress As SymbolsInProgress(Of ParameterSymbol)
            Get
                Return SymbolsInProgress(Of ParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property OptionStrict As OptionStrict
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property OptionInfer As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property OptionExplicit As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property OptionCompareText As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property CheckOverflow As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property AllImplicitVariableDeclarationsAreHandled As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ImplicitVariableDeclarationAllowed As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function DeclareImplicitLocalVariable(nameSyntax As IdentifierNameSyntax, diagnostics As DiagnosticBag) As LocalSymbol
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property ImplicitlyDeclaredVariables As ImmutableArray(Of LocalSymbol)
            Get
                Return ImmutableArray(Of LocalSymbol).Empty
            End Get
        End Property

        Public Overrides Sub DisallowFurtherImplicitVariableDeclaration(diagnostics As DiagnosticBag)
            ' No action needed.
        End Sub

        Public Overrides Function GetContinueLabel(continueSyntaxKind As SyntaxKind) As LabelSymbol
            Return Nothing
        End Function

        Public Overrides Function GetExitLabel(exitSyntaxKind As SyntaxKind) As LabelSymbol
            Return Nothing
        End Function

        Public Overrides Function GetReturnLabel() As LabelSymbol
            Return Nothing
        End Function

        Public Overrides Function GetLocalForFunctionValue() As LocalSymbol
            Return Nothing
        End Function

        Protected Overrides ReadOnly Property IsInsideChainedConstructorCallArguments As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property BindingLocation As BindingLocation
            Get
                Return BindingLocation.None
            End Get
        End Property

        Private Shared ReadOnly s_defaultXmlNamespaces As New Dictionary(Of String, String) From {
            {StringConstants.DefaultXmlnsPrefix, StringConstants.DefaultXmlNamespace},
            {StringConstants.XmlPrefix, StringConstants.XmlNamespace},
            {StringConstants.XmlnsPrefix, StringConstants.XmlnsNamespace}
        }

        Friend Overrides Function LookupXmlNamespace(prefix As String, ignoreXmlNodes As Boolean, <Out()> ByRef [namespace] As String, <Out()> ByRef fromImports As Boolean) As Boolean
            fromImports = False
            Return s_defaultXmlNamespaces.TryGetValue(prefix, [namespace])
        End Function

        Friend Overrides ReadOnly Property HasImportedXmlNamespaces As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Sub GetInScopeXmlNamespaces(builder As ArrayBuilder(Of KeyValuePair(Of String, String)))
            ' Method shouldn't be called outside of XmlElement.
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Function LookupLabelByNameToken(labelName As SyntaxToken) As LabelSymbol
            Return Nothing
        End Function

#If DEBUG Then
        Public Overrides Sub CheckSimpleNameBindingOrder(node As SimpleNameSyntax)
            ' do nothing
        End Sub

        Public Overrides Sub EnableSimpleNameBindingOrderChecks(enable As Boolean)
            ' do nothing
        End Sub
#End If

        Friend Overrides Function GetWithStatementPlaceholderSubstitute(placeholder As BoundValuePlaceholderBase) As BoundExpression
            Return Nothing
        End Function

        Public Overrides ReadOnly Property QuickAttributeChecker As QuickAttributeChecker
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsDefaultInstancePropertyAllowed As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property SuppressCallerInfo As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property SuppressObsoleteDiagnostics As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSemanticModelBinder As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function BinderSpecificLookupOptions(options As LookupOptions) As LookupOptions
            Return options
        End Function
    End Class

End Namespace
