' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.ImplementInterface
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ImplementInterface
    <ExportLanguageService(GetType(IImplementInterfaceService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicImplementInterfaceService
        Inherits AbstractImplementInterfaceService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property CanImplementImplicitly As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides ReadOnly Property HasHiddenExplicitImplementation As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function TryInitializeState(
                document As Document, model As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken,
                ByRef classOrStructDecl As SyntaxNode, ByRef classOrStructType As INamedTypeSymbol, ByRef interfaceTypes As IEnumerable(Of INamedTypeSymbol)) As Boolean
            If cancellationToken.IsCancellationRequested Then
                Return False
            End If

            Dim implementsStatement As ImplementsStatementSyntax
            Dim interfaceNode As TypeSyntax
            If TypeOf node Is ImplementsStatementSyntax Then
                interfaceNode = Nothing
                implementsStatement = DirectCast(node, ImplementsStatementSyntax)
            ElseIf TypeOf node Is TypeSyntax AndAlso TypeOf node.Parent Is ImplementsStatementSyntax Then
                interfaceNode = DirectCast(node, TypeSyntax)
                implementsStatement = DirectCast(node.Parent, ImplementsStatementSyntax)
            Else
                Return False
            End If

            If implementsStatement.IsParentKind(SyntaxKind.ClassBlock) OrElse
               implementsStatement.IsParentKind(SyntaxKind.StructureBlock) Then

                If interfaceNode IsNot Nothing Then
                    interfaceTypes = {GetInterfaceType(model, interfaceNode, cancellationToken)}
                Else
                    interfaceTypes = implementsStatement.Types.Select(
                        Function(t) GetInterfaceType(model, t, cancellationToken))
                End If

                interfaceTypes = interfaceTypes.WhereNotNull().Where(Function(t) t.TypeKind = TypeKind.Interface).ToList()
                If interfaceTypes.Any() Then
                    cancellationToken.ThrowIfCancellationRequested()

                    classOrStructDecl = implementsStatement.Parent
                    Dim classOrStructBlock = TryCast(classOrStructDecl, TypeBlockSyntax)
                    classOrStructType = model.GetDeclaredSymbol(classOrStructBlock.BlockStatement, cancellationToken)

                    Return classOrStructType IsNot Nothing
                End If
            End If

            classOrStructDecl = Nothing
            classOrStructType = Nothing
            interfaceTypes = Nothing
            Return False
        End Function

        Private Function GetInterfaceType(semanticModel As SemanticModel,
                                          node As SyntaxNode,
                                          cancellationToken As CancellationToken) As INamedTypeSymbol
            Dim symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken)
            If symbolInfo.CandidateReason = CandidateReason.WrongArity Then
                Return Nothing
            End If

            Return TryCast(symbolInfo.GetAnySymbol(), INamedTypeSymbol)
        End Function

        Private Shared Function GetClassBlockAt(root As SyntaxNode, position As Integer) As ClassBlockSyntax
            Dim node = root.FindToken(position).Parent.FirstAncestorOrSelf(Function(n As SyntaxNode) n.IsKind(SyntaxKind.ClassBlock))
            Return TryCast(node, ClassBlockSyntax)
        End Function

        Protected Overrides Function CanImplementDisposePattern(symbol As INamedTypeSymbol, classBlock As SyntaxNode) As Boolean
            ' The dispose pattern is only applicable if the implementing type is a class that does not already declare any conflicting
            ' members named 'disposedValue' or 'Dispose' (because we will be generating a 'disposedValue' field and a couple of methods
            ' named 'Dispose' as part of implementing the dispose pattern).
            Return (classBlock IsNot Nothing) AndAlso
                   classBlock.IsKind(SyntaxKind.ClassBlock) AndAlso
                   (symbol IsNot Nothing) AndAlso
                   Not symbol.GetMembers().Any(
                       Function(m) CaseInsensitiveComparison.Comparer.Equals(m.MetadataName, "Dispose") OrElse
                                   CaseInsensitiveComparison.Comparer.Equals(m.MetadataName, "disposedValue"))
        End Function

        Protected Overrides Function ImplementDisposePattern(document As Document, root As SyntaxNode, symbol As INamedTypeSymbol, position As Integer, explicitly As Boolean) As Document
            Dim classBlock = GetClassBlockAt(root, position)
            Debug.Assert(CanImplementDisposePattern(symbol, classBlock), "ImplementDisposePattern called with bad inputs")

            ' Generate the IDisposable boilerplate code.  The generated code cannot be one giant resource string
            ' because of the need to parse, format, and simplify the result; during pseudo-localized builds, resource
            ' strings are given a special prefix and suffix that will break the parser, hence the requirement to
            ' localize the comments individually.
            Dim code = $"
#Region ""IDisposable Support""
    Private disposedValue As Boolean ' {FeaturesResources.To_detect_redundant_calls}

    ' IDisposable
    Protected {If(symbol.IsSealed, "", "Overridable ")}Sub Dispose(disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' {FeaturesResources.TODO_colon_dispose_managed_state_managed_objects}
            End If

            ' {VBFeaturesResources.TODO_colon_free_unmanaged_resources_unmanaged_objects_and_override_Finalize_below}
            ' {FeaturesResources.TODO_colon_set_large_fields_to_null}
        End If
        Me.disposedValue = True
    End Sub

    ' {VBFeaturesResources.TODO_colon_override_Finalize_only_if_Dispose_disposing_As_Boolean_above_has_code_to_free_unmanaged_resources}
    'Protected Overrides Sub Finalize()
    '    ' {VBFeaturesResources.Do_not_change_this_code_Put_cleanup_code_in_Dispose_disposing_As_Boolean_above}
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' {VBFeaturesResources.This_code_added_by_Visual_Basic_to_correctly_implement_the_disposable_pattern}
    Public Sub Dispose() Implements System.IDisposable.Dispose
        ' {VBFeaturesResources.Do_not_change_this_code_Put_cleanup_code_in_Dispose_disposing_As_Boolean_above}
        Dispose(True)
        ' {VBFeaturesResources.TODO_colon_uncomment_the_following_line_if_Finalize_is_overridden_above}
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
"

            Dim decls = SyntaxFactory.ParseSyntaxTree(code).
                GetRoot().DescendantNodes().
                Where(Function(n) n.IsKind(SyntaxKind.FieldDeclaration) OrElse n.IsKind(SyntaxKind.SubBlock)).
                Cast(Of StatementSyntax).
                Select(Function(decl) decl.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)).
                ToArray()

            ' Append #endregion to the trailing trivia of the last declaration being generated.
            decls(decls.Length - 1) = decls(decls.Length - 1).WithAppendedTrailingTrivia(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.Trivia(SyntaxFactory.EndRegionDirectiveTrivia()),
                    SyntaxFactory.ElasticCarriageReturnLineFeed))

            ' Ensure that open and close brace tokens are generated in case they are missing.
            Dim newNode = classBlock.AddMembers(decls).FixTerminators()

            Return document.WithSyntaxRoot(root.ReplaceNode(classBlock, newNode))
        End Function
    End Class
End Namespace
