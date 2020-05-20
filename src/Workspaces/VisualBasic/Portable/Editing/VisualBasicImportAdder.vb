' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Editing
    <ExportLanguageService(GetType(ImportAdderService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicImportAdder
        Inherits ImportAdderService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetExplicitNamespaceSymbol(node As SyntaxNode, model As SemanticModel) As INamespaceSymbol
            Dim qname = TryCast(node, QualifiedNameSyntax)
            If qname IsNot Nothing Then
                Return GetExplicitNamespaceSymbol(qname, qname.Left, model)
            End If

            Dim maccess = TryCast(node, MemberAccessExpressionSyntax)
            If maccess IsNot Nothing Then
                Return GetExplicitNamespaceSymbol(maccess, maccess.Expression, model)
            End If

            Return Nothing
        End Function

        Protected Overrides Sub AddPotentiallyConflictingImports(model As SemanticModel,
                container As SyntaxNode,
                namespaceSymbols As ImmutableArray(Of INamespaceSymbol),
                conflicts As HashSet(Of INamespaceSymbol),
                cancellationToken As CancellationToken)
            Dim walker = New ConflictWalker(model, namespaceSymbols, conflicts, cancellationToken)
            walker.Visit(container)
        End Sub

        Private Overloads Function GetExplicitNamespaceSymbol(fullName As ExpressionSyntax, namespacePart As ExpressionSyntax, model As SemanticModel) As INamespaceSymbol
            ' name must refer to something that is not a namespace, but be qualified with a namespace.
            Dim Symbol = model.GetSymbolInfo(fullName).Symbol
            Dim nsSymbol = TryCast(model.GetSymbolInfo(namespacePart).Symbol, INamespaceSymbol)

            If Symbol IsNot Nothing AndAlso Symbol.Kind <> SymbolKind.Namespace AndAlso nsSymbol IsNot Nothing Then
                ' use the symbols containing namespace, and not the potentially less than fully qualified namespace in the full name expression.
                Dim ns = Symbol.ContainingNamespace
                If ns IsNot Nothing Then
                    Return model.Compilation.GetCompilationNamespace(ns)
                End If
            End If

            Return Nothing
        End Function

        Private Class ConflictWalker
            Inherits VisualBasicSyntaxWalker

            Private ReadOnly _cancellationToken As CancellationToken
            Private ReadOnly _model As SemanticModel

            ''' <summary>
            ''' A mapping containing the simple names And arity of all namespace members, mapped to the import that
            ''' they're brought in by.
            ''' </summary>
            Private ReadOnly _namespaceMembers As MultiDictionary(Of (name As String, arity As Integer), INamespaceSymbol) =
                New MultiDictionary(Of (name As String, arity As Integer), INamespaceSymbol)()

            ''' <summary>
            ''' A mapping containing the simple names of all extension methods, mapped to the import that they're
            ''' brought in by.  This doesn't keep track of arity because methods can be called with type arguments.
            ''' </summary>
            Private ReadOnly _extensionMethods As MultiDictionary(Of String, INamespaceSymbol) =
                New MultiDictionary(Of String, INamespaceSymbol)()

            Private ReadOnly _conflictNamespaces As HashSet(Of INamespaceSymbol)

            ''' <summary>
            ''' Track if we're in an anonymous method or not.  If so, because of how the language binds lambdas and
            ''' overloads, we'll assume any method access we see inside (instance or otherwise) could end up conflicting
            ''' with an extension method we might pull in.
            ''' </summary>
            Private _inAnonymousMethod As Boolean

            Public Sub New(
                    model As SemanticModel,
                    namespaceSymbols As ImmutableArray(Of INamespaceSymbol),
                    conflictNamespaces As HashSet(Of INamespaceSymbol),
                    cancellationToken As CancellationToken)
                MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
                _model = model
                _cancellationToken = cancellationToken
                _conflictNamespaces = conflictNamespaces

                For Each ns In namespaceSymbols
                    For Each typeOrNamespace In ns.GetMembers()
                        _namespaceMembers.Add((typeOrNamespace.Name, typeOrNamespace.GetArity()), ns)

                        Dim type = TryCast(typeOrNamespace, INamedTypeSymbol)
                        If type?.MightContainExtensionMethods Then
                            For Each member In type.GetMembers()
                                Dim method = TryCast(member, IMethodSymbol)
                                If method?.IsExtensionMethod Then
                                    _extensionMethods.Add(method.Name, ns)
                                End If
                            Next
                        End If
                    Next
                Next
            End Sub

            Public Overrides Sub VisitMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax)

                ' lambdas are interesting.  Say you have
                '
                '      Goo(sub (x) x.M())
                '
                '      sub Goo(act as Action(of C))
                '      sub Goo(act as Action(of integer))
                '
                '      class C : public sub M()
                '
                ' This Is legal code where the lambda body Is calling the instance method.  However, if we introduce a
                ' using that brings in an extension method 'M' on 'int', then the above will become ambiguous.  This is
                ' because lambda binding will try each interpretation separately And eliminate the ones that fail.
                ' Adding the import will make the int form succeed, causing ambiguity.
                '
                ' To deal with that, we keep track of if we're in a lambda, and we conservatively assume that a method
                ' access (even to a non-extension method) could conflict with an extension method brought in.

                Dim previousInAnonymousMethod = _inAnonymousMethod
                _inAnonymousMethod = True
                MyBase.VisitMultiLineLambdaExpression(node)
                _inAnonymousMethod = previousInAnonymousMethod
            End Sub

            Public Overrides Sub VisitSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax)
                Dim previousInAnonymousMethod = _inAnonymousMethod
                _inAnonymousMethod = True
                MyBase.VisitSingleLineLambdaExpression(node)
                _inAnonymousMethod = previousInAnonymousMethod
            End Sub

            Private Sub CheckName(node As NameSyntax)
                ' Check to see if we have an standalone identifier (Or identifier on the left of a dot). If so, if that
                ' identifier binds to a namespace Or type, then we don't want to bring in any imports that would bring
                ' in the same name And could then potentially conflict here.

                If node.IsRightSideOfDotOrBang Then
                    Return
                End If

                Dim symbol = _model.GetSymbolInfo(node, _cancellationToken).GetAnySymbol()
                If symbol Is Nothing Then
                    Return
                End If

                If symbol.Kind = SymbolKind.Namespace Or symbol.Kind = SymbolKind.NamedType Then
                    _conflictNamespaces.AddRange(_namespaceMembers((symbol.Name, node.Arity)))
                ElseIf symbol.OriginalDefinition.IsReducedExtension() Then
                    _conflictNamespaces.AddRange(_extensionMethods(symbol.Name))
                End If
            End Sub

            Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
                MyBase.VisitIdentifierName(node)
                CheckName(node)
            End Sub

            Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
                MyBase.VisitGenericName(node)
                CheckName(node)
            End Sub

            Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
                MyBase.VisitMemberAccessExpression(node)

                ' Check to see if we have a reference to an extension method.  If so, then pulling in an import could
                ' bring in an extension that conflicts with that.

                Dim method = TryCast(_model.GetSymbolInfo(node.Name, _cancellationToken).GetAnySymbol(), IMethodSymbol)
                If method IsNot Nothing Then
                    ' see explanation in VisitSimpleLambdaExpression for the _inAnonymousMethod check
                    If method.IsReducedExtension() OrElse _inAnonymousMethod Then
                        _conflictNamespaces.AddRange(_extensionMethods(method.Name))
                    End If
                End If
            End Sub
        End Class
    End Class
End Namespace
