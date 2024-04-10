' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
    Friend Class TypeSyntaxSimplifierWalker
        Inherits VisualBasicSyntaxWalker

        Private Shared ReadOnly s_emptyAliasedNames As ImmutableHashSet(Of String) = ImmutableHashSet.Create(Of String)(CaseInsensitiveComparison.Comparer)

        Private Shared ReadOnly s_predefinedTypeMetadataNames As ImmutableHashSet(Of String) = ImmutableHashSet.Create(
            CaseInsensitiveComparison.Comparer,
            NameOf([Boolean]),
            NameOf([SByte]),
            NameOf([Byte]),
            NameOf(Int16),
            NameOf(UInt16),
            NameOf(Int32),
            NameOf(UInt32),
            NameOf(Int64),
            NameOf(UInt64),
            NameOf([Single]),
            NameOf([Double]),
            NameOf([Decimal]),
            NameOf([String]),
            NameOf([Char]),
            NameOf(DateTime),
            NameOf([Object]))

        Private ReadOnly _analyzer As VisualBasicSimplifyTypeNamesDiagnosticAnalyzer
        Private ReadOnly _semanticModel As SemanticModel
        Private ReadOnly _options As VisualBasicSimplifierOptions
        Private ReadOnly _analyzerOptions As AnalyzerOptions
        Private ReadOnly _ignoredSpans As TextSpanIntervalTree
        Private ReadOnly _cancellationToken As CancellationToken

        Private _diagnostics As ImmutableArray(Of Diagnostic).Builder

        ''' <summary>
        ''' Set of type and namespace names that have an alias associated with them.  i.e. if the
        ''' user has <c>Imports X = System.DateTime</c>, then <c>DateTime</c> will be in this set.
        ''' This is used so we can easily tell if we should try to simplify some identifier to an
        ''' alias when we encounter it.
        ''' </summary>
        Private ReadOnly _aliasedNames As ImmutableHashSet(Of String)

        Public ReadOnly Property Diagnostics As ImmutableArray(Of Diagnostic)
            Get
                Return If(_diagnostics?.ToImmutable(), ImmutableArray(Of Diagnostic).Empty)
            End Get
        End Property

        Public ReadOnly Property DiagnosticsBuilder As ImmutableArray(Of Diagnostic).Builder
            Get
                If _diagnostics Is Nothing Then
                    Interlocked.CompareExchange(_diagnostics, ImmutableArray.CreateBuilder(Of Diagnostic)(), Nothing)
                End If

                Return _diagnostics
            End Get
        End Property

        Public Sub New(analyzer As VisualBasicSimplifyTypeNamesDiagnosticAnalyzer, semanticModel As SemanticModel, options As VisualBasicSimplifierOptions, analyzerOptions As AnalyzerOptions, ignoredSpans As TextSpanIntervalTree, cancellationToken As CancellationToken)
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)

            _analyzer = analyzer
            _semanticModel = semanticModel
            _options = options
            _analyzerOptions = analyzerOptions
            _ignoredSpans = ignoredSpans
            _cancellationToken = cancellationToken

            Dim root = semanticModel.SyntaxTree.GetRoot(cancellationToken)
            _aliasedNames = GetAliasedNames(TryCast(root, CompilationUnitSyntax))

            For Each aliasSymbol In semanticModel.Compilation.AliasImports()
                _aliasedNames = _aliasedNames.Add(aliasSymbol.Target.Name)
            Next
        End Sub

        Private Shared Function GetAliasedNames(compilationUnit As CompilationUnitSyntax) As ImmutableHashSet(Of String)
            Dim aliasedNames = s_emptyAliasedNames
            If compilationUnit Is Nothing Then
                Return aliasedNames
            End If

            For Each importsStatement In compilationUnit.Imports
                For Each importsClause In importsStatement.ImportsClauses
                    Dim simpleImportsClause = TryCast(importsClause, SimpleImportsClauseSyntax)
                    If simpleImportsClause Is Nothing Then
                        Continue For
                    End If

                    AddAliasedName(aliasedNames, simpleImportsClause)
                Next
            Next

            Return aliasedNames
        End Function

        Private Shared Sub AddAliasedName(ByRef aliasedNames As ImmutableHashSet(Of String), simpleImportsClause As SimpleImportsClauseSyntax)
            If simpleImportsClause.Alias IsNot Nothing Then
                Dim identifierName = TryCast(simpleImportsClause.Name.GetRightmostName(), IdentifierNameSyntax)
                If identifierName IsNot Nothing Then
                    If Not String.IsNullOrEmpty(identifierName.Identifier.ValueText) Then
                        aliasedNames = aliasedNames.Add(identifierName.Identifier.ValueText)
                    End If
                End If
            End If
        End Sub

        Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
            If _ignoredSpans IsNot Nothing AndAlso _ignoredSpans.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) Then
                Return
            End If

            If node.IsKind(SyntaxKind.QualifiedName) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitQualifiedName(node)
        End Sub

        Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
            If _ignoredSpans IsNot Nothing AndAlso _ignoredSpans.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) Then
                Return
            End If

            If node.IsKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitMemberAccessExpression(node)
        End Sub

        Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
            If _ignoredSpans IsNot Nothing AndAlso _ignoredSpans.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) Then
                Return
            End If

            ' Always try to simplify identifiers with an 'Attribute' suffix.
            '
            ' In other cases, don't bother looking at the right side of A.B or A!B. We will process those in
            ' one of our other top level Visit methods (Like VisitQualifiedName).
            Dim canTrySimplify = CaseInsensitiveComparison.EndsWith(node.Identifier.ValueText, "Attribute")
            If Not canTrySimplify AndAlso Not node.IsRightSideOfDotOrBang() Then
                ' The only possible simplifications to an unqualified identifier are replacement with an alias or
                ' replacement with a predefined type.
                canTrySimplify = CanReplaceIdentifierWithAlias(node.Identifier.ValueText) _
                    OrElse CanReplaceIdentifierWithPredefinedType(node.Identifier.ValueText)
            End If

            If canTrySimplify AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitIdentifierName(node)
        End Sub

        Private Function CanReplaceIdentifierWithAlias(identifier As String) As Boolean
            Return _aliasedNames.Contains(identifier)
        End Function

        Private Shared Function CanReplaceIdentifierWithPredefinedType(identifier As String) As Boolean
            Return s_predefinedTypeMetadataNames.Contains(identifier)
        End Function

        Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
            If _ignoredSpans IsNot Nothing AndAlso _ignoredSpans.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) Then
                Return
            End If

            If node.IsKind(SyntaxKind.GenericName) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitGenericName(node)
        End Sub

        Private Function TrySimplify(node As SyntaxNode) As Boolean
            Dim diagnostic As Diagnostic = Nothing
            If Not _analyzer.TrySimplify(_semanticModel, node, diagnostic, _options, _analyzerOptions, _cancellationToken) Then
                Return False
            End If

            DiagnosticsBuilder.Add(diagnostic)
            Return True
        End Function
    End Class
End Namespace
