' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
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

        Private Overloads Shared Function GetExplicitNamespaceSymbol(fullName As ExpressionSyntax, namespacePart As ExpressionSyntax, model As SemanticModel) As INamespaceSymbol
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
            Implements IEqualityComparer(Of (name As String, arity As Integer))

            Private ReadOnly _cancellationToken As CancellationToken
            Private ReadOnly _model As SemanticModel

            ''' <summary>
            ''' A mapping containing the simple names And arity of all namespace members, mapped to the import that
            ''' they're brought in by.
            ''' </summary>
            Private ReadOnly _importedTypesAndNamespaces As MultiDictionary(Of (name As String, arity As Integer), INamespaceSymbol)

            ''' <summary>
            ''' A mapping containing the simple names of all members, mapped to the import that they're brought in by.
            ''' Members are imported in through modules in vb. This doesn't keep track of arity because methods can be
            ''' called with type arguments.
            ''' </summary>
            Private ReadOnly _importedMembers As MultiDictionary(Of String, INamespaceSymbol)

            ''' <summary>
            ''' A mapping containing the simple names of all extension methods, mapped to the import that they're
            ''' brought in by. This doesn't keep track of arity because methods can be called with type arguments.
            ''' </summary>
            Private ReadOnly _importedExtensionMethods As MultiDictionary(Of String, INamespaceSymbol)

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

                _importedTypesAndNamespaces = New MultiDictionary(Of (name As String, arity As Integer), INamespaceSymbol)(Me)
                _importedMembers = New MultiDictionary(Of String, INamespaceSymbol)(VisualBasicSyntaxFacts.Instance.StringComparer)
                _importedExtensionMethods = New MultiDictionary(Of String, INamespaceSymbol)(VisualBasicSyntaxFacts.Instance.StringComparer)

                AddImportedMembers(namespaceSymbols)
            End Sub

            Private Sub AddImportedMembers(namespaceSymbols As ImmutableArray(Of INamespaceSymbol))
                For Each ns In namespaceSymbols
                    For Each typeOrNamespace In ns.GetMembers()
                        _importedTypesAndNamespaces.Add((typeOrNamespace.Name, typeOrNamespace.GetArity()), ns)

                        Dim type = TryCast(typeOrNamespace, INamedTypeSymbol)
                        If type?.MightContainExtensionMethods Then
                            For Each member In type.GetMembers()
                                Dim method = TryCast(member, IMethodSymbol)
                                If method?.IsExtensionMethod Then
                                    _importedExtensionMethods.Add(method.Name, ns)
                                End If
                            Next
                        End If

                        If type?.TypeKind = TypeKind.Module Then
                            ' modules make their members available to the containing scope.
                            For Each member In type.GetMembers()
                                Dim moduleType = TryCast(member, INamedTypeSymbol)
                                If moduleType IsNot Nothing Then
                                    _importedTypesAndNamespaces.Add((moduleType.Name, moduleType.GetArity()), ns)
                                Else
                                    _importedMembers.Add(member.Name, ns)
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

            Private Sub CheckName(node As NameSyntax, name As String)
                ' Check to see if we have an standalone identifier (Or identifier on the left of a dot). If so, then we
                ' don't want to bring in any imports that would bring in the same name And could then potentially
                ' conflict here.

                If node.IsRightSideOfDotOrBang Then
                    Return
                End If

                _conflictNamespaces.AddRange(_importedTypesAndNamespaces((name, node.Arity)))
                _conflictNamespaces.AddRange(_importedMembers(name))
            End Sub

            Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
                MyBase.VisitIdentifierName(node)
                CheckName(node, node.Identifier.ValueText)
            End Sub

            Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
                MyBase.VisitGenericName(node)
                CheckName(node, node.Identifier.ValueText)
            End Sub

            Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
                MyBase.VisitMemberAccessExpression(node)

                ' Check to see if we have a reference to an extension method.  If so, then pulling in an import could
                ' bring in an extension that conflicts with that.

                Dim method = TryCast(_model.GetSymbolInfo(node.Name, _cancellationToken).GetAnySymbol(), IMethodSymbol)
                If method IsNot Nothing Then
                    ' see explanation in VisitSimpleLambdaExpression for the _inAnonymousMethod check
                    If method.IsReducedExtension() OrElse _inAnonymousMethod Then
                        _conflictNamespaces.AddRange(_importedExtensionMethods(method.Name))
                    End If
                End If
            End Sub

            Public Shadows Function Equals(
                    x As (name As String, arity As Integer),
                    y As (name As String, arity As Integer)) As Boolean Implements IEqualityComparer(Of (name As String, arity As Integer)).Equals

                Return x.arity = y.arity AndAlso
                    VisualBasicSyntaxFacts.Instance.StringComparer.Equals(x.name, y.name)
            End Function

            Public Shadows Function GetHashCode(obj As (name As String, arity As Integer)) As Integer Implements IEqualityComparer(Of (name As String, arity As Integer)).GetHashCode
                Return Hash.Combine(obj.arity,
                    VisualBasicSyntaxFacts.Instance.StringComparer.GetHashCode(obj.name))
            End Function
        End Class
    End Class
End Namespace
