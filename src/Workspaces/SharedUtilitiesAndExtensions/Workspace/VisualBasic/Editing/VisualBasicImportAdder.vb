' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Editing
    <ExportLanguageService(GetType(ImportAdderService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend NotInheritable Class VisualBasicImportAdder
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

        Protected Overrides Function AddPotentiallyConflictingImportsAsync(model As SemanticModel, container As SyntaxNode, namespaceSymbols As ImmutableArray(Of INamespaceSymbol), conflicts As HashSet(Of INamespaceSymbol), cancellationToken As CancellationToken) As Task
            Dim conflictFinder = New ConflictFinder(model, namespaceSymbols)
            Return conflictFinder.AddPotentiallyConflictingImportsAsync(container, conflicts, cancellationToken)
        End Function

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

        Private Class ConflictFinder
            Inherits VisualBasicSyntaxWalker
            Implements IEqualityComparer(Of (name As String, arity As Integer))

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

            Public Sub New(
                    model As SemanticModel,
                    namespaceSymbols As ImmutableArray(Of INamespaceSymbol))
                MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
                _model = model

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

            Public Async Function AddPotentiallyConflictingImportsAsync(container As SyntaxNode, conflicts As HashSet(Of INamespaceSymbol), cancellationToken As CancellationToken) As Task
                Dim nodes = ArrayBuilder(Of SyntaxNode).GetInstance()
                Dim containsAnonymousMethods = False

                CollectInfoFromContainer(container, nodes, containsAnonymousMethods)

                Dim produceItems =
                    Function(node As SyntaxNode, onItemsFound As Action(Of INamespaceSymbol), conflictFinder As ConflictFinder, cancellationToken1 As CancellationToken) As Task
                        If TypeOf node Is SimpleNameSyntax Then
                            Me.ProduceConflicts(TryCast(node, SimpleNameSyntax), onItemsFound, cancellationToken)
                        ElseIf TypeOf node Is MemberAccessExpressionSyntax Then
                            Me.ProduceConflicts(TryCast(node, MemberAccessExpressionSyntax), containsAnonymousMethods, onItemsFound, cancellationToken)
                        Else
                            Throw ExceptionUtilities.Unreachable()
                        End If
                        Return Task.CompletedTask
                    End Function

                Dim items = Await ProducerConsumer(Of INamespaceSymbol).RunParallelAsync(
                    source:=nodes,
                    produceItems:=produceItems,
                    args:=Me,
                    cancellationToken).ConfigureAwait(False)

                conflicts.AddRange(items)
            End Function

            Private Sub CollectInfoFromContainer(container As SyntaxNode, nodes As ArrayBuilder(Of SyntaxNode), ByRef containsAnonymousMethods As Boolean)
                For Each node In container.DescendantNodesAndSelf()
                    Select Case node.Kind()
                        Case SyntaxKind.IdentifierName,
                             SyntaxKind.GenericName
                            If IsPotentialConflictWithImportedTypeNamespaceOrMember(TryCast(node, SimpleNameSyntax)) Then
                                nodes.Add(node)
                            End If
                        Case SyntaxKind.SimpleMemberAccessExpression,
                             SyntaxKind.DictionaryAccessExpression
                            If IsPotentialConflictWithImportedExtensionMethod(TryCast(node, MemberAccessExpressionSyntax)) Then
                                nodes.Add(node)
                            End If
                        Case SyntaxKind.MultiLineFunctionLambdaExpression,
                             SyntaxKind.MultiLineSubLambdaExpression,
                             SyntaxKind.SingleLineFunctionLambdaExpression,
                             SyntaxKind.SingleLineSubLambdaExpression
                            ' Track if we've seen an anonymous method or not.  If so, because of how the language binds lambdas and
                            ' overloads, we'll assume any method access we see inside (instance or otherwise) could end up conflicting
                            ' with an extension method we might pull in.
                            containsAnonymousMethods = True
                    End Select
                Next
            End Sub

            Private Function IsPotentialConflictWithImportedTypeNamespaceOrMember(node As SimpleNameSyntax) As Boolean
                ' Check to see if we have an standalone identifier (Or identifier on the left of a dot). If so, then we
                ' don't want to bring in any imports that would bring in the same name And could then potentially
                ' conflict here.
                If node.IsRightSideOfDotOrBang Then
                    Return False
                End If

                ' Drastically reduce the number of nodes that need to be inspected by filtering
                ' out nodes whose identifier isn't a potential conflict.
                If _importedTypesAndNamespaces.ContainsKey((node.Identifier.Text, node.Arity)) Then
                    Return True
                End If

                If _importedMembers.ContainsKey(node.Identifier.Text) Then
                    Return True
                End If

                Return False
            End Function

            Private Function IsPotentialConflictWithImportedExtensionMethod(node As MemberAccessExpressionSyntax) As Boolean
                Return _importedExtensionMethods.ContainsKey(node.Name.Identifier.Text)
            End Function

            Private Sub ProduceConflicts(node As SimpleNameSyntax, addConflict As Action(Of INamespaceSymbol), cancellationToken As CancellationToken)
                For Each conflictingSymbol In _importedTypesAndNamespaces.Item((node.Identifier.ValueText, node.Arity))
                    addConflict(conflictingSymbol)
                Next

                For Each conflictingSymbol In _importedMembers.Item(node.Identifier.ValueText)
                    addConflict(conflictingSymbol)
                Next
            End Sub

            Private Sub ProduceConflicts(node As MemberAccessExpressionSyntax, containsAnonymousMethods As Boolean, addConflict As Action(Of INamespaceSymbol), cancellationToken As CancellationToken)
                ' Check to see if we have a reference to an extension method.  If so, then pulling in an import could
                ' bring in an extension that conflicts with that.
                Dim method = TryCast(_model.GetSymbolInfo(node.Name, cancellationToken).GetAnySymbol(), IMethodSymbol)
                If method IsNot Nothing Then
                    Dim isConflicting = method.IsReducedExtension()

                    If Not isConflicting And containsAnonymousMethods Then
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

                        isConflicting = node.HasAncestor(Of LambdaExpressionSyntax)()
                    End If

                    If isConflicting Then
                        For Each conflictingSymbol In _importedExtensionMethods.Item(method.Name)
                            addConflict(conflictingSymbol)
                        Next
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
