' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Allows asking semantic questions about a tree of syntax nodes in a Compilation. Typically,
    ''' an instance is obtained by a call to Compilation.GetBinding. 
    ''' </summary>
    ''' <remarks>
    ''' <para>An instance of SemanticModel caches local symbols and semantic information. Thus, it
    ''' is much more efficient to use a single instance of SemanticModel when asking multiple
    ''' questions about a syntax tree, because information from the first question may be reused.
    ''' This also means that holding onto an instance of SemanticModel for a long time may keep a
    ''' significant amount of memory from being garbage collected.
    ''' </para>
    ''' <para>
    ''' When an answer is a named symbol that is reachable by traversing from the root of the symbol
    ''' table, (that is, from an AssemblySymbol of the Compilation), that symbol will be returned
    ''' (i.e. the returned value will be reference-equal to one reachable from the root of the
    ''' symbol table). Symbols representing entities without names (e.g. array-of-int) may or may
    ''' not exhibit reference equality. However, some named symbols (such as local variables) are
    ''' not reachable from the root. These symbols are visible as answers to semantic questions.
    ''' When the same SemanticModel object is used, the answers exhibit reference-equality.  
    ''' </para>
    ''' </remarks>
    Friend MustInherit Class VBSemanticModel
        Inherits SemanticModel

        ''' <summary> 
        ''' The compilation associated with this binding.
        ''' </summary> 
        Public MustOverride Shadows ReadOnly Property Compilation As VisualBasicCompilation

        ''' <summary> 
        ''' The root node of the syntax tree that this binding is based on.
        ''' </summary> 
        Friend MustOverride Shadows ReadOnly Property Root As SyntaxNode

        <Experimental(RoslynExperiments.NullableDisabledSemanticModel, UrlFormat:=RoslynExperiments.NullableDisabledSemanticModel_Url)>
        Public NotOverridable Overrides ReadOnly Property NullableAnalysisIsDisabled As Boolean = False

        ''' <summary>
        ''' Gets symbol information about an expression syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called.
        ''' </summary>
        Friend MustOverride Function GetExpressionSymbolInfo(node As ExpressionSyntax, options As SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo

        ''' <summary>
        ''' Gets symbol information about the 'Add' method corresponding to an expression syntax <paramref name="node"/> within collection initializer.
        ''' This is the worker function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called and the <paramref name="node"/> is in the right place in the syntax tree.
        ''' </summary>
        Friend MustOverride Function GetCollectionInitializerAddSymbolInfo(collectionInitializer As ObjectCreationExpressionSyntax, node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo

        ''' <summary>
        ''' Gets symbol information about an attribute syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called.
        ''' </summary>
        Friend MustOverride Function GetAttributeSymbolInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo

        ''' <summary>
        ''' Gets type information about an expression syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called.
        ''' </summary>
        Friend MustOverride Function GetExpressionTypeInfo(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo

        ''' <summary>
        ''' Gets type information about an attribute syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called.
        ''' </summary>
        Friend MustOverride Function GetAttributeTypeInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo

        ''' <summary>
        ''' Gets constant value information about an expression syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called.
        ''' </summary>
        Friend MustOverride Function GetExpressionConstantValue(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ConstantValue

        ''' <summary>
        ''' Gets member group information about an expression syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called.
        ''' </summary>
        Friend MustOverride Function GetExpressionMemberGroup(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' Gets member group information about an attribute syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. It can assume that 
        ''' CheckSyntaxNode has already been called.
        ''' </summary>
        Friend MustOverride Function GetAttributeMemberGroup(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' Gets symbol information about a cref reference syntax node. This is the worker
        ''' function that is overridden in various derived kinds of Semantic Models. 
        ''' </summary>
        Friend MustOverride Function GetCrefReferenceSymbolInfo(crefReference As CrefReferenceSyntax, options As SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo

        ' Is this node one that could be successfully interrogated by GetSymbolInfo/GetTypeInfo/GetMemberGroup/GetConstantValue?
        Friend Function CanGetSemanticInfo(node As VisualBasicSyntaxNode, Optional allowNamedArgumentName As Boolean = False) As Boolean
            Debug.Assert(node IsNot Nothing)

            ' These aren't really expressions - it's just a manifestation of the SyntaxNode type hierarchy.
            If node.Kind = SyntaxKind.XmlName Then
                Return False
            End If

            Dim trivia As StructuredTriviaSyntax = node.EnclosingStructuredTrivia
            If trivia IsNot Nothing Then
                ' Allow getting semantic info on names from Cref and Name attributes 
                ' inside documentation trivia

                Return IsInCrefOrNameAttributeInterior(node)
            End If

            Return Not node.IsMissing AndAlso
                (TypeOf (node) Is ExpressionSyntax AndAlso (allowNamedArgumentName OrElse Not SyntaxFacts.IsNamedArgumentName(node)) OrElse
                 TypeOf (node) Is AttributeSyntax OrElse
                 TypeOf (node) Is QueryClauseSyntax OrElse
                 TypeOf (node) Is ExpressionRangeVariableSyntax OrElse
                 TypeOf (node) Is OrderingSyntax)
        End Function

        Protected Overrides Function GetOperationCore(node As SyntaxNode, cancellationToken As CancellationToken) As IOperation
            Dim vbnode = DirectCast(node, VisualBasicSyntaxNode)
            CheckSyntaxNode(vbnode)

            Return GetOperationWorker(vbnode, cancellationToken)
        End Function

        Friend Overridable Function GetOperationWorker(node As VisualBasicSyntaxNode, cancellationToken As CancellationToken) As IOperation
            Return Nothing
        End Function

        ''' <summary>
        ''' Returns what symbol(s), if any, the given expression syntax bound to in the program.
        '''
        ''' An AliasSymbol will never be returned by this method. What the alias refers to will be
        ''' returned instead. To get information about aliases, call GetAliasInfo.
        '''
        ''' If binding the type name C in the expression "new C(...)" the actual constructor bound to
        ''' will be returned (or all constructor if overload resolution failed). This occurs as long as C
        ''' unambiguously binds to a single type that has a constructor. If C ambiguously binds to multiple
        ''' types, or C binds to a static class, then type(s) are returned.
        ''' </summary>
        Public Shadows Function GetSymbolInfo(expression As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            CheckSyntaxNode(expression)

            If CanGetSemanticInfo(expression, allowNamedArgumentName:=True) Then
                If SyntaxFacts.IsNamedArgumentName(expression) Then
                    ' Named arguments are handled in a special way.
                    Return GetNamedArgumentSymbolInfo(DirectCast(expression, IdentifierNameSyntax), cancellationToken)
                Else
                    Return GetExpressionSymbolInfo(expression, SymbolInfoOptions.DefaultOptions, cancellationToken)
                End If
            Else
                Return SymbolInfo.None
            End If
        End Function

        ''' <summary>
        ''' Returns what 'Add' method symbol(s), if any, corresponds to the given expression syntax 
        ''' within <see cref="ObjectCollectionInitializerSyntax.Initializer"/>.
        ''' </summary>
        Public Shadows Function GetCollectionInitializerSymbolInfo(expression As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            CheckSyntaxNode(expression)

            If expression.Parent IsNot Nothing AndAlso expression.Parent.Kind = SyntaxKind.CollectionInitializer AndAlso
               expression.Parent.Parent IsNot Nothing AndAlso expression.Parent.Parent.Kind = SyntaxKind.ObjectCollectionInitializer AndAlso
               DirectCast(expression.Parent.Parent, ObjectCollectionInitializerSyntax).Initializer Is expression.Parent AndAlso
               expression.Parent.Parent.Parent IsNot Nothing AndAlso expression.Parent.Parent.Parent.Kind = SyntaxKind.ObjectCreationExpression AndAlso
               CanGetSemanticInfo(expression.Parent.Parent.Parent, allowNamedArgumentName:=False) Then

                Dim collectionInitializer = DirectCast(expression.Parent.Parent.Parent, ObjectCreationExpressionSyntax)
                If collectionInitializer.Initializer Is expression.Parent.Parent Then
                    Return GetCollectionInitializerAddSymbolInfo(collectionInitializer, expression, cancellationToken)
                End If
            End If

            Return SymbolInfo.None
        End Function

        ''' <summary>
        ''' Returns what symbol(s), if any, the given cref reference syntax bound to in the documentation comment.
        ''' 
        ''' An AliasSymbol will never be returned by this method. What the alias refers to will be
        ''' returned instead.
        ''' </summary>
        Public Shadows Function GetSymbolInfo(crefReference As CrefReferenceSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            CheckSyntaxNode(crefReference)
            Return GetCrefReferenceSymbolInfo(crefReference, SymbolInfoOptions.DefaultOptions, cancellationToken)
        End Function

        ''' <summary>
        ''' Binds the expression in the context of the specified location and get semantic
        ''' information such as type, symbols and diagnostics. This method is used to get semantic
        ''' information about an expression that did not actually appear in the source code.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and
        ''' accessibility. This character position must be within the FullSpan of the Root syntax
        ''' node in this SemanticModel.
        ''' </param>
        ''' <param name="expression">A syntax node that represents a parsed expression. This syntax
        ''' node need not and typically does not appear in the source code referred to  SemanticModel
        ''' instance.</param>
        ''' <param name="bindingOption">Indicates whether to binding the expression as a full expressions,
        ''' or as a type or namespace. If SpeculativeBindingOption.BindAsTypeOrNamespace is supplied, then
        ''' expression should derive from TypeSyntax.</param>
        ''' <returns>The semantic information for the topmost node of the expression.</returns>
        ''' <remarks>The passed in expression is interpreted as a stand-alone expression, as if it
        ''' appeared by itself somewhere within the scope that encloses "position".</remarks>
        Public Shadows Function GetSpeculativeSymbolInfo(position As Integer, expression As ExpressionSyntax, bindingOption As SpeculativeBindingOption) As SymbolInfo
            Dim binder As Binder = Nothing ' Passed ByRef to GetSpeculativelyBoundNodeSummary.
            Dim bnodeSummary = GetSpeculativelyBoundNodeSummary(position, expression, bindingOption, binder)

            If bnodeSummary.LowestBoundNode IsNot Nothing Then
                Return Me.GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, bnodeSummary, binder)
            Else
                Return SymbolInfo.None
            End If
        End Function

        ''' <summary>
        ''' Bind the attribute in the context of the specified location and get semantic information
        ''' such as type, symbols and diagnostics. This method is used to get semantic information about an attribute
        ''' that did not actually appear in the source code.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and accessibility. This
        ''' character position must be within the FullSpan of the Root syntax node in this SemanticModel. In order to obtain
        ''' the correct scoping rules for the attribute, position should be the Start position of the Span of the symbol that
        ''' the attribute is being applied to.
        ''' </param>
        ''' <param name="attribute">A syntax node that represents a parsed attribute. This syntax node
        ''' need not and typically does not appear in the source code referred to SemanticModel instance.</param>
        ''' <returns>The semantic information for the topmost node of the attribute.</returns>
        Public Shadows Function GetSpeculativeSymbolInfo(position As Integer, attribute As AttributeSyntax) As SymbolInfo
            Dim binder As Binder = Nothing ' Passed ByRef to GetSpeculativelyBoundNodeSummary.
            Dim bnodeSummary = GetSpeculativelyBoundAttributeSummary(position, attribute, binder)

            If bnodeSummary.LowestBoundNode IsNot Nothing Then
                Return Me.GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, bnodeSummary, binder)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Public Shadows Function GetSymbolInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            CheckSyntaxNode(attribute)

            If CanGetSemanticInfo(attribute) Then
                Return GetAttributeSymbolInfo(attribute, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        ' Gets the symbol info from a specific bound node
        Friend Function GetSymbolInfoForNode(options As SymbolInfoOptions, boundNodes As BoundNodeSummary, binderOpt As Binder) As SymbolInfo
            ' Determine the symbols, resultKind, and member group.
            Dim resultKind As LookupResultKind = LookupResultKind.Empty
            Dim memberGroup As ImmutableArray(Of Symbol) = Nothing
            Dim symbols As ImmutableArray(Of Symbol) = GetSemanticSymbols(boundNodes, binderOpt, options, resultKind, memberGroup)

            Return SymbolInfoFactory.Create(symbols, resultKind)
        End Function

        ''' <summary>
        ''' Gets type information about an expression.
        ''' </summary>
        ''' <param name="expression">The syntax node to get type information for.</param>
        Public Shadows Function GetTypeInfo(expression As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As TypeInfo
            Return GetTypeInfoWorker(expression, cancellationToken)
        End Function

        Friend Overloads Function GetTypeInfoWorker(expression As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            CheckSyntaxNode(expression)

            If CanGetSemanticInfo(expression) Then
                If SyntaxFacts.IsNamedArgumentName(expression) Then
                    Return VisualBasicTypeInfo.None
                Else
                    Return GetExpressionTypeInfo(expression, cancellationToken)
                End If
            Else
                Return VisualBasicTypeInfo.None
            End If
        End Function

        ''' <summary>
        ''' Binds the expression in the context of the specified location and gets type information.
        ''' This method is used to get type information about an expression that did not actually
        ''' appear in the source code.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and
        ''' accessibility. This character position must be within the FullSpan of the Root syntax
        ''' node in this SemanticModel.
        ''' </param>
        ''' <param name="expression">A syntax node that represents a parsed expression. This syntax
        ''' node need not and typically does not appear in the source code referred to by the
        ''' SemanticModel instance.</param>
        ''' <param name="bindingOption">Indicates whether to binding the expression as a full expressions,
        ''' or as a type or namespace. If SpeculativeBindingOption.BindAsTypeOrNamespace is supplied, then
        ''' expression should derive from TypeSyntax.</param>
        ''' <returns>The type information for the topmost node of the expression.</returns>
        ''' <remarks>The passed in expression is interpreted as a stand-alone expression, as if it
        ''' appeared by itself somewhere within the scope that encloses "position".</remarks>
        Public Shadows Function GetSpeculativeTypeInfo(position As Integer, expression As ExpressionSyntax, bindingOption As SpeculativeBindingOption) As TypeInfo
            Return GetSpeculativeTypeInfoWorker(position, expression, bindingOption)
        End Function

        Friend Function GetSpeculativeTypeInfoWorker(position As Integer, expression As ExpressionSyntax, bindingOption As SpeculativeBindingOption) As VisualBasicTypeInfo
            Dim binder As Binder = Nothing ' passed ByRef to GetSpeculativelyBoundNodeSummary
            Dim bnodeSummary = GetSpeculativelyBoundNodeSummary(position, expression, bindingOption, binder)

            If bnodeSummary.LowestBoundNode IsNot Nothing Then
                Return Me.GetTypeInfoForNode(bnodeSummary)
            Else
                Return VisualBasicTypeInfo.None
            End If
        End Function

        Public Shadows Function GetTypeInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As TypeInfo
            Return GetTypeInfoWorker(attribute, cancellationToken)
        End Function

        Private Overloads Function GetTypeInfoWorker(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            CheckSyntaxNode(attribute)

            If CanGetSemanticInfo(attribute) Then
                Return GetAttributeTypeInfo(attribute, cancellationToken)
            Else
                Return VisualBasicTypeInfo.None
            End If
        End Function

        ' Gets the type info from a specific bound node
        Friend Function GetTypeInfoForNode(boundNodes As BoundNodeSummary) As VisualBasicTypeInfo
            ' Determine the type, converted type, and expression
            Dim type As TypeSymbol = Nothing
            Dim convertedType As TypeSymbol = Nothing
            Dim conversion As Conversion = Nothing
            type = GetSemanticType(boundNodes, convertedType, conversion)

            Return New VisualBasicTypeInfo(type, convertedType, conversion)
        End Function

        ''' <summary>
        ''' Gets the conversion that occurred between the expression's type and type implied by the expression's context.
        ''' </summary>
        Public Function GetConversion(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As Conversion
            Dim expression = TryCast(node, ExpressionSyntax)
            If expression IsNot Nothing Then
                Return GetTypeInfoWorker(expression, cancellationToken).ImplicitConversion
            End If

            Dim attribute = TryCast(node, AttributeSyntax)
            If attribute IsNot Nothing Then
                Return GetTypeInfoWorker(attribute, cancellationToken).ImplicitConversion
            End If

            Return VisualBasicTypeInfo.None.ImplicitConversion
        End Function

        ''' <summary>
        ''' Gets the conversion that occurred between the expression's type and type implied by the expression's context.
        ''' </summary>
        Public Function GetSpeculativeConversion(position As Integer, expression As ExpressionSyntax, bindingOption As SpeculativeBindingOption) As Conversion
            Return GetSpeculativeTypeInfoWorker(position, expression, bindingOption).ImplicitConversion
        End Function

        Public Shadows Function GetConstantValue(expression As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As [Optional](Of Object)
            CheckSyntaxNode(expression)

            If CanGetSemanticInfo(expression) Then
                Dim val As ConstantValue = GetExpressionConstantValue(expression, cancellationToken)

                If val IsNot Nothing AndAlso Not val.IsBad Then
                    Return New [Optional](Of Object)(val.Value)
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Binds the expression in the context of the specified location and gets constant value information. 
        ''' This method is used to get information about an expression that did not actually appear in the source code.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and
        ''' accessibility. This character position must be within the FullSpan of the Root syntax
        ''' node in this SemanticModel.
        ''' </param>
        ''' <param name="expression">A syntax node that represents a parsed expression. This syntax
        ''' node need not and typically does not appear in the source code referred to by SemanticModel
        ''' instance.</param>
        ''' <remarks>The passed in expression is interpreted as a stand-alone expression, as if it
        ''' appeared by itself somewhere within the scope that encloses "position".</remarks>
        Public Shadows Function GetSpeculativeConstantValue(position As Integer, expression As ExpressionSyntax) As [Optional](Of Object)
            Dim binder As Binder = Nothing ' passed ByRef to GetSpeculativelyBoundNodeSummary
            Dim bnodeSummary = GetSpeculativelyBoundNodeSummary(position, expression, SpeculativeBindingOption.BindAsExpression, binder)

            If bnodeSummary.LowestBoundNode IsNot Nothing Then
                Dim val As ConstantValue = Me.GetConstantValueForNode(bnodeSummary)

                If val IsNot Nothing AndAlso Not val.IsBad Then
                    Return New [Optional](Of Object)(val.Value)
                End If
            End If

            Return Nothing
        End Function

        Friend Function GetConstantValueForNode(boundNodes As BoundNodeSummary) As ConstantValue
            Dim constValue As ConstantValue = Nothing
            Dim lowerExpr = TryCast(boundNodes.LowestBoundNode, BoundExpression)
            If lowerExpr IsNot Nothing Then
                constValue = lowerExpr.ConstantValueOpt
            End If

            Return constValue
        End Function

        Public Shadows Function GetMemberGroup(expression As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            CheckSyntaxNode(expression)

            If CanGetSemanticInfo(expression) Then
                Dim result = GetExpressionMemberGroup(expression, cancellationToken)
#If DEBUG Then
                For Each item In result
                    Debug.Assert(item.Kind <> SymbolKind.Namespace)
                Next
#End If
                Return StaticCast(Of ISymbol).From(result)
            Else
                Return ImmutableArray(Of ISymbol).Empty
            End If
        End Function

        Public Shadows Function GetSpeculativeMemberGroup(position As Integer, expression As ExpressionSyntax) As ImmutableArray(Of ISymbol)
            Dim binder As Binder = Nothing ' passed ByRef to GetSpeculativelyBoundNodeSummary
            Dim bnodeSummary = GetSpeculativelyBoundNodeSummary(position, expression, SpeculativeBindingOption.BindAsExpression, binder)

            If bnodeSummary.LowestBoundNode IsNot Nothing Then
                Dim result = Me.GetMemberGroupForNode(bnodeSummary, binderOpt:=Nothing)
#If DEBUG Then
                For Each item In result
                    Debug.Assert(item.Kind <> SymbolKind.Namespace)
                Next
#End If
                Return StaticCast(Of ISymbol).From(result)
            Else
                Return ImmutableArray(Of ISymbol).Empty
            End If
        End Function

        Public Shadows Function GetMemberGroup(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            CheckSyntaxNode(attribute)

            If CanGetSemanticInfo(attribute) Then
                Dim result = GetAttributeMemberGroup(attribute, cancellationToken)
#If DEBUG Then
                For Each item In result
                    Debug.Assert(item.Kind <> SymbolKind.Namespace)
                Next
#End If
                Return StaticCast(Of ISymbol).From(result)
            Else
                Return ImmutableArray(Of ISymbol).Empty
            End If
        End Function

        Friend Function GetMemberGroupForNode(boundNodes As BoundNodeSummary, binderOpt As Binder) As ImmutableArray(Of Symbol)
            ' Determine the symbols, resultKind, and member group.
            Dim resultKind As LookupResultKind = LookupResultKind.Empty
            Dim memberGroup As ImmutableArray(Of Symbol) = Nothing
            Dim symbols As ImmutableArray(Of Symbol) = GetSemanticSymbols(boundNodes, binderOpt, SymbolInfoOptions.DefaultOptions, resultKind, memberGroup)

            Return memberGroup
        End Function

        ''' <summary>
        ''' If "nameSyntax" resolves to an alias name, return the AliasSymbol corresponding
        ''' to A. Otherwise return null.
        ''' </summary>
        Public Shadows Function GetAliasInfo(nameSyntax As IdentifierNameSyntax, Optional cancellationToken As CancellationToken = Nothing) As IAliasSymbol
            CheckSyntaxNode(nameSyntax)

            If CanGetSemanticInfo(nameSyntax) Then
                Dim info = GetExpressionSymbolInfo(nameSyntax, SymbolInfoOptions.PreferTypeToConstructors Or SymbolInfoOptions.PreserveAliases, cancellationToken)
                Return TryCast(info.Symbol, IAliasSymbol)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Binds the name in the context of the specified location and sees if it resolves to an
        ''' alias name. If it does, return the AliasSymbol corresponding to it. Otherwise, return null.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and
        ''' accessibility. This character position must be within the FullSpan of the Root syntax
        ''' node in this SemanticModel.
        ''' </param>
        ''' <param name="nameSyntax">A syntax node that represents a name. This syntax
        ''' node need not and typically does not appear in the source code referred to by the
        ''' SemanticModel instance.</param>
        ''' <param name="bindingOption">Indicates whether to binding the name as a full expression,
        ''' or as a type or namespace. If SpeculativeBindingOption.BindAsTypeOrNamespace is supplied, then
        ''' expression should derive from TypeSyntax.</param>
        ''' <remarks>The passed in name is interpreted as a stand-alone name, as if it
        ''' appeared by itself somewhere within the scope that encloses "position".</remarks>
        Public Shadows Function GetSpeculativeAliasInfo(position As Integer, nameSyntax As IdentifierNameSyntax, bindingOption As SpeculativeBindingOption) As IAliasSymbol
            Dim binder As Binder = Nothing
            Dim bnodeSummary = GetSpeculativelyBoundNodeSummary(position, nameSyntax, bindingOption, binder)

            If bnodeSummary.LowestBoundNode IsNot Nothing Then
                Dim info As SymbolInfo = Me.GetSymbolInfoForNode(SymbolInfoOptions.PreferTypeToConstructors Or SymbolInfoOptions.PreserveAliases, bnodeSummary, binderOpt:=binder)
                Return TryCast(info.Symbol, IAliasSymbol)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Gets the binder that encloses the position. See comment on LookupSymbols for how
        ''' positions are interpreted.
        ''' </summary>
        Friend MustOverride Function GetEnclosingBinder(position As Integer) As Binder

        Friend Function IsInTree(node As SyntaxNode) As Boolean
            Return IsUnderNode(node, Me.Root)
        End Function

        Private Shared Function IsUnderNode(node As SyntaxNode, root As SyntaxNode) As Boolean
            While node IsNot Nothing
                If node Is root Then
                    Return True
                End If
                If node.IsStructuredTrivia Then
                    node = DirectCast(node, StructuredTriviaSyntax).ParentTrivia.Token.Parent
                Else
                    node = node.Parent
                End If
            End While
            Return False
        End Function

        ' Checks that a position is within the span of the root of this binding. 
        Protected Sub CheckPosition(position As Integer)
            Dim fullStart As Integer = Root.Position
            Dim fullEnd As Integer = Root.EndPosition

            ' Is position at the actual end of file (not just end of Root.FullSpan)?
            Dim atEOF As Boolean = (position = fullEnd AndAlso position = SyntaxTree.GetRoot().FullSpan.End)

            If (fullStart <= position AndAlso position < fullEnd) OrElse
                atEOF OrElse
                (fullStart = fullEnd AndAlso position = fullEnd) Then
                Return
            End If

            Throw New ArgumentException(VBResources.PositionIsNotWithinSyntax)
        End Sub

        Friend Sub CheckSyntaxNode(node As VisualBasicSyntaxNode)
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            If Not IsInTree(node) Then
                Throw New ArgumentException(VBResources.NodeIsNotWithinSyntaxTree)
            End If
        End Sub

        Private Sub CheckModelAndSyntaxNodeToSpeculate(node As VisualBasicSyntaxNode)
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            If Me.IsSpeculativeSemanticModel Then
                Throw New InvalidOperationException(VBResources.ChainingSpeculativeModelIsNotSupported)
            End If

            If Me.Compilation.ContainsSyntaxTree(node.SyntaxTree) Then
                Throw New ArgumentException(VBResources.SpeculatedSyntaxNodeCannotBelongToCurrentCompilation)
            End If
        End Sub

        ' Find the initial syntax node to start traversing up the syntax when getting binders from 
        ' a position. Just using FindToken doesn't give quite the right results, especially in situations where
        ' end constructs haven't been typed yet. If we are in the trivia between two tokens, we move backward to the previous
        ' token. There are also some special cases around beginning and end of the whole tree.
        Friend Function FindInitialNodeFromPosition(position As Integer) As SyntaxNode
            Dim fullStart As Integer = Root.Position
            Dim fullEnd As Integer = Root.EndPosition

            ' Is position at the actual end of file (not just end of Root.FullSpan)?
            Dim atEOF As Boolean = (position = fullEnd AndAlso position = SyntaxTree.GetRoot().FullSpan.End)

            If (fullStart <= position AndAlso position < fullEnd) OrElse atEOF Then
                Dim token As SyntaxToken

                If atEOF Then
                    token = SyntaxTree.GetRoot().FindToken(position, True)
                Else
                    token = Root.FindToken(position, True)
                End If

                Dim trivia As StructuredTriviaSyntax = DirectCast(token.Parent, VisualBasicSyntaxNode).EnclosingStructuredTrivia
                If trivia Is Nothing OrElse Not IsInCrefOrNameAttributeInterior(DirectCast(token.Parent, VisualBasicSyntaxNode)) Then
                    If atEOF Then
                        token = SyntaxTree.GetRoot().FindToken(position)
                    Else
                        token = Root.FindToken(position)
                    End If
                End If

                If (position < token.SpanStart) Then
                    ' Before the start of this token, go to previous token.
                    token = token.GetPreviousToken(includeSkipped:=False, includeDirectives:=False, includeDocumentationComments:=False)
                End If

                ' If the first token in the root is missing, it's possible to step backwards
                ' past the start of the root.  All sorts of bad things will happen in that case,
                ' so just use the root.
                If token.SpanStart < fullStart Then
                    Return Root
                ElseIf token.Parent IsNot Nothing Then
                    Debug.Assert(IsInTree(token.Parent))
                    Return DirectCast(token.Parent, VisualBasicSyntaxNode)
                Else
                    Return Root
                End If

            ElseIf fullStart = fullEnd AndAlso position = fullEnd Then
                ' The root is an empty span and isn't the full compilation unit. No other choice here.
                Return Root
            End If

            ' Should have been caught by CheckPosition
            Throw ExceptionUtilities.Unreachable
        End Function

        ' Is this node in a place where it bind to an implemented member.
        Friend Shared Function IsInCrefOrNameAttributeInterior(node As VisualBasicSyntaxNode) As Boolean
            Debug.Assert(node IsNot Nothing)

            Select Case node.Kind
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName,
                     SyntaxKind.PredefinedType,
                     SyntaxKind.QualifiedName,
                     SyntaxKind.GlobalName,
                     SyntaxKind.QualifiedCrefOperatorReference,
                     SyntaxKind.CrefOperatorReference,
                     SyntaxKind.CrefReference,
                     SyntaxKind.XmlString
                    ' fall through

                Case Else
                    Return False
            End Select

            Dim parent As VisualBasicSyntaxNode = node.Parent
            Dim inXmlAttribute As Boolean = False

            While parent IsNot Nothing
                Select Case parent.Kind
                    Case SyntaxKind.XmlCrefAttribute,
                         SyntaxKind.XmlNameAttribute
                        Return True

                    Case SyntaxKind.XmlAttribute
                        inXmlAttribute = True
                        parent = parent.Parent

                    Case SyntaxKind.DocumentationCommentTrivia
                        If inXmlAttribute Then
                            Return True
                        End If

                        parent = parent.Parent

                    Case Else
                        parent = parent.Parent
                End Select
            End While

            Return False
        End Function

        Friend Function GetSpeculativeBinderForExpression(position As Integer, expression As ExpressionSyntax, bindingOption As SpeculativeBindingOption) As SpeculativeBinder
            Debug.Assert(expression IsNot Nothing)

            CheckPosition(position)

            If bindingOption = SpeculativeBindingOption.BindAsTypeOrNamespace Then
                If TryCast(expression, TypeSyntax) Is Nothing Then
                    Return Nothing
                End If
            End If

            Dim binder = Me.GetEnclosingBinder(position)

            ' Add speculative binder to bind speculatively.
            Return If(binder IsNot Nothing, SpeculativeBinder.Create(binder), Nothing)
        End Function

        Private Function GetSpeculativelyBoundNode(
            binder As Binder,
            expression As ExpressionSyntax,
            bindingOption As SpeculativeBindingOption
        ) As BoundNode
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)
            Debug.Assert(expression IsNot Nothing)
            Debug.Assert(bindingOption <> SpeculativeBindingOption.BindAsTypeOrNamespace OrElse
                         TryCast(expression, TypeSyntax) IsNot Nothing)

            Dim bnode As BoundNode
            If bindingOption = SpeculativeBindingOption.BindAsTypeOrNamespace Then
                bnode = binder.BindNamespaceOrTypeExpression(DirectCast(expression, TypeSyntax), BindingDiagnosticBag.Discarded)
            Else
                Debug.Assert(bindingOption = SpeculativeBindingOption.BindAsExpression)
                bnode = Me.Bind(binder, expression, BindingDiagnosticBag.Discarded)
                bnode = MakeValueIfPossible(binder, bnode)
            End If

            Return bnode
        End Function

        Friend Function GetSpeculativelyBoundNode(position As Integer,
                                                  expression As ExpressionSyntax,
                                                  bindingOption As SpeculativeBindingOption,
                                                  <Out> ByRef binder As Binder) As BoundNode
            Debug.Assert(expression IsNot Nothing)

            binder = Me.GetSpeculativeBinderForExpression(position, expression, bindingOption)
            If binder IsNot Nothing Then
                Dim bnode = Me.GetSpeculativelyBoundNode(binder, expression, bindingOption)
                Return bnode
            Else
                Return Nothing
            End If
        End Function

        Private Function GetSpeculativelyBoundNodeSummary(position As Integer,
                                                   expression As ExpressionSyntax,
                                                   bindingOption As SpeculativeBindingOption,
                                                   <Out> ByRef binder As Binder) As BoundNodeSummary
            If expression Is Nothing Then
                Throw New ArgumentNullException(NameOf(expression))
            End If

            Dim standalone = SyntaxFactory.GetStandaloneExpression(expression)

            Dim bnode = Me.GetSpeculativelyBoundNode(position, standalone, bindingOption, binder)
            If bnode IsNot Nothing Then
                Debug.Assert(binder IsNot Nothing)
                Return New BoundNodeSummary(bnode, bnode, Nothing)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' When doing speculative binding, we don't have any context information about expressions or the
        ''' context that is expected. We try to interpret as a value, but
        ''' only if it doesn't cause additional errors (indicating that it wasn't value to interpret it
        ''' that way). This should get us the most "liberal" interpretation
        ''' for semantic information.
        ''' </summary>
        Private Function MakeValueIfPossible(binder As Binder, node As BoundNode) As BoundNode
            ' Convert a stand-alone speculatively bound expression to an rvalue. 
            ' This will get the value of properties, convert lambdas to anonymous 
            ' delegate type, etc.
            Dim boundExpression = TryCast(node, BoundExpression)
            If boundExpression IsNot Nothing Then
                ' Try calling ReclassifyAsValue
                Dim diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)
                Dim resultNode = binder.ReclassifyAsValue(boundExpression, diagnostics)

                ' Reclassify ArrayLiterals and other expressions missing types to expressions with types.
                If Not resultNode.HasErrors AndAlso resultNode.Type Is Nothing Then
                    resultNode = binder.ReclassifyExpression(resultNode, diagnostics)
                End If

                Dim noErrors As Boolean = Not diagnostics.HasAnyErrors()
                diagnostics.Free()
                If noErrors Then
                    Return resultNode
                End If
            End If

            Return node
        End Function

        Private Function GetSpeculativeAttributeBinder(position As Integer, attribute As AttributeSyntax) As AttributeBinder
            Debug.Assert(attribute IsNot Nothing)

            CheckPosition(position)

            Dim binder = Me.GetEnclosingBinder(position)

            ' Add speculative attribute binder to bind speculatively.
            Return If(binder IsNot Nothing,
                      BinderBuilder.CreateBinderForAttribute(binder.SyntaxTree, binder, attribute),
                      Nothing)
        End Function

        ''' <summary>
        ''' Bind the given attribute speculatively at the given position, and return back
        ''' the resulting bound node. May return null in some error cases.
        ''' </summary>
        Friend Function GetSpeculativelyBoundAttribute(position As Integer, attribute As AttributeSyntax, <Out> ByRef binder As Binder) As BoundAttribute
            binder = Me.GetSpeculativeAttributeBinder(position, attribute)

            If binder IsNot Nothing Then
                Dim bnode As BoundAttribute = binder.BindAttribute(attribute, BindingDiagnosticBag.Discarded)
                Return bnode
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Bind the given attribute speculatively at the given position, and return back
        ''' the resulting bound node summary. May return null in some error cases.
        ''' </summary>
        Private Function GetSpeculativelyBoundAttributeSummary(position As Integer, attribute As AttributeSyntax, <Out> ByRef binder As Binder) As BoundNodeSummary
            If attribute Is Nothing Then
                Throw New ArgumentNullException(NameOf(attribute))
            End If

            Dim bnode = GetSpeculativelyBoundAttribute(position, attribute, binder)
            If bnode IsNot Nothing Then
                Debug.Assert(binder IsNot Nothing)
                Return New BoundNodeSummary(bnode, bnode, Nothing)
            Else
                Return Nothing
            End If
        End Function

        ' Given a diagnosticInfo, add any symbols from the diagnosticInfo to the symbol builder.
        Private Sub AddSymbolsFromDiagnosticInfo(symbolsBuilder As ArrayBuilder(Of Symbol), diagnosticInfo As DiagnosticInfo)
            Dim diagInfoWithSymbols = TryCast(diagnosticInfo, IDiagnosticInfoWithSymbols)
            If diagInfoWithSymbols IsNot Nothing Then
                diagInfoWithSymbols.GetAssociatedSymbols(symbolsBuilder)
            End If
        End Sub

        ' Given a symbolsBuilder with a bunch of symbols in it, return an ImmutableArray containing
        ' just the symbols that are not ErrorTypeSymbols, and without any duplicates.
        Friend Function RemoveErrorTypesAndDuplicates(symbolsBuilder As ArrayBuilder(Of Symbol), options As SymbolInfoOptions) As ImmutableArray(Of Symbol)
            ' Common case is 0 or 1 symbol, so we optimize those cases to not allocate a HashSet, since
            ' duplicates aren't possible for those cases.

            If symbolsBuilder.Count = 0 Then
                Return ImmutableArray(Of Symbol).Empty
            ElseIf symbolsBuilder.Count = 1 Then
                Dim s = symbolsBuilder(0)
                If (options And SymbolInfoOptions.ResolveAliases) <> 0 Then
                    s = UnwrapAlias(s)
                End If

                If TypeOf s Is ErrorTypeSymbol Then
                    symbolsBuilder.Clear()
                    AddSymbolsFromDiagnosticInfo(symbolsBuilder, DirectCast(s, ErrorTypeSymbol).ErrorInfo)
                    Return symbolsBuilder.ToImmutable()
                Else
                    Return ImmutableArray.Create(s)
                End If
            Else
                ' 2 or more symbols. Use a hash set to remove duplicates.
                Dim symbolSet = PooledHashSet(Of Symbol).GetInstance()
                For Each s In symbolsBuilder
                    If (options And SymbolInfoOptions.ResolveAliases) <> 0 Then
                        s = UnwrapAlias(s)
                    End If

                    If TypeOf s Is ErrorTypeSymbol Then
                        Dim tempBuilder As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()
                        AddSymbolsFromDiagnosticInfo(tempBuilder, DirectCast(s, ErrorTypeSymbol).ErrorInfo)
                        For Each sym In tempBuilder
                            symbolSet.Add(sym)
                        Next
                        tempBuilder.Free()
                    Else
                        symbolSet.Add(s)
                    End If
                Next

                Dim result = ImmutableArray.CreateRange(symbolSet)
                symbolSet.Free()
                Return result
            End If
        End Function

        ' Given the lower and upper bound expressions, get the type, converted type, and conversion.
        Private Function GetSemanticType(boundNodes As BoundNodeSummary,
                   ByRef convertedType As TypeSymbol,
                   ByRef conversion As Conversion) As TypeSymbol
            convertedType = Nothing
            conversion = New Conversion(Conversions.Identity)

            Dim lowestExpr = TryCast(boundNodes.LowestBoundNode, BoundExpression)
            Dim highestExpr = TryCast(boundNodes.HighestBoundNode, BoundExpression)

            If lowestExpr Is Nothing Then
                ' Only BoundExpressions have a type.
                Return Nothing
            End If

            Dim type As TypeSymbol

            ' Similar to a lambda expression, array literal doesn't have a type.
            ' However, during binding we create BoundArrayCreation node that has type the literal got converted to.
            ' Let's account for that.
            If lowestExpr.Kind = BoundKind.ArrayCreation AndAlso DirectCast(lowestExpr, BoundArrayCreation).ArrayLiteralOpt IsNot Nothing Then
                type = Nothing
                conversion = New Conversion(New KeyValuePair(Of ConversionKind, MethodSymbol)(DirectCast(lowestExpr, BoundArrayCreation).ArrayLiteralConversion, Nothing))
            ElseIf lowestExpr.Kind = BoundKind.ConvertedTupleLiteral Then
                type = DirectCast(lowestExpr, BoundConvertedTupleLiteral).NaturalTypeOpt
            Else
                type = lowestExpr.Type
            End If

            Dim useOfLocalBeforeDeclaration As Boolean = False

            ' Use of local before declaration requires some additional fixup.
            ' Due complications around implicit locals and type inference, we do not
            ' try to obtain a type of a local when it is used before declaration, we use
            ' a special error type symbol. However, semantic model should return the same
            ' type information for usage of a local before and after its declaration.
            ' We will detect the use before declaration cases and replace the error type
            ' symbol with the one obtained from the local. It is safe to get the type
            ' from the local at this point because we have already bound the whole method
            ' body if implicit locals were allowed.
            If type Is LocalSymbol.UseBeforeDeclarationResultType AndAlso
               lowestExpr.Kind = BoundKind.Local Then
                useOfLocalBeforeDeclaration = True
                type = DirectCast(lowestExpr, BoundLocal).LocalSymbol.Type
            End If

            ' See if the node is being implicitly converted to another type. If so, there would
            ' be a higher conversion node associated to the same syntax node.

            If highestExpr IsNot Nothing Then
                If highestExpr.Type IsNot Nothing AndAlso highestExpr.Type.TypeKind <> TypeKind.Error Then
                    convertedType = highestExpr.Type
                    If (type Is Nothing OrElse Not type.IsSameTypeIgnoringAll(convertedType)) Then
                        ' If the upper expression is of a different type, we want to return
                        ' a conversion. Hopefully we have a conversion node. 
                        ' TODO: Understand cases where we don't have a conversion node better.
                        If highestExpr.Kind = BoundKind.Conversion Then
                            Dim conversionNode = DirectCast(highestExpr, BoundConversion)

                            If useOfLocalBeforeDeclaration AndAlso Not type.IsErrorType() Then
                                conversion = New Conversion(Conversions.ClassifyConversion(type, convertedType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
                            Else
                                conversion = New Conversion(KeyValuePair.Create(conversionNode.ConversionKind,
                                                                                TryCast(conversionNode.ExpressionSymbol, MethodSymbol)))
                            End If
                        End If
                    End If
                End If
            End If

            If type Is Nothing AndAlso TypeOf boundNodes.LowestBoundNodeOfSyntacticParent Is BoundBadExpression Then
                ' Special case: overload failure on X in New X(...), where overload resolution failed. 
                ' Binds to method group which can't have a type.

                Dim parentSyntax As SyntaxNode = boundNodes.LowestBoundNodeOfSyntacticParent.Syntax
                If parentSyntax IsNot Nothing AndAlso
                   parentSyntax Is boundNodes.LowestBoundNode.Syntax.Parent AndAlso
                   ((parentSyntax.Kind = SyntaxKind.ObjectCreationExpression AndAlso (DirectCast(parentSyntax, ObjectCreationExpressionSyntax).Type Is boundNodes.LowestBoundNode.Syntax))) Then
                    type = DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundBadExpression).Type
                End If
            End If

            ' If we didn't have a converted type, then use the type.
            If convertedType Is Nothing Then
                convertedType = type
            End If

            Return type
        End Function

        ' Given the lower and upper bound expressions, get the symbols, resultkind, and member group.
        Private Function GetSemanticSymbols(boundNodes As BoundNodeSummary,
                 binderOpt As Binder,
                 options As SymbolInfoOptions,
                 ByRef resultKind As LookupResultKind,
                 ByRef memberGroup As ImmutableArray(Of Symbol)) As ImmutableArray(Of Symbol)
            ' TODO: Understand the case patched by TODO in GetSemanticInfoForNode and create a better fix.

            Dim symbolsBuilder = ArrayBuilder(Of Symbol).GetInstance()
            Dim memberGroupBuilder = ArrayBuilder(Of Symbol).GetInstance()
            resultKind = LookupResultKind.Good  ' assume good unless we find out otherwise.

            If boundNodes.LowestBoundNode IsNot Nothing Then
                Select Case boundNodes.LowestBoundNode.Kind
                    Case BoundKind.MethodGroup
                        ' Complex enough to split out into its own function.
                        GetSemanticSymbolsForMethodGroup(boundNodes, symbolsBuilder, memberGroupBuilder, resultKind)

                    Case BoundKind.PropertyGroup
                        ' Complex enough to split out into its own function.
                        GetSemanticSymbolsForPropertyGroup(boundNodes, symbolsBuilder, memberGroupBuilder, resultKind)

                    Case BoundKind.TypeExpression

                        ' Watch out for not creatable types within object creation syntax
                        If boundNodes.LowestBoundNodeOfSyntacticParent IsNot Nothing AndAlso
                           boundNodes.LowestBoundNodeOfSyntacticParent.Syntax.Kind = SyntaxKind.ObjectCreationExpression AndAlso
                           DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent.Syntax, ObjectCreationExpressionSyntax).Type Is boundNodes.LowestBoundNode.Syntax AndAlso
                           boundNodes.LowestBoundNodeOfSyntacticParent.Kind = BoundKind.BadExpression AndAlso
                           DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundBadExpression).ResultKind = LookupResultKind.NotCreatable Then
                            resultKind = LookupResultKind.NotCreatable
                        End If

                        ' We want to return the alias symbol if one exists, otherwise the type symbol.
                        ' If its an error type look at underlying symbols and kind.
                        ' The alias symbol is latter mapped to its target depending on options in RemoveErrorTypesAndDuplicates.
                        Dim boundType = DirectCast(boundNodes.LowestBoundNode, BoundTypeExpression)
                        If boundType.AliasOpt IsNot Nothing Then
                            symbolsBuilder.Add(boundType.AliasOpt)
                        Else
                            Dim typeSymbol As TypeSymbol = boundType.Type
                            Dim originalErrorType = TryCast(typeSymbol.OriginalDefinition, ErrorTypeSymbol)
                            If originalErrorType IsNot Nothing Then
                                resultKind = originalErrorType.ResultKind
                                symbolsBuilder.AddRange(originalErrorType.CandidateSymbols)
                            Else
                                symbolsBuilder.Add(typeSymbol)
                            End If
                        End If

                    Case BoundKind.Attribute
                        Debug.Assert(boundNodes.LowestBoundNodeOfSyntacticParent Is Nothing)
                        Dim attribute = DirectCast(boundNodes.LowestBoundNode, BoundAttribute)
                        resultKind = attribute.ResultKind

                        ' If attribute name bound to a single named type or an error type
                        ' with a single named type candidate symbol, we will return constructors
                        ' of the named type in the semantic info.
                        ' Otherwise, we will return the error type candidate symbols.

                        Dim namedType = DirectCast(attribute.Type, NamedTypeSymbol)
                        If namedType.IsErrorType() Then
                            Debug.Assert(resultKind <> LookupResultKind.Good)
                            Dim errorType = DirectCast(namedType, ErrorTypeSymbol)
                            Dim candidateSymbols = errorType.CandidateSymbols

                            ' If error type has a single named type candidate symbol, we want to 
                            ' use that type for symbol info. 
                            If candidateSymbols.Length = 1 AndAlso candidateSymbols(0).Kind = SymbolKind.NamedType Then
                                namedType = DirectCast(errorType.CandidateSymbols(0), NamedTypeSymbol)
                            Else
                                symbolsBuilder.AddRange(candidateSymbols)
                                Exit Select
                            End If

                        End If

                        Dim symbols = ImmutableArray(Of Symbol).Empty
                        AdjustSymbolsForObjectCreation(attribute, namedType, attribute.Constructor, binderOpt, symbols, memberGroupBuilder, resultKind)
                        symbolsBuilder.AddRange(symbols)

                    Case BoundKind.ObjectCreationExpression

                        Dim creation = DirectCast(boundNodes.LowestBoundNode, BoundObjectCreationExpression)

                        If creation.MethodGroupOpt IsNot Nothing Then
                            creation.MethodGroupOpt.GetExpressionSymbols(memberGroupBuilder)
                            resultKind = creation.MethodGroupOpt.ResultKind
                        End If

                        If creation.ConstructorOpt IsNot Nothing Then
                            symbolsBuilder.Add(creation.ConstructorOpt)
                        Else
                            symbolsBuilder.AddRange(memberGroupBuilder)
                        End If

                    Case BoundKind.LateMemberAccess
                        GetSemanticSymbolsForLateBoundMemberAccess(boundNodes, symbolsBuilder, memberGroupBuilder, resultKind)

                    Case BoundKind.LateInvocation
                        Dim lateInvocation = DirectCast(boundNodes.LowestBoundNode, BoundLateInvocation)
                        GetSemanticSymbolsForLateBoundInvocation(lateInvocation, symbolsBuilder, memberGroupBuilder, resultKind)

                    Case BoundKind.MyBaseReference,
                         BoundKind.MeReference,
                         BoundKind.MyClassReference

                        Dim meReference = DirectCast(boundNodes.LowestBoundNode, BoundExpression)
                        Dim binder As Binder = If(binderOpt, GetEnclosingBinder(boundNodes.LowestBoundNode.Syntax.SpanStart))
                        Dim containingType As NamedTypeSymbol = binder.ContainingType
                        Dim containingMember = binder.ContainingMember

                        Dim meParam As ParameterSymbol = GetMeParameter(meReference.Type, containingType, containingMember, resultKind)
                        symbolsBuilder.Add(meParam)

                    Case BoundKind.TypeOrValueExpression
                        ' If we're seeing a node of this kind, then we failed to resolve the member access
                        ' as either a type or a property/field/event/local/parameter.  In such cases,
                        ' the second interpretation applies so just visit the node for that.
                        Dim boundTypeOrValue = DirectCast(boundNodes.LowestBoundNode, BoundTypeOrValueExpression)
                        Dim valueBoundNodes = New BoundNodeSummary(boundTypeOrValue.Data.ValueExpression, boundNodes.HighestBoundNode, boundNodes.LowestBoundNodeOfSyntacticParent)
                        Return GetSemanticSymbols(valueBoundNodes, binderOpt, options, resultKind, memberGroup)

                    Case Else
_Default:
                        ' Currently, only nodes deriving from BoundExpression have symbols or
                        ' resultkind. If this turns out to be too restrictive, we can move them up
                        ' the hierarchy.
                        Dim lowestExpr = TryCast(boundNodes.LowestBoundNode, BoundExpression)

                        If lowestExpr IsNot Nothing Then
                            lowestExpr.GetExpressionSymbols(symbolsBuilder)
                            resultKind = lowestExpr.ResultKind

                            If lowestExpr.Kind = BoundKind.BadExpression AndAlso lowestExpr.Syntax.Kind = SyntaxKind.ObjectCreationExpression Then
                                ' Look for a method group under this bad node
                                Dim typeSyntax = DirectCast(lowestExpr.Syntax, ObjectCreationExpressionSyntax).Type

                                For Each child In DirectCast(lowestExpr, BoundBadExpression).ChildBoundNodes
                                    If child.Kind = BoundKind.MethodGroup AndAlso child.Syntax Is typeSyntax Then
                                        Dim group = DirectCast(child, BoundMethodGroup)
                                        group.GetExpressionSymbols(memberGroupBuilder)

                                        If resultKind = LookupResultKind.NotCreatable Then
                                            resultKind = group.ResultKind
                                        Else
                                            resultKind = LookupResult.WorseResultKind(resultKind, group.ResultKind)
                                        End If

                                        Exit For
                                    End If
                                Next
                            End If
                        End If
                End Select
            End If

            Dim bindingSymbols As ImmutableArray(Of Symbol) = RemoveErrorTypesAndDuplicates(symbolsBuilder, options)
            symbolsBuilder.Free()

            If boundNodes.LowestBoundNodeOfSyntacticParent IsNot Nothing AndAlso (options And SymbolInfoOptions.PreferConstructorsToType) <> 0 Then
                ' Adjust symbols to get the constructors if we're T in a "New T(...)".
                AdjustSymbolsForObjectCreation(boundNodes, binderOpt, bindingSymbols, memberGroupBuilder, resultKind)
            End If

            memberGroup = memberGroupBuilder.ToImmutableAndFree()

            ' We have a different highest bound node than lowest bound node. If it has a result kind less good than the 
            ' one we already determined, use that. This is typically the case where a BoundBadExpression or BoundBadValue
            ' is created around another expression. 
            ' CONSIDER: Can it arise that the highest node has associated symbols but the lowest node doesn't? In that case,
            ' we may wish to use the symbols from the highest.
            Dim highestBoundNodeExpr = TryCast(boundNodes.HighestBoundNode, BoundExpression)
            If highestBoundNodeExpr IsNot Nothing AndAlso boundNodes.HighestBoundNode IsNot boundNodes.LowestBoundNode Then
                If highestBoundNodeExpr.ResultKind <> LookupResultKind.Empty AndAlso highestBoundNodeExpr.ResultKind < resultKind Then
                    resultKind = highestBoundNodeExpr.ResultKind
                End If

                If highestBoundNodeExpr.Kind = BoundKind.BadExpression AndAlso bindingSymbols.Length = 0 Then
                    ' If we didn't have symbols from the lowest, maybe the bad expression has symbols.
                    bindingSymbols = DirectCast(highestBoundNodeExpr, BoundBadExpression).Symbols
                End If
            End If

            Return bindingSymbols
        End Function

        Private Shared Function GetMeParameter(referenceType As TypeSymbol,
                                               containingType As TypeSymbol,
                                               containingMember As Symbol,
                                               ByRef resultKind As LookupResultKind) As ParameterSymbol

            If containingMember Is Nothing OrElse containingType Is Nothing Then
                ' not in a member of a type (can happen when speculating)
                resultKind = LookupResultKind.NotReferencable
                Return New MeParameterSymbol(containingMember, referenceType)
            End If

            Dim meParam As ParameterSymbol

            Select Case containingMember.Kind
                Case SymbolKind.Method, SymbolKind.Field, SymbolKind.Property
                    If containingMember.IsShared Then
                        ' in a static member
                        resultKind = LookupResultKind.MustNotBeInstance
                        meParam = New MeParameterSymbol(containingMember, containingType)

                    Else
                        If TypeSymbol.Equals(referenceType, ErrorTypeSymbol.UnknownResultType, TypeCompareKind.ConsiderEverything) Then
                            ' in an instance member, but binder considered Me/MyBase/MyClass unreferenceable
                            meParam = New MeParameterSymbol(containingMember, containingType)
                            resultKind = LookupResultKind.NotReferencable
                        Else
                            ' should be good
                            resultKind = LookupResultKind.Good
                            meParam = containingMember.GetMeParameter()
                        End If
                    End If

                Case Else
                    meParam = New MeParameterSymbol(containingMember, referenceType)
                    resultKind = LookupResultKind.NotReferencable
            End Select

            Return meParam
        End Function

        Private Sub GetSemanticSymbolsForLateBoundInvocation(lateInvocation As BoundLateInvocation,
                                                                symbolsBuilder As ArrayBuilder(Of Symbol),
                                                                memberGroupBuilder As ArrayBuilder(Of Symbol),
                                                                ByRef resultKind As LookupResultKind)

            resultKind = LookupResultKind.LateBound
            Dim group = lateInvocation.MethodOrPropertyGroupOpt
            If group IsNot Nothing Then
                group.GetExpressionSymbols(memberGroupBuilder)
                group.GetExpressionSymbols(symbolsBuilder)
            End If
        End Sub

        Private Sub GetSemanticSymbolsForLateBoundMemberAccess(boundNodes As BoundNodeSummary,
                                                               symbolsBuilder As ArrayBuilder(Of Symbol),
                                                               memberGroupBuilder As ArrayBuilder(Of Symbol),
                                                               ByRef resultKind As LookupResultKind)

            If boundNodes.LowestBoundNodeOfSyntacticParent IsNot Nothing AndAlso
                boundNodes.LowestBoundNodeOfSyntacticParent.Kind = BoundKind.LateInvocation Then

                GetSemanticSymbolsForLateBoundInvocation(DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundLateInvocation),
                                                                symbolsBuilder,
                                                                memberGroupBuilder,
                                                                resultKind)

                Return
            End If

            resultKind = LookupResultKind.LateBound
        End Sub

        ' Get the semantic symbols for a BoundMethodGroup. These are somewhat complex, as we want to get the result
        ' of overload resolution even though that result is associated with the parent node.
        Private Sub GetSemanticSymbolsForMethodGroup(boundNodes As BoundNodeSummary,
                    symbolsBuilder As ArrayBuilder(Of Symbol),
                    memberGroupBuilder As ArrayBuilder(Of Symbol),
                    ByRef resultKind As LookupResultKind)

            ' Get the method group.
            Dim methodGroup = DirectCast(boundNodes.LowestBoundNode, BoundMethodGroup)
            resultKind = methodGroup.ResultKind

            methodGroup.GetExpressionSymbols(memberGroupBuilder)

            ' Try to figure out what method this resolved to from the parent node.
            Dim foundResolution As Boolean = False

            'TODO: Will the below work correctly even in the case where M is a parameterless method that returns
            'something with a default parameter (e.g. Item) on it?
            If boundNodes.LowestBoundNodeOfSyntacticParent IsNot Nothing Then
                Select Case boundNodes.LowestBoundNodeOfSyntacticParent.Kind
                    Case BoundKind.Call
                        ' If we are looking for info on M in M(args), we want the symbol that overload resolution chose for M.
                        Dim parentCall = DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundCall)
                        symbolsBuilder.Add(parentCall.Method)
                        If parentCall.ResultKind < resultKind Then
                            resultKind = parentCall.ResultKind
                        End If
                        foundResolution = True

                    Case BoundKind.DelegateCreationExpression
                        ' If we are looking for info on M in AddressOf M, we want the symbol that overload resolution chose for M. This
                        ' should be a BoundDelegateCreation.
                        Dim parentDelegateCreation = DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundDelegateCreationExpression)
                        symbolsBuilder.Add(parentDelegateCreation.Method)
                        If parentDelegateCreation.ResultKind < resultKind Then
                            resultKind = parentDelegateCreation.ResultKind
                        End If
                        foundResolution = True

                    Case BoundKind.BadExpression
                        Dim badExpression = DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundBadExpression)
                        ' If the bad expressions has symbols(s) from the method group, it better
                        ' indicates the problem.
                        symbolsBuilder.AddRange(badExpression.Symbols.Where(Function(sym) memberGroupBuilder.Contains(sym)))
                        If symbolsBuilder.Count > 0 Then
                            resultKind = badExpression.ResultKind
                            foundResolution = True
                        End If

                    Case BoundKind.NameOfOperator
                        symbolsBuilder.AddRange(memberGroupBuilder)
                        resultKind = LookupResultKind.MemberGroup
                        foundResolution = True
                End Select
            End If

            If Not foundResolution Then
                ' If we didn't find a resolution, then use what we had as the member group.
                symbolsBuilder.AddRange(memberGroupBuilder)
                resultKind = LookupResultKind.OverloadResolutionFailure
            End If

            If methodGroup.ResultKind < resultKind Then
                resultKind = methodGroup.ResultKind
            End If
        End Sub

        ' Get the semantic symbols for a BoundPropertyGroup. These are somewhat complex, as we want to get the result
        ' of overload resolution even though that result is associated with the parent node.
        Private Sub GetSemanticSymbolsForPropertyGroup(boundNodes As BoundNodeSummary,
                      symbolsBuilder As ArrayBuilder(Of Symbol),
                      memberGroupBuilder As ArrayBuilder(Of Symbol),
                      ByRef resultKind As LookupResultKind)

            ' Get the property group.
            Dim propertyGroup = DirectCast(boundNodes.LowestBoundNode, BoundPropertyGroup)
            resultKind = propertyGroup.ResultKind
            memberGroupBuilder.AddRange(propertyGroup.Properties)

            ' Try to figure out what property this resolved to from the parent node.
            Dim foundResolution As Boolean = False

            If boundNodes.LowestBoundNodeOfSyntacticParent IsNot Nothing Then
                Select Case boundNodes.LowestBoundNodeOfSyntacticParent.Kind
                    Case BoundKind.PropertyAccess
                        ' If we are looking for info on M in M(args), we want the symbol that overload resolution chose for M.
                        Dim parentPropAccess = TryCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundPropertyAccess)
                        If parentPropAccess IsNot Nothing Then
                            symbolsBuilder.Add(parentPropAccess.PropertySymbol)
                            If parentPropAccess.ResultKind < resultKind Then
                                resultKind = parentPropAccess.ResultKind
                            End If
                            foundResolution = True
                        End If

                    Case BoundKind.BadExpression
                        Dim badExpression = DirectCast(boundNodes.LowestBoundNodeOfSyntacticParent, BoundBadExpression)
                        ' If the bad expressions has symbols(s) from the property group, it better
                        ' indicates the problem.
                        symbolsBuilder.AddRange(badExpression.Symbols.Where(Function(sym) memberGroupBuilder.Contains(sym)))
                        If symbolsBuilder.Count > 0 Then
                            resultKind = badExpression.ResultKind
                            foundResolution = True
                        End If

                    Case BoundKind.NameOfOperator
                        symbolsBuilder.AddRange(memberGroupBuilder)
                        resultKind = LookupResultKind.MemberGroup
                        foundResolution = True
                End Select
            End If

            If Not foundResolution Then
                ' If we didn't find a resolution, then use what we had as the member group. 
                symbolsBuilder.AddRange(memberGroupBuilder)
                resultKind = LookupResultKind.OverloadResolutionFailure
            End If

            If propertyGroup.ResultKind < resultKind Then
                resultKind = propertyGroup.ResultKind
            End If
        End Sub

        Private Shared Function UnwrapAliases(symbols As ImmutableArray(Of Symbol)) As ImmutableArray(Of Symbol)
            Dim anyAliases As Boolean = symbols.Any(Function(sym) sym.Kind = SymbolKind.Alias)

            If Not anyAliases Then
                Return symbols
            End If

            Dim builder As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()
            For Each sym In symbols
                builder.Add(UnwrapAlias(sym))
            Next

            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' In cases where we are binding C in "[C(...)]", the bound nodes return the symbol for the type. However, we've
        ''' decided that we want this case to return the constructor of the type instead (based on the SemanticInfoOptions. This 
        ''' affects only attributes. This method checks for this situation and adjusts the syntax and method group.
        ''' </summary>
        Private Sub AdjustSymbolsForObjectCreation(boundNodes As BoundNodeSummary,
                     binderOpt As Binder,
                     ByRef bindingSymbols As ImmutableArray(Of Symbol),
                     memberGroupBuilder As ArrayBuilder(Of Symbol),
                     ByRef resultKind As LookupResultKind)
            Dim constructor As MethodSymbol = Nothing
            Dim lowestBoundNode = boundNodes.LowestBoundNode
            Dim boundNodeOfSyntacticParent = boundNodes.LowestBoundNodeOfSyntacticParent

            Debug.Assert(boundNodeOfSyntacticParent IsNot Nothing)

            ' Check if boundNode.Syntax is the type-name child of an ObjectCreationExpression or Attribute.
            Dim parentSyntax As SyntaxNode = boundNodeOfSyntacticParent.Syntax
            If parentSyntax IsNot Nothing AndAlso
               lowestBoundNode IsNot Nothing AndAlso
               parentSyntax Is lowestBoundNode.Syntax.Parent AndAlso
               parentSyntax.Kind = SyntaxKind.Attribute AndAlso
               (DirectCast(parentSyntax, AttributeSyntax).Name Is lowestBoundNode.Syntax) Then

                Dim unwrappedSymbols = UnwrapAliases(bindingSymbols)

                ' We must have bound to a single named type 
                If unwrappedSymbols.Length = 1 AndAlso TypeOf unwrappedSymbols(0) Is TypeSymbol Then
                    Dim typeSymbol As TypeSymbol = DirectCast(unwrappedSymbols(0), TypeSymbol)
                    Dim namedTypeSymbol As NamedTypeSymbol = TryCast(typeSymbol, NamedTypeSymbol)

                    ' Figure out which constructor was selected.
                    Select Case boundNodeOfSyntacticParent.Kind
                        Case BoundKind.Attribute
                            Dim boundAttribute As BoundAttribute = DirectCast(boundNodeOfSyntacticParent, BoundAttribute)

                            Debug.Assert(resultKind <> LookupResultKind.Good OrElse TypeSymbol.Equals(namedTypeSymbol, boundAttribute.Type, TypeCompareKind.ConsiderEverything))
                            constructor = boundAttribute.Constructor
                            resultKind = LookupResult.WorseResultKind(resultKind, boundAttribute.ResultKind)

                        Case BoundKind.BadExpression
                            ' Note that namedTypeSymbol might be null here; e.g., a type parameter.
                            Dim boundBadExpression As BoundBadExpression = DirectCast(boundNodeOfSyntacticParent, BoundBadExpression)
                            resultKind = LookupResult.WorseResultKind(resultKind, boundBadExpression.ResultKind)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(boundNodeOfSyntacticParent.Kind)
                    End Select

                    AdjustSymbolsForObjectCreation(lowestBoundNode, namedTypeSymbol, constructor, binderOpt, bindingSymbols, memberGroupBuilder, resultKind)
                End If
            End If
        End Sub

        Private Sub AdjustSymbolsForObjectCreation(
            lowestBoundNode As BoundNode,
            namedTypeSymbol As NamedTypeSymbol,
            constructor As MethodSymbol,
            binderOpt As Binder,
            ByRef bindingSymbols As ImmutableArray(Of Symbol),
            memberGroupBuilder As ArrayBuilder(Of Symbol),
            ByRef resultKind As LookupResultKind)

            Debug.Assert(memberGroupBuilder IsNot Nothing)
            Debug.Assert(lowestBoundNode IsNot Nothing)
            Debug.Assert(binderOpt IsNot Nothing OrElse IsInTree(lowestBoundNode.Syntax))

            If namedTypeSymbol IsNot Nothing Then
                Debug.Assert(lowestBoundNode.Syntax IsNot Nothing)

                ' Filter namedTypeSymbol's instance constructors by accessibility.
                ' If all the instance constructors are inaccessible, we retain
                ' all the instance constructors.
                Dim binder As Binder = If(binderOpt, GetEnclosingBinder(lowestBoundNode.Syntax.SpanStart))
                Dim candidateConstructors As ImmutableArray(Of MethodSymbol)

                If binder IsNot Nothing Then
                    Dim interfaceCoClass As NamedTypeSymbol = If(namedTypeSymbol.IsInterface,
                                                                 TryCast(namedTypeSymbol.CoClassType, NamedTypeSymbol), Nothing)
                    candidateConstructors = binder.GetAccessibleConstructors(If(interfaceCoClass, namedTypeSymbol), useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)

                    Dim instanceConstructors = namedTypeSymbol.InstanceConstructors
                    If Not candidateConstructors.Any() AndAlso instanceConstructors.Any() Then
                        Debug.Assert(resultKind <> LookupResultKind.Good)
                        candidateConstructors = instanceConstructors
                    End If
                Else
                    candidateConstructors = ImmutableArray(Of MethodSymbol).Empty
                End If

                If constructor IsNot Nothing Then
                    Debug.Assert(candidateConstructors.Contains(constructor))
                    bindingSymbols = ImmutableArray.Create(Of Symbol)(constructor)
                ElseIf candidateConstructors.Length <> 0 Then
                    bindingSymbols = StaticCast(Of Symbol).From(candidateConstructors)
                    resultKind = LookupResult.WorseResultKind(resultKind, LookupResultKind.OverloadResolutionFailure)
                End If

                memberGroupBuilder.AddRange(candidateConstructors)
            End If
        End Sub

        ' Gets SymbolInfo for a type or namespace or alias reference or implemented method.
        Friend Function GetSymbolInfoForSymbol(
            symbol As Symbol,
            options As SymbolInfoOptions
        ) As SymbolInfo

            ' 1. Determine type, dig through alias if needed.
            Dim type = TryCast(UnwrapAlias(symbol), TypeSymbol)

            ' 2. Determine symbols.
            ' We never return error symbols in the SemanticInfo.
            ' Error types carry along other symbols and result kinds in error cases.

            ' Getting the set of symbols is a bit involved. We use the union of the symbol with 
            ' any symbols from the diagnostics, but error symbols are not included.\
            Dim resultKind As LookupResultKind
            Dim symbolsBuilder = ArrayBuilder(Of Symbol).GetInstance()
            Dim originalErrorSymbol = If(type IsNot Nothing, TryCast(type.OriginalDefinition, ErrorTypeSymbol), Nothing)
            If originalErrorSymbol IsNot Nothing Then
                ' Error case.
                resultKind = originalErrorSymbol.ResultKind
                If resultKind <> LookupResultKind.Empty Then
                    symbolsBuilder.AddRange(originalErrorSymbol.CandidateSymbols)
                End If

            ElseIf symbol.Kind = SymbolKind.Namespace AndAlso DirectCast(symbol, NamespaceSymbol).NamespaceKind = NamespaceKindNamespaceGroup Then
                symbolsBuilder.AddRange(DirectCast(symbol, NamespaceSymbol).ConstituentNamespaces)
                resultKind = LookupResultKind.Ambiguous
            Else
                symbolsBuilder.Add(symbol)
                resultKind = LookupResultKind.Good
            End If

            Dim symbols As ImmutableArray(Of Symbol) = RemoveErrorTypesAndDuplicates(symbolsBuilder, options)
            symbolsBuilder.Free()

            Return SymbolInfoFactory.Create(symbols, resultKind)
        End Function

        ' Gets TypeInfo for a type or namespace or alias reference or implemented method.
        Friend Function GetTypeInfoForSymbol(
            symbol As Symbol
        ) As VisualBasicTypeInfo

            ' 1. Determine type, dig through alias if needed.
            Dim type = TryCast(UnwrapAlias(symbol), TypeSymbol)

            Return New VisualBasicTypeInfo(type, type, New Conversion(Conversions.Identity))
        End Function

        ' This is used by other binding API's to invoke the right binder API
        Friend Overridable Function Bind(binder As Binder, node As SyntaxNode, diagnostics As BindingDiagnosticBag) As BoundNode
            Dim expr = TryCast(node, ExpressionSyntax)
            If expr IsNot Nothing Then
                Return binder.BindNamespaceOrTypeOrExpressionSyntaxForSemanticModel(expr, diagnostics)
            Else
                Dim statement = TryCast(node, StatementSyntax)
                If statement IsNot Nothing Then
                    Return binder.BindStatement(statement, diagnostics)
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the available named symbols in the context of the specified location And optional container. Only
        ''' symbols that are accessible And visible from the given location are returned.
        ''' </summary>
        ''' <param name="position">The character position for determining the enclosing declaration scope And
        ''' accessibility.</param>
        ''' <param name="container">The container to search for symbols within. If null then the enclosing declaration
        ''' scope around position Is used.</param>
        ''' <param name="name">The name of the symbol to find. If null Is specified then symbols
        ''' with any names are returned.</param>
        ''' <param name="includeReducedExtensionMethods">Consider (reduced) extension methods.</param>
        ''' <returns>A list of symbols that were found. If no symbols were found, an empty list Is returned.</returns>
        ''' <remarks>
        ''' The "position" Is used to determine what variables are visible And accessible. Even if "container" Is
        ''' specified, the "position" location Is significant for determining which members of "containing" are
        ''' accessible. 
        ''' 
        ''' Labels are Not considered (see <see cref="LookupLabels"/>).
        ''' 
        ''' Non-reduced extension methods are considered regardless of the value of <paramref name="includeReducedExtensionMethods"/>.
        ''' </remarks>
        Public Shadows Function LookupSymbols(
            position As Integer,
            Optional container As INamespaceOrTypeSymbol = Nothing,
            Optional name As String = Nothing,
            Optional includeReducedExtensionMethods As Boolean = False
        ) As ImmutableArray(Of ISymbol)
            Dim options = If(includeReducedExtensionMethods, LookupOptions.Default, LookupOptions.IgnoreExtensionMethods)

            Dim result = LookupSymbolsInternal(position, ToLanguageSpecific(container), name, options, useBaseReferenceAccessibility:=False)

#If DEBUG Then
            For Each item In result
                Debug.Assert(item.Kind <> SymbolKind.Namespace OrElse DirectCast(item, NamespaceSymbol).NamespaceKind <> NamespaceKindNamespaceGroup)
            Next
#End If

            Return StaticCast(Of ISymbol).From(result)
        End Function

        ''' <summary>
        ''' Gets the available base type members in the context of the specified location.  Akin to
        ''' calling <see cref="LookupSymbols"/> with the container set to the immediate base type of
        ''' the type in which <paramref name="position"/> occurs.  However, the accessibility rules
        ''' are different: protected members of the base type will be visible.
        ''' 
        ''' Consider the following example:
        ''' 
        '''   Public Class Base
        '''       Protected Sub M()
        '''       End Sub
        '''   End Class
        '''   
        '''   Public Class Derived : Inherits Base
        '''       Sub Test(b as Base)
        '''           b.M() ' Error - cannot access protected member.
        '''           MyBase.M()
        '''       End Sub
        '''   End Class
        ''' 
        ''' Protected members of an instance of another type are only accessible if the instance is known
        ''' to be "this" instance (as indicated by the "MyBase" keyword).
        ''' </summary>
        ''' <param name="position">The character position for determining the enclosing declaration scope and
        ''' accessibility.</param>
        ''' <param name="name">The name of the symbol to find. If null is specified then symbols
        ''' with any names are returned.</param>
        ''' <returns>A list of symbols that were found. If no symbols were found, an empty list is returned.</returns>
        ''' <remarks>
        ''' The "position" is used to determine what variables are visible and accessible.
        ''' 
        ''' Non-reduced extension methods are considered, but reduced extension methods are not.
        ''' </remarks>
        Public Shadows Function LookupBaseMembers(
            position As Integer,
            Optional name As String = Nothing
        ) As ImmutableArray(Of ISymbol)
            Dim result = LookupSymbolsInternal(position, Nothing, name, LookupOptions.Default, useBaseReferenceAccessibility:=True)
#If DEBUG Then
            For Each item In result
                Debug.Assert(item.Kind <> SymbolKind.Namespace)
            Next
#End If

            Return StaticCast(Of ISymbol).From(result)
        End Function

        ''' <summary>
        ''' Gets the available named static member symbols in the context of the specified location And optional container.
        ''' Only members that are accessible And visible from the given location are returned.
        ''' 
        ''' Non-reduced extension methods are considered, since they are static methods.
        ''' </summary>
        ''' <param name="position">The character position for determining the enclosing declaration scope And
        ''' accessibility.</param>
        ''' <param name="container">The container to search for symbols within. If null then the enclosing declaration
        ''' scope around position Is used.</param>
        ''' <param name="name">The name of the symbol to find. If null Is specified then symbols
        ''' with any names are returned.</param>
        ''' <returns>A list of symbols that were found. If no symbols were found, an empty list Is returned.</returns>
        ''' <remarks>
        ''' The "position" Is used to determine what variables are visible And accessible. Even if "container" Is
        ''' specified, the "position" location Is significant for determining which members of "containing" are
        ''' accessible. 
        ''' </remarks>
        Public Shadows Function LookupStaticMembers(
            position As Integer,
            Optional container As INamespaceOrTypeSymbol = Nothing,
            Optional name As String = Nothing
        ) As ImmutableArray(Of ISymbol)
            Dim result = LookupSymbolsInternal(position, ToLanguageSpecific(container), name, LookupOptions.MustNotBeInstance Or LookupOptions.IgnoreExtensionMethods, useBaseReferenceAccessibility:=False)
#If DEBUG Then
            For Each item In result
                Debug.Assert(item.Kind <> SymbolKind.Namespace OrElse DirectCast(item, NamespaceSymbol).NamespaceKind <> NamespaceKindNamespaceGroup)
            Next
#End If
            Return StaticCast(Of ISymbol).From(result)
        End Function

        ''' <summary>
        ''' Gets the available named namespace And type symbols in the context of the specified location And optional container.
        ''' Only members that are accessible And visible from the given location are returned.
        ''' </summary>
        ''' <param name="position">The character position for determining the enclosing declaration scope And
        ''' accessibility.</param>
        ''' <param name="container">The container to search for symbols within. If null then the enclosing declaration
        ''' scope around position Is used.</param>
        ''' <param name="name">The name of the symbol to find. If null Is specified then symbols
        ''' with any names are returned.</param>
        ''' <returns>A list of symbols that were found. If no symbols were found, an empty list Is returned.</returns>
        ''' <remarks>
        ''' The "position" Is used to determine what variables are visible And accessible. Even if "container" Is
        ''' specified, the "position" location Is significant for determining which members of "containing" are
        ''' accessible. 
        ''' 
        ''' Does Not return INamespaceOrTypeSymbol, because there could be aliases.
        ''' </remarks>
        Public Shadows Function LookupNamespacesAndTypes(
            position As Integer,
            Optional container As INamespaceOrTypeSymbol = Nothing,
            Optional name As String = Nothing
        ) As ImmutableArray(Of ISymbol)
            Dim result = LookupSymbolsInternal(position, ToLanguageSpecific(container), name, LookupOptions.NamespacesOrTypesOnly, useBaseReferenceAccessibility:=False)

#If DEBUG Then
            For Each item In result
                Debug.Assert(item.Kind <> SymbolKind.Namespace OrElse DirectCast(item, NamespaceSymbol).NamespaceKind <> NamespaceKindNamespaceGroup)
            Next
#End If
            Return StaticCast(Of ISymbol).From(result)
        End Function

        ''' <summary>
        ''' Gets the available named label symbols in the context of the specified location And optional container.
        ''' Only members that are accessible And visible from the given location are returned.
        ''' </summary>
        ''' <param name="position">The character position for determining the enclosing declaration scope And
        ''' accessibility.</param>
        ''' <param name="name">The name of the symbol to find. If null Is specified then symbols
        ''' with any names are returned.</param>
        ''' <returns>A list of symbols that were found. If no symbols were found, an empty list Is returned.</returns>
        ''' <remarks>
        ''' The "position" Is used to determine what variables are visible And accessible. Even if "container" Is
        ''' specified, the "position" location Is significant for determining which members of "containing" are
        ''' accessible. 
        ''' </remarks>
        Public Shadows Function LookupLabels(
            position As Integer,
            Optional name As String = Nothing
        ) As ImmutableArray(Of ISymbol)
            Dim result = LookupSymbolsInternal(position, container:=Nothing, name:=name, options:=LookupOptions.LabelsOnly, useBaseReferenceAccessibility:=False)
#If DEBUG Then
            For Each item In result
                Debug.Assert(item.Kind <> SymbolKind.Namespace)
            Next
#End If
            Return StaticCast(Of ISymbol).From(result)
        End Function

        ''' <summary>
        ''' Gets the available named symbols in the context of the specified location and optional
        ''' container. Only symbols that are accessible and visible from the given location are
        ''' returned.
        ''' </summary>
        ''' <param name="position">The character position for determining the enclosing declaration
        ''' scope and accessibility.</param>
        ''' <param name="container">The container to search for symbols within. If null then the
        ''' enclosing declaration scope around position is used.</param>
        ''' <param name="name">The name of the symbol to find. If null is specified then symbols
        ''' with any names are returned.</param>
        ''' <param name="options">Additional options that affect the lookup process.</param>
        ''' <param name="useBaseReferenceAccessibility">Ignore 'throughType' in accessibility checking. 
        ''' Used in checking accessibility of symbols accessed via 'MyBase' or 'base'.</param>
        ''' <returns>A list of symbols that were found. If no symbols were found, an empty list is
        ''' returned.</returns>
        ''' <remarks>
        ''' The "position" is used to determine what variables are visible and accessible. Even if
        ''' "container" is specified, the "position" location is significant for determining which
        ''' members of "containing" are accessible. 
        ''' 
        ''' Locations are character locations, just as used as the Syntax APIs such as FindToken, or 
        ''' returned from the Span property on tokens and syntax node.
        ''' 
        ''' The text of the program is divided into scopes, which nest but don't otherwise
        ''' intersect. When doing an operation such as LookupSymbols, the code first determines the
        ''' smallest scope containing the position, and from there all containing scopes. 
        ''' 
        ''' Scopes that span an entire block statement start at the beginning of the first token of 
        ''' the block header, and end immediately before the statement terminator token following
        ''' the end statement of the block. If the end statement of the block is missing, it ends
        ''' immediately before the next token. Examples of these include members and type parameters
        ''' of a type, type parameters of a method, and variables declared in a For statement.
        ''' 
        ''' Scopes that span the interior of a block statement start at the statement terminator of 
        ''' the block header statement, and end immediately before the first token of the end
        ''' statement of the block. If the end statement of the block is missing, it ends
        ''' immediately before the next statement. Examples of these include local variables, method
        ''' parameters, and members of a namespace.
        ''' 
        ''' Scopes of variables declared in a single-line If statement start at the beginning of the
        ''' "Then" token, and end immediately before the Else token or statement terminator. 
        ''' 
        ''' Scopes of variables declared in the Else part of a single-line If start at the beginning
        ''' of the "Else" token, and end immediately before the statement terminator.
        ''' 
        ''' Some specialized binding rules are in place for a single statement, like Imports or
        ''' Inherits. These specialized binding rules begin at the start of the first token of the
        ''' statement, and end immediately before the statement terminator of that statement.
        ''' 
        ''' In all of the above, the "start" means the start of a token without considering leading
        ''' trivia. In other words, Span.Start, not FullSpan.Start. With the exception of
        ''' documentation comments, all scopes begin at the start of a token, and end immediately
        ''' before the start of a token.
        ''' 
        ''' The scope of the default namespace, and all symbols introduced via Imports statements,
        ''' is the entire file.
        ''' 
        ''' Positions within a documentation comment that is correctly attached to a symbol take on
        ''' the binding scope of that symbol. 
        ''' </remarks>
        ''' <exception cref="ArgumentException">Throws an argument exception if the passed lookup options are invalid.</exception>
        Private Function LookupSymbolsInternal(position As Integer,
                 container As NamespaceOrTypeSymbol,
                 name As String,
                 options As LookupOptions,
                 useBaseReferenceAccessibility As Boolean) As ImmutableArray(Of Symbol)

            Debug.Assert((options And LookupOptions.UseBaseReferenceAccessibility) = 0, "Use the useBaseReferenceAccessibility parameter.")
            If useBaseReferenceAccessibility Then
                options = options Or LookupOptions.UseBaseReferenceAccessibility
            End If
            Debug.Assert(options.IsValid())

            CheckPosition(position)

            Dim binder = Me.GetEnclosingBinder(position)
            If binder Is Nothing Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            If useBaseReferenceAccessibility Then
                Debug.Assert(container Is Nothing)
                Dim containingType = binder.ContainingType
                Dim baseType = If(containingType Is Nothing, Nothing, containingType.BaseTypeNoUseSiteDiagnostics)
                If baseType Is Nothing Then
                    Throw New ArgumentException(NameOf(position),
                            "Not a valid position for a call to LookupBaseMembers (must be in a type with a base type)")
                End If
                container = baseType
            End If

            If name Is Nothing Then
                ' If they didn't provide a name, then look up all names and associated arities 
                ' and find all the corresponding symbols.
                Dim info = LookupSymbolsInfo.GetInstance()
                Me.AddLookupSymbolsInfo(position, info, container, options)

                Dim results = ArrayBuilder(Of Symbol).GetInstance(info.Count)

                For Each foundName In info.Names
                    AppendSymbolsWithName(results, foundName, binder, container, options, info)
                Next

                info.Free()

                Dim sealedResults = results.ToImmutableAndFree()

                Dim builder As ArrayBuilder(Of Symbol) = Nothing
                Dim pos = 0
                For Each result In sealedResults
                    ' Special case: we want to see constructors, even though they can't be referenced by name.
                    If result.CanBeReferencedByName OrElse
                        (result.Kind = SymbolKind.Method AndAlso DirectCast(result, MethodSymbol).MethodKind = MethodKind.Constructor) Then
                        If builder IsNot Nothing Then
                            builder.Add(result)
                        End If
                    ElseIf builder Is Nothing Then
                        builder = ArrayBuilder(Of Symbol).GetInstance()
                        builder.AddRange(sealedResults, pos)
                    End If

                    pos = pos + 1
                Next

                Return If(builder Is Nothing, sealedResults, builder.ToImmutableAndFree())
            Else
                ' They provided a name.  Find all the arities for that name, and then look all of those up.
                Dim info = LookupSymbolsInfo.GetInstance()
                info.FilterName = name

                Me.AddLookupSymbolsInfo(position, info, container, options)

                Dim results = ArrayBuilder(Of Symbol).GetInstance(info.Count)

                AppendSymbolsWithName(results, name, binder, container, options, info)

                info.Free()

                ' If the name was specified, we don't have to do additional filtering - this is what they asked for.
                Return results.ToImmutableAndFree()
            End If
        End Function

        Private Sub AppendSymbolsWithName(results As ArrayBuilder(Of Symbol), name As String, binder As Binder, container As NamespaceOrTypeSymbol, options As LookupOptions, info As LookupSymbolsInfo)
            Dim arities As LookupSymbolsInfo.IArityEnumerable = Nothing
            Dim uniqueSymbol As Symbol = Nothing

            If info.TryGetAritiesAndUniqueSymbol(name, arities, uniqueSymbol) Then
                If uniqueSymbol IsNot Nothing Then
                    ' This name mapped to something unique.  We don't need to proceed
                    ' with a costly lookup.  Just add it straight to the results.
                    results.Add(uniqueSymbol)
                Else
                    ' The name maps to multiple symbols. Actually do a real lookup so 
                    ' that we will properly figure out hiding and whatnot.
                    If arities IsNot Nothing Then
                        Me.LookupSymbols(binder, container, name, arities, options, results)
                    Else
                        ' If there's no unique symbol, then there won't have been a non-zero arity
                        Me.LookupSymbols(binder, container, name, 0, options, results)
                    End If
                End If
            End If
        End Sub

        ' Lookup symbol using a given binding. Options has already had the ByLocation and AllNames
        ' flags taken off appropriately.
        Private Shadows Sub LookupSymbols(binder As Binder,
                                  container As NamespaceOrTypeSymbol,
                                  name As String,
                                  arities As LookupSymbolsInfo.IArityEnumerable,
                                  options As LookupOptions,
                                  results As ArrayBuilder(Of Symbol))
            Debug.Assert(results IsNot Nothing)

            Dim uniqueSymbols = PooledHashSet(Of Symbol).GetInstance()
            Dim tempResults = ArrayBuilder(Of Symbol).GetInstance(arities.Count)

            For Each knownArity In arities
                ' TODO: What happens here if options has LookupOptions.AllMethodsOfAnyArity bit set?
                '       It looks like we will be dealing with a lot of duplicate methods. Should we optimize this
                '       by clearing the bit?
                Me.LookupSymbols(binder, container, name, knownArity, options, tempResults)
                uniqueSymbols.UnionWith(tempResults)

                tempResults.Clear()
            Next
            tempResults.Free()

            results.AddRange(uniqueSymbols)
            uniqueSymbols.Free()
        End Sub

        Private Shadows Sub LookupSymbols(binder As Binder,
                                  container As NamespaceOrTypeSymbol,
                                  name As String,
                                  arity As Integer,
                                  options As LookupOptions,
                                  results As ArrayBuilder(Of Symbol))
            If name = WellKnownMemberNames.InstanceConstructorName Then  ' intentionally case sensitive; constructors always exactly ".ctor".
                ' Constructors have very different lookup rules.
                LookupInstanceConstructors(binder, container, options, results)

                Return
            End If

            Dim result = LookupResult.GetInstance()
            Dim realArity = arity

            options = CType(options Or LookupOptions.EagerlyLookupExtensionMethods, LookupOptions)

            If options.IsAttributeTypeLookup Then
                binder.LookupAttributeType(result, container, name, options, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
            ElseIf container Is Nothing Then
                binder.Lookup(result, name, realArity, options, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
            Else
                binder.LookupMember(result, container, name, realArity, options, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
            End If

            If result.IsGoodOrAmbiguous Then
                If result.HasDiagnostic Then
                    ' In the ambiguous symbol case, we have a good symbol with a diagnostics that
                    ' mentions the other symbols. Union everything together with a set to prevent dups.
                    Dim symbolSet = PooledHashSet(Of Symbol).GetInstance()
                    Dim symBuilder = ArrayBuilder(Of Symbol).GetInstance()
                    AddSymbolsFromDiagnosticInfo(symBuilder, result.Diagnostic)
                    symbolSet.UnionWith(symBuilder)
                    symbolSet.UnionWith(result.Symbols)
                    symBuilder.Free()

                    results.AddRange(symbolSet)
                    symbolSet.Free()
                ElseIf result.HasSingleSymbol AndAlso result.SingleSymbol.Kind = SymbolKind.Namespace AndAlso
                       DirectCast(result.SingleSymbol, NamespaceSymbol).NamespaceKind = NamespaceKindNamespaceGroup Then
                    results.AddRange(DirectCast(result.SingleSymbol, NamespaceSymbol).ConstituentNamespaces)
                Else
                    results.AddRange(result.Symbols)
                End If
            End If
            result.Free()
        End Sub

        ' Do a lookup of instance constructors, taking LookupOptions into account.
        Private Sub LookupInstanceConstructors(
            binder As Binder,
            container As NamespaceOrTypeSymbol,
            options As LookupOptions,
            results As ArrayBuilder(Of Symbol)
        )
            Debug.Assert(results IsNot Nothing)

            Dim constructors As ImmutableArray(Of MethodSymbol) = ImmutableArray(Of MethodSymbol).Empty
            Dim type As NamedTypeSymbol = TryCast(container, NamedTypeSymbol)

            If type IsNot Nothing AndAlso
                (options And (LookupOptions.LabelsOnly Or LookupOptions.NamespacesOrTypesOnly Or LookupOptions.MustNotBeInstance)) = 0 Then
                If (options And LookupOptions.IgnoreAccessibility) <> 0 Then
                    constructors = type.InstanceConstructors
                Else
                    constructors = binder.GetAccessibleConstructors(type, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
                End If
            End If

            results.AddRange(constructors)
        End Sub

        ''' <summary>
        ''' Gets the names of the available named symbols in the context of the specified location
        ''' and optional container. Only symbols that are accessible and visible from the given
        ''' location are returned.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and
        ''' accessibility. This character position must be within the FullSpan of the Root syntax
        ''' node in this SemanticModel.
        ''' </param>
        ''' <param name="container">The container to search for symbols within. If null then the
        ''' enclosing declaration scope around position is used.</param>
        ''' <param name="options">Additional options that affect the lookup process.</param>
        ''' <remarks>
        ''' The "position" is used to determine what variables are visible and accessible. Even if
        ''' "container" is specified, the "position" location is significant for determining which
        ''' members of "containing" are accessible.
        ''' </remarks>
        Private Sub AddLookupSymbolsInfo(position As Integer,
                                        info As LookupSymbolsInfo,
                                        Optional container As NamespaceOrTypeSymbol = Nothing,
                                        Optional options As LookupOptions = LookupOptions.Default)
            CheckPosition(position)

            Dim binder = Me.GetEnclosingBinder(position)

            If binder IsNot Nothing Then
                If container Is Nothing Then
                    binder.AddLookupSymbolsInfo(info, options)
                Else
                    binder.AddMemberLookupSymbolsInfo(info, container, options)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Determines if the symbol is accessible from the specified location.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and
        ''' accessibility. This character position must be within the FullSpan of the Root syntax
        ''' node in this SemanticModel.
        ''' </param>
        ''' <param name="symbol">The symbol that we are checking to see if it accessible.</param>
        ''' <returns>
        ''' True if "symbol is accessible, false otherwise.</returns>
        ''' <remarks>
        ''' This method only checks accessibility from the point of view of the accessibility
        ''' modifiers on symbol and its containing types. Even if true is returned, the given symbol
        ''' may not be able to be referenced for other reasons, such as name hiding.
        ''' </remarks>
        Public Shadows Function IsAccessible(position As Integer, symbol As ISymbol) As Boolean
            CheckPosition(position)

            If symbol Is Nothing Then
                Throw New ArgumentNullException(NameOf(symbol))
            End If

            Dim vbsymbol = symbol.EnsureVbSymbolOrNothing(Of Symbol)(NameOf(symbol))

            Dim binder = Me.GetEnclosingBinder(position)
            If binder IsNot Nothing Then
                Return binder.IsAccessible(vbsymbol, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
            End If

            Return False
        End Function

        ''' <summary>
        ''' Analyze control-flow within a part of a method body.
        ''' </summary>
        ''' <param name="firstStatement">The first statement to be included in the analysis.</param>
        ''' <param name="lastStatement">The last statement to be included in the analysis.</param>
        ''' <returns>An object that can be used to obtain the result of the control flow analysis.</returns>
        ''' <exception cref="ArgumentException">The two statements are not contained within the same statement list.</exception>
        Public Overridable Shadows Function AnalyzeControlFlow(firstStatement As StatementSyntax, lastStatement As StatementSyntax) As ControlFlowAnalysis
            Throw New NotSupportedException()
        End Function

        ''' <summary>
        ''' Analyze control-flow within a part of a method body.
        ''' </summary>
        ''' <param name="statement">The statement to be included in the analysis.</param>
        ''' <returns>An object that can be used to obtain the result of the control flow analysis.</returns>
        Public Overridable Shadows Function AnalyzeControlFlow(statement As StatementSyntax) As ControlFlowAnalysis
            Return AnalyzeControlFlow(statement, statement)
        End Function

        ''' <summary>
        ''' Analyze data-flow within an expression. 
        ''' </summary>
        ''' <param name="expression">The expression within the associated SyntaxTree to analyze.</param>
        ''' <returns>An object that can be used to obtain the result of the data flow analysis.</returns>
        Public Overridable Shadows Function AnalyzeDataFlow(expression As ExpressionSyntax) As DataFlowAnalysis
            Throw New NotSupportedException()
        End Function

        ''' <summary>
        ''' Analyze data-flow within a set of contiguous statements.
        ''' </summary>
        ''' <param name="firstStatement">The first statement to be included in the analysis.</param>
        ''' <param name="lastStatement">The last statement to be included in the analysis.</param>
        ''' <returns>An object that can be used to obtain the result of the data flow analysis.</returns>
        ''' <exception cref="ArgumentException">The two statements are not contained within the same statement list.</exception>
        Public Overridable Shadows Function AnalyzeDataFlow(firstStatement As StatementSyntax, lastStatement As StatementSyntax) As DataFlowAnalysis
            Throw New NotSupportedException()
        End Function

        ''' <summary>
        ''' Analyze data-flow within a statement.
        ''' </summary>
        ''' <param name="statement">The statement to be included in the analysis.</param>
        ''' <returns>An object that can be used to obtain the result of the data flow analysis.</returns>
        Public Overridable Shadows Function AnalyzeDataFlow(statement As StatementSyntax) As DataFlowAnalysis
            Return AnalyzeDataFlow(statement, statement)
        End Function

        ''' <summary>
        ''' Get a SemanticModel object that is associated with a method body that did not appear in this source code.
        ''' Given <paramref name="position"/> must lie within an existing method body of the Root syntax node for this SemanticModel.
        ''' Locals and labels declared within this existing method body are not considered to be in scope of the speculated method body.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and accessibility. This
        ''' character position must be within the FullSpan of the Root syntax node in this SemanticModel and must be
        ''' within the FullSpan of a Method body within the Root syntax node.</param>
        ''' <param name="method">A syntax node that represents a parsed method declaration. This method should not be
        ''' present in the syntax tree associated with this object, but must have identical signature to the method containing
        ''' the given <paramref name="position"/> in this SemanticModel.</param>
        ''' <param name="speculativeModel">A SemanticModel object that can be used to inquire about the semantic
        ''' information associated with syntax nodes within <paramref name="method"/>.</param>
        ''' <returns>Flag indicating whether a speculative semantic model was created.</returns>
        ''' <exception cref="ArgumentException">Throws this exception if the <paramref name="method"/> node is contained any SyntaxTree in the current Compilation.</exception>
        ''' <exception cref="ArgumentNullException">Throws this exception if <paramref name="method"/> is null.</exception>
        ''' <exception cref="InvalidOperationException">Throws this exception if this model is a speculative semantic model, i.e. <see cref="IsSpeculativeSemanticModel"/> is True.
        ''' Chaining of speculative semantic model is not supported.</exception>
        Public Function TryGetSpeculativeSemanticModelForMethodBody(position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            CheckPosition(position)
            CheckModelAndSyntaxNodeToSpeculate(method)

            Dim speculativePublicModel As PublicSemanticModel = Nothing
            Dim result = TryGetSpeculativeSemanticModelForMethodBodyCore(DirectCast(Me, SyntaxTreeSemanticModel), position, method, speculativePublicModel)
            speculativeModel = speculativePublicModel

            Return result
        End Function

        Friend MustOverride Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean

        ''' <summary>
        ''' Get a SemanticModel object that is associated with a range argument syntax that did not appear in
        ''' this source code. This can be used to get detailed semantic information about sub-parts
        ''' of this node that did not appear in source code. 
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and accessibility. This
        ''' character position must be within the FullSpan of the Root syntax node in this SemanticModel.
        ''' </param>
        ''' <param name="rangeArgument">A syntax node that represents a parsed RangeArgumentSyntax node. This node should not be
        ''' present in the syntax tree associated with this object.</param>
        ''' <param name="speculativeModel">A SemanticModel object that can be used to inquire about the semantic
        ''' information associated with syntax nodes within <paramref name="rangeArgument"/>.</param>
        ''' <returns>Flag indicating whether a speculative semantic model was created.</returns>
        ''' <exception cref="ArgumentException">Throws this exception if the <paramref name="rangeArgument"/> node is contained any SyntaxTree in the current Compilation.</exception>
        ''' <exception cref="ArgumentNullException">Throws this exception if <paramref name="rangeArgument"/> is null.</exception>
        ''' <exception cref="InvalidOperationException">Throws this exception if this model is a speculative semantic model, i.e. <see cref="IsSpeculativeSemanticModel"/> is True.
        ''' Chaining of speculative semantic model is not supported.</exception>
        Public Function TryGetSpeculativeSemanticModel(position As Integer, rangeArgument As RangeArgumentSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            CheckPosition(position)
            CheckModelAndSyntaxNodeToSpeculate(rangeArgument)

            Dim speculativePublicModel As PublicSemanticModel = Nothing
            Dim result = TryGetSpeculativeSemanticModelCore(DirectCast(Me, SyntaxTreeSemanticModel), position, rangeArgument, speculativePublicModel)
            speculativeModel = speculativePublicModel

            Return result
        End Function

        Friend MustOverride Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, rangeArgument As RangeArgumentSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean

        ''' <summary>
        ''' Get a SemanticModel object that is associated with an executable statement that did not appear in
        ''' this source code. This can be used to get detailed semantic information about sub-parts
        ''' of a statement that did not appear in source code. 
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and accessibility. This
        ''' character position must be within the FullSpan of the Root syntax node in this SemanticModel.</param>
        ''' <param name="statement">A syntax node that represents a parsed statement. This statement should not be
        ''' present in the syntax tree associated with this object.</param>
        ''' <param name="speculativeModel">A SemanticModel object that can be used to inquire about the semantic
        ''' information associated with syntax nodes within <paramref name="statement"/>.</param>
        ''' <returns>Flag indicating whether a speculative semantic model was created.</returns>
        ''' <exception cref="ArgumentException">Throws this exception if the <paramref name="statement"/> node is contained any SyntaxTree in the current Compilation.</exception>
        ''' <exception cref="ArgumentNullException">Throws this exception if <paramref name="statement"/> is null.</exception>
        ''' <exception cref="InvalidOperationException">Throws this exception if this model is a speculative semantic model, i.e. <see cref="IsSpeculativeSemanticModel"/> is True.
        ''' Chaining of speculative semantic model is not supported.</exception>
        Public Function TryGetSpeculativeSemanticModel(position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            CheckPosition(position)
            CheckModelAndSyntaxNodeToSpeculate(statement)

            Dim speculativePublicModel As PublicSemanticModel = Nothing
            Dim result = TryGetSpeculativeSemanticModelCore(DirectCast(Me, SyntaxTreeSemanticModel), position, statement, speculativePublicModel)
            speculativeModel = speculativePublicModel

            Return result
        End Function

        Friend MustOverride Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean

        ''' <summary>
        ''' Get a SemanticModel object that is associated with an initializer that did not appear in
        ''' this source code. This can be used to get detailed semantic information about sub-parts
        ''' of a field initializer, property initializer or default parameter value that did not appear in source code.
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and accessibility. This
        ''' character position must be within the FullSpan of the Root syntax node in this SemanticModel.
        ''' </param>
        ''' <param name="initializer">A syntax node that represents a parsed initializer. This initializer should not be
        ''' present in the syntax tree associated with this object.</param>
        ''' <param name="speculativeModel">A SemanticModel object that can be used to inquire about the semantic
        ''' information associated with syntax nodes within <paramref name="initializer"/>.</param>
        ''' <returns>Flag indicating whether a speculative semantic model was created.</returns>
        ''' <exception cref="ArgumentException">Throws this exception if the <paramref name="initializer"/> node is contained any SyntaxTree in the current Compilation.</exception>
        ''' <exception cref="ArgumentNullException">Throws this exception if <paramref name="initializer"/> is null.</exception>
        ''' <exception cref="InvalidOperationException">Throws this exception if this model is a speculative semantic model, i.e. <see cref="IsSpeculativeSemanticModel"/> is True.
        ''' Chaining of speculative semantic model is not supported.</exception>
        Public Function TryGetSpeculativeSemanticModel(position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            CheckPosition(position)
            CheckModelAndSyntaxNodeToSpeculate(initializer)

            Dim speculativePublicModel As PublicSemanticModel = Nothing
            Dim result = TryGetSpeculativeSemanticModelCore(DirectCast(Me, SyntaxTreeSemanticModel), position, initializer, speculativePublicModel)
            speculativeModel = speculativePublicModel

            Return result
        End Function

        Friend MustOverride Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean

        ''' <summary>
        ''' Get a SemanticModel object that is associated with an attribute that did not appear in
        ''' this source code. This can be used to get detailed semantic information about sub-parts
        ''' of an attribute that did not appear in source code. 
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and accessibility. This
        ''' character position must be within the FullSpan of the Root syntax node in this SemanticModel.</param>
        ''' <param name="attribute">A syntax node that represents a parsed attribute. This attribute should not be
        ''' present in the syntax tree associated with this object.</param>
        ''' <param name="speculativeModel">A SemanticModel object that can be used to inquire about the semantic
        ''' information associated with syntax nodes within <paramref name="attribute"/>.</param>
        ''' <returns>Flag indicating whether a speculative semantic model was created.</returns>
        ''' <exception cref="ArgumentException">Throws this exception if the <paramref name="attribute"/> node is contained any SyntaxTree in the current Compilation.</exception>
        ''' <exception cref="ArgumentNullException">Throws this exception if <paramref name="attribute"/> is null.</exception>
        ''' <exception cref="InvalidOperationException">Throws this exception if this model is a speculative semantic model, i.e. <see cref="IsSpeculativeSemanticModel"/> is True.
        ''' Chaining of speculative semantic model is not supported.</exception>
        Public Function TryGetSpeculativeSemanticModel(position As Integer, attribute As AttributeSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            CheckPosition(position)
            CheckModelAndSyntaxNodeToSpeculate(attribute)

            Dim binder As Binder = Me.GetSpeculativeAttributeBinder(position, attribute)
            If binder Is Nothing Then
                speculativeModel = Nothing
                Return False
            End If

            speculativeModel = AttributeSemanticModel.CreateSpeculative(DirectCast(Me, SyntaxTreeSemanticModel), attribute, binder, position)
            Return True
        End Function

        ''' <summary>
        ''' Get a SemanticModel object that is associated with a type syntax that did not appear in
        ''' this source code. This can be used to get detailed semantic information about sub-parts
        ''' of a type syntax that did not appear in source code. 
        ''' </summary>
        ''' <param name="position">A character position used to identify a declaration scope and accessibility. This
        ''' character position must be within the FullSpan of the Root syntax node in this SemanticModel.
        ''' </param>
        ''' <param name="type">A syntax node that represents a parsed type syntax. This expression should not be
        ''' present in the syntax tree associated with this object.</param>
        ''' <param name="bindingOption">Indicates whether to bind the expression as a full expression,
        ''' or as a type or namespace.</param>
        ''' <param name="speculativeModel">A SemanticModel object that can be used to inquire about the semantic
        ''' information associated with syntax nodes within <paramref name="type"/>.</param>
        ''' <returns>Flag indicating whether a speculative semantic model was created.</returns>
        ''' <exception cref="ArgumentException">Throws this exception if the <paramref name="type"/> node is contained any SyntaxTree in the current Compilation.</exception>
        ''' <exception cref="InvalidOperationException">Throws this exception if this model is a speculative semantic model, i.e. <see cref="IsSpeculativeSemanticModel"/> is True.
        ''' Chaining of speculative semantic model is not supported.</exception>
        Public Function TryGetSpeculativeSemanticModel(position As Integer, type As TypeSyntax, <Out> ByRef speculativeModel As SemanticModel, Optional bindingOption As SpeculativeBindingOption = SpeculativeBindingOption.BindAsExpression) As Boolean
            CheckPosition(position)
            CheckModelAndSyntaxNodeToSpeculate(type)

            Dim speculativePublicModel As PublicSemanticModel = Nothing
            Dim result = TryGetSpeculativeSemanticModelCore(DirectCast(Me, SyntaxTreeSemanticModel), position, type, bindingOption, speculativePublicModel)
            speculativeModel = speculativePublicModel

            Return result
        End Function

        Friend MustOverride Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, type As TypeSyntax, bindingOption As SpeculativeBindingOption, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean

        ''' <summary>
        ''' If this is a speculative semantic model, then returns its parent semantic model.
        ''' Otherwise, returns null.
        ''' </summary>
        Public MustOverride Shadows ReadOnly Property ParentModel As SemanticModel

        ''' <summary>
        ''' Determines what type of conversion, if any, would be used if a given expression was
        ''' converted to a given type.
        ''' </summary>
        ''' <param name="expression">An expression which must occur within the syntax tree
        ''' associated with this object.</param>
        ''' <param name="destination">The type to attempt conversion to.</param>
        ''' <returns>Returns a Conversion object that summarizes whether the conversion was
        ''' possible, and if so, what kind of conversion it was. If no conversion was possible, a
        ''' Conversion object with a false "Exists " property is returned.</returns>
        ''' <remarks>To determine the conversion between two types (instead of an expression and a
        ''' type), use Compilation.ClassifyConversion.</remarks>
        Public MustOverride Shadows Function ClassifyConversion(expression As ExpressionSyntax, destination As ITypeSymbol) As Conversion

        ''' <summary>
        ''' Determines what type of conversion, if any, would be used if a given expression was
        ''' converted to a given type.
        ''' </summary>
        ''' <param name="position">The character position for determining the enclosing declaration scope and accessibility.</param>
        ''' <param name="expression">An expression to classify. This expression does not need to be
        ''' present in the syntax tree associated with this object.</param>
        ''' <param name="destination">The type to attempt conversion to.</param>
        ''' <returns>Returns a Conversion object that summarizes whether the conversion was
        ''' possible, and if so, what kind of conversion it was. If no conversion was possible, a
        ''' Conversion object with a false "Exists " property is returned.</returns>
        ''' <remarks>To determine the conversion between two types (instead of an expression and a
        ''' type), use Compilation.ClassifyConversion.</remarks>
        Public Shadows Function ClassifyConversion(position As Integer, expression As ExpressionSyntax, destination As ITypeSymbol) As Conversion
            If destination Is Nothing Then
                Throw New ArgumentNullException(NameOf(destination))
            End If

            Dim vbdestination = destination.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(destination))

            CheckPosition(position)
            Dim binder = Me.GetEnclosingBinder(position)

            If binder IsNot Nothing Then
                ' Add speculative binder to bind speculatively.
                binder = SpeculativeBinder.Create(binder)

                Dim bnode = binder.BindValue(expression, BindingDiagnosticBag.Discarded)

                If bnode IsNot Nothing AndAlso Not vbdestination.IsErrorType() Then
                    Return New Conversion(Conversions.ClassifyConversion(bnode, vbdestination, binder, CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
                End If
            End If

            Return New Conversion(Nothing) ' NoConversion
        End Function

        ''' <summary>
        ''' Given a modified identifier that is part of a variable declaration, get the
        ''' corresponding symbol.
        ''' </summary>
        ''' <param name="identifierSyntax">The modified identifier that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overridable Overloads Function GetDeclaredSymbol(identifierSyntax As ModifiedIdentifierSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            If identifierSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(identifierSyntax))
            End If
            If Not IsInTree(identifierSyntax) Then
                Throw New ArgumentException(VBResources.IdentifierSyntaxNotWithinSyntaxTree)
            End If

            Dim binder As Binder = Me.GetEnclosingBinder(identifierSyntax.SpanStart)
            Dim blockBinder = TryCast(StripSemanticModelBinder(binder), BlockBaseBinder)
            If blockBinder IsNot Nothing Then
                ' Most of the time, we should be able to find the identifier by name.
                Dim lookupResult As LookupResult = LookupResult.GetInstance()
                Try
                    ' NB: "binder", not "blockBinder", so that we don't incorrectly mark imports as used.
                    binder.Lookup(lookupResult, identifierSyntax.Identifier.ValueText, 0, Nothing, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
                    If lookupResult.IsGood Then
                        Dim sym As LocalSymbol = TryCast(lookupResult.Symbols(0), LocalSymbol)
                        If sym IsNot Nothing AndAlso sym.IdentifierToken = identifierSyntax.Identifier Then
                            Return sym
                        End If
                    End If
                Finally
                    lookupResult.Free()
                End Try

                ' In some error cases, like multiple symbols of the same name in the same scope, we
                ' need to do a linear search instead.
                For Each local In blockBinder.Locals
                    If local.IdentifierToken = identifierSyntax.Identifier Then
                        Return local
                    End If
                Next
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the corresponding symbol for a specified tuple element.
        ''' </summary>
        ''' <param name="elementSyntax">A TupleElementSyntax object.</param>
        ''' <param name="cancellationToken">A cancellation token.</param>
        ''' <returns>A symbol, for the specified element; otherwise Nothing. </returns>
        Public Overloads Function GetDeclaredSymbol(elementSyntax As TupleElementSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            CheckSyntaxNode(elementSyntax)

            Dim tupleTypeSyntax = TryCast(elementSyntax.Parent, TupleTypeSyntax)

            If tupleTypeSyntax IsNot Nothing Then
                Return TryCast(GetSymbolInfo(tupleTypeSyntax, cancellationToken).Symbol, TupleTypeSymbol)?.TupleElements.ElementAtOrDefault(tupleTypeSyntax.Elements.IndexOf(elementSyntax))
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a FieldInitializerSyntax, get the corresponding symbol of anonymous type property.
        ''' </summary>
        ''' <param name="fieldInitializerSyntax">The anonymous object creation field initializer syntax.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists or 
        ''' if the field initializer was not part of an anonymous type creation.</returns>
        Public Overridable Overloads Function GetDeclaredSymbol(fieldInitializerSyntax As FieldInitializerSyntax, Optional cancellationToken As CancellationToken = Nothing) As IPropertySymbol
            If fieldInitializerSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(fieldInitializerSyntax))
            End If
            If Not IsInTree(fieldInitializerSyntax) Then
                Throw New ArgumentException(VBResources.FieldInitializerSyntaxNotWithinSyntaxTree)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given an AnonymousObjectCreationExpressionSyntax, get the corresponding symbol of anonymous type.
        ''' </summary>
        ''' <param name="anonymousObjectCreationExpressionSyntax">The anonymous object creation syntax.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overridable Overloads Function GetDeclaredSymbol(anonymousObjectCreationExpressionSyntax As AnonymousObjectCreationExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            If anonymousObjectCreationExpressionSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(anonymousObjectCreationExpressionSyntax))
            End If
            If Not IsInTree(anonymousObjectCreationExpressionSyntax) Then
                Throw New ArgumentException(VBResources.AnonymousObjectCreationExpressionSyntaxNotWithinTree)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given an ExpressionRangeVariableSyntax, get the corresponding symbol.
        ''' </summary>
        ''' <param name="rangeVariableSyntax">The range variable syntax that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overridable Overloads Function GetDeclaredSymbol(rangeVariableSyntax As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            If rangeVariableSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(rangeVariableSyntax))
            End If
            If Not IsInTree(rangeVariableSyntax) Then
                Throw New ArgumentException(VBResources.RangeVariableSyntaxNotWithinSyntaxTree)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a CollectionRangeVariableSyntax, get the corresponding symbol.
        ''' </summary>
        ''' <param name="rangeVariableSyntax">The range variable syntax that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overridable Overloads Function GetDeclaredSymbol(rangeVariableSyntax As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            If rangeVariableSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(rangeVariableSyntax))
            End If
            If Not IsInTree(rangeVariableSyntax) Then
                Throw New ArgumentException(VBResources.RangeVariableSyntaxNotWithinSyntaxTree)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given an AggregationRangeVariableSyntax, get the corresponding symbol.
        ''' </summary>
        ''' <param name="rangeVariableSyntax">The range variable syntax that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overridable Overloads Function GetDeclaredSymbol(rangeVariableSyntax As AggregationRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            If rangeVariableSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(rangeVariableSyntax))
            End If
            If Not IsInTree(rangeVariableSyntax) Then
                Throw New ArgumentException(VBResources.RangeVariableSyntaxNotWithinSyntaxTree)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a label statement, get the corresponding label symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The label statement.</param>
        ''' <returns>The label symbol, or Nothing if no such symbol exists.</returns>
        Public Overridable Overloads Function GetDeclaredSymbol(declarationSyntax As LabelStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As ILabelSymbol
            If declarationSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(declarationSyntax))
            End If
            If Not IsInTree(declarationSyntax) Then
                Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinSyntaxTree)
            End If

            Dim binder = TryCast(StripSemanticModelBinder(Me.GetEnclosingBinder(declarationSyntax.SpanStart)), BlockBaseBinder)
            If binder IsNot Nothing Then
                Dim label As LabelSymbol = binder.LookupLabelByNameToken(declarationSyntax.LabelToken)
                If label IsNot Nothing Then
                    Return label
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a declarationSyntax that is part of a enum constant declaration, get the
        ''' corresponding symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The declarationSyntax that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public MustOverride Overloads Function GetDeclaredSymbol(declarationSyntax As EnumMemberDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As IFieldSymbol

        ''' <summary>
        ''' Given a type declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a type.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public MustOverride Overloads Function GetDeclaredSymbol(declarationSyntax As TypeStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol

        ''' <summary>
        ''' Given a type block, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a type block.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As TypeBlockSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            Return GetDeclaredSymbol(declarationSyntax.BlockStatement, cancellationToken)
        End Function

        ''' <summary>
        ''' Given a enum declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares an enum.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public MustOverride Overloads Function GetDeclaredSymbol(declarationSyntax As EnumStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol

        ''' <summary>
        ''' Given a enum block, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares an enum block.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As EnumBlockSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            Return GetDeclaredSymbol(declarationSyntax.EnumStatement, cancellationToken)
        End Function

        ''' <summary>
        ''' Given a namespace declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a namespace.</param>
        ''' <returns>The namespace symbol that was declared.</returns>
        Public MustOverride Overloads Function GetDeclaredSymbol(declarationSyntax As NamespaceStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamespaceSymbol

        ''' <summary>
        ''' Given a namespace block, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a namespace block.</param>
        ''' <returns>The namespace symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As NamespaceBlockSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamespaceSymbol
            Return GetDeclaredSymbol(declarationSyntax.NamespaceStatement, cancellationToken)
        End Function

        ''' <summary>
        ''' Given a method, property, or event declaration, get the corresponding symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a method, property, or event.</param>
        ''' <returns>The method, property, or event symbol that was declared.</returns>
        Friend MustOverride Overloads Function GetDeclaredSymbol(declarationSyntax As MethodBaseSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol

        ''' <summary>
        ''' Given a parameter declaration, get the corresponding parameter symbol.
        ''' </summary>
        ''' <param name="parameter">The syntax node that declares a parameter.</param>
        ''' <returns>The parameter symbol that was declared.</returns>
        Public MustOverride Overloads Function GetDeclaredSymbol(parameter As ParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As IParameterSymbol

        ''' <summary>
        ''' Given a type parameter declaration, get the corresponding type parameter symbol.
        ''' </summary>
        ''' <param name="typeParameter">The syntax node that declares a type parameter.</param>
        ''' <returns>The type parameter symbol that was declared.</returns>
        Public MustOverride Overloads Function GetDeclaredSymbol(typeParameter As TypeParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As ITypeParameterSymbol

        ''' <summary>
        ''' Given a delegate statement syntax get the corresponding named type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a delegate.</param>
        ''' <returns>The named type that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As DelegateStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As NamedTypeSymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), NamedTypeSymbol)
        End Function

        ''' <summary>
        ''' Given a constructor statement syntax get the corresponding method symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a constructor.</param>
        ''' <returns>The method symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As SubNewStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As IMethodSymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), MethodSymbol)
        End Function

        ''' <summary>
        ''' Given a method statement syntax get the corresponding method symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a method.</param>
        ''' <returns>The method symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As MethodStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As IMethodSymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), MethodSymbol)
        End Function

        ''' <summary>
        ''' Given a method statement syntax get the corresponding method symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a method.</param>
        ''' <returns>The method symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As DeclareStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As IMethodSymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), MethodSymbol)
        End Function

        ''' <summary>
        ''' Given a operator statement syntax get the corresponding method symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares an operator.</param>
        ''' <returns>The method symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As OperatorStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As IMethodSymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), MethodSymbol)
        End Function

        ''' <summary>
        ''' Given a method block syntax get the corresponding method, property or event symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares method, property or event.</param>
        ''' <returns>The method, property or event symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As MethodBlockBaseSyntax, Optional cancellationToken As CancellationToken = Nothing) As IMethodSymbol
            Return DirectCast(GetDeclaredSymbol(declarationSyntax.BlockStatement, cancellationToken), MethodSymbol)
        End Function

        ''' <summary>
        ''' Given a property statement syntax get the corresponding property symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a property.</param>
        ''' <returns>The property symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As PropertyStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As IPropertySymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), PropertySymbol)
        End Function

        ''' <summary>
        ''' Given an event statement syntax get the corresponding event symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares an event.</param>
        ''' <returns>The event symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As EventStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As IEventSymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), EventSymbol)
        End Function

        ''' <summary>
        ''' Given a property block syntax get the corresponding property symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares property.</param>
        ''' <returns>The property symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As PropertyBlockSyntax, Optional cancellationToken As CancellationToken = Nothing) As IPropertySymbol
            Return GetDeclaredSymbol(declarationSyntax.PropertyStatement, cancellationToken)
        End Function

        ''' <summary>
        ''' Given a custom event block syntax get the corresponding event symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares the custom event.</param>
        ''' <returns>The event symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As EventBlockSyntax, Optional cancellationToken As CancellationToken = Nothing) As IEventSymbol
            Return GetDeclaredSymbol(declarationSyntax.EventStatement, cancellationToken)
        End Function

        ''' <summary>
        ''' Given a catch statement syntax get the corresponding local symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The catch statement syntax node.</param>
        ''' <returns>The local symbol that was declared by the Catch statement or Nothing if statement does not declare a local variable.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As CatchStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As ILocalSymbol
            Dim enclosingBinder = StripSemanticModelBinder(Me.GetEnclosingBinder(declarationSyntax.SpanStart))
            Dim catchBinder = TryCast(enclosingBinder, CatchBlockBinder)

            If catchBinder IsNot Nothing Then
                Return catchBinder.Locals.FirstOrDefault
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a property block syntax get the corresponding property symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares property.</param>
        ''' <returns>The property symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As AccessorStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As IMethodSymbol
            Return DirectCast(GetDeclaredSymbol(DirectCast(declarationSyntax, MethodBaseSyntax), cancellationToken), MethodSymbol)
        End Function

        ''' <summary>
        ''' Given an import clause get the corresponding symbol for the import alias that was introduced.
        ''' </summary>
        ''' <param name="declarationSyntax">The import statement syntax node.</param>
        ''' <returns>The alias symbol that was declared or Nothing if no alias symbol was declared.</returns>
        Public MustOverride Overloads Function GetDeclaredSymbol(declarationSyntax As SimpleImportsClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As IAliasSymbol

        ''' <summary>
        ''' Given a field declaration syntax, get the corresponding symbols.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares one or more fields.</param>
        ''' <returns>The field symbols that were declared.</returns>
        Friend MustOverride Function GetDeclaredSymbols(declarationSyntax As FieldDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)

        ''' <summary>
        ''' Gets bound node summary of the underlying invocation in a case of RaiseEvent
        ''' </summary>
        Friend MustOverride Function GetInvokeSummaryForRaiseEvent(node As RaiseEventStatementSyntax) As BoundNodeSummary

        ' Get the symbol info of a named argument in an invocation-like expression.
        Private Function GetNamedArgumentSymbolInfo(identifierNameSyntax As IdentifierNameSyntax, cancellationToken As CancellationToken) As SymbolInfo
            Debug.Assert(SyntaxFacts.IsNamedArgumentName(identifierNameSyntax))

            ' Argument names do not have bound nodes associated with them, so we cannot use the usual
            ' GetSemanticInfo mechanism. Instead, we just do the following:
            '   1. Find the containing invocation.
            '   2. Call GetSemanticInfo on that.
            '   3. For each method or indexer in the return semantic info, find the argument
            '      with the given name (if any).
            '   4. Use the ResultKind in that semantic info and any symbols to create the semantic info
            '      for the named argument.
            '   5. Type is always null, as is constant value.

            Dim argumentName As String = identifierNameSyntax.Identifier.ValueText
            If argumentName.Length = 0 Then
                Return SymbolInfo.None
            End If

            ' RaiseEvent Invocation(SimpleArgument(((Identifier):=)(Expression))
            ' check for RaiseEvent here, it is not an expression.
            If identifierNameSyntax.Parent.Parent.Parent.Parent.Kind = SyntaxKind.RaiseEventStatement Then
                Dim asRaiseEvent = DirectCast(identifierNameSyntax.Parent.Parent.Parent.Parent, RaiseEventStatementSyntax)
                Return GetNamedArgumentSymbolInfoInRaiseEvent(argumentName, asRaiseEvent)
            End If

            ' Invocation(SimpleArgument(((Identifier):=)(Expression))
            Dim containingInvocation = DirectCast(identifierNameSyntax.Parent.Parent.Parent.Parent, ExpressionSyntax)

            Dim containingInvocationInfo As SymbolInfo = GetExpressionSymbolInfo(containingInvocation, SymbolInfoOptions.PreferConstructorsToType Or SymbolInfoOptions.ResolveAliases, cancellationToken)

            Return FindNameParameterInfo(containingInvocationInfo.GetAllSymbols().Cast(Of Symbol).ToImmutableArray(),
                                         argumentName,
                                         containingInvocationInfo.CandidateReason)
        End Function

        ''' <summary>
        ''' RaiseEvent situation is very special: 
        ''' 1) Unlike other syntaxes that take named arguments, RaiseEvent is a statement. 
        ''' 2) RaiseEvent is essentially a wrapper around underlying call to the event rising method.
        '''    Note that while event itself may have named parameters in its syntax, their names could be irrelevant
        '''    For the purpose of fetching named parameters, it is the target of the call that we are interested in.
        '''    
        '''    === Example:
        ''' 
        ''' Interface I1
        '''    Event E(qwer As Integer)  
        ''' End Interface
        ''' 
        ''' Class cls1 : Implements I1
        '''    Event E3(bar As Integer) Implements I1.E   '  "bar" means nothing here. Only type matters.
        '''
        '''    Sub moo()
        '''        RaiseEvent E3(qwer:=123)  ' qwer binds to parameter on I1.EEventhandler.invoke(goo)
        '''    End Sub
        '''End Class
        ''' 
        ''' 
        ''' </summary>
        Private Function GetNamedArgumentSymbolInfoInRaiseEvent(argumentName As String,
                                                                containingRaiseEvent As RaiseEventStatementSyntax) As SymbolInfo

            Dim summary = GetInvokeSummaryForRaiseEvent(containingRaiseEvent)

            ' Determine the symbols, resultKind, and member group.
            Dim resultKind As LookupResultKind = LookupResultKind.Empty
            Dim memberGroup As ImmutableArray(Of Symbol) = Nothing
            Dim containingInvocationInfosymbols As ImmutableArray(Of Symbol) = GetSemanticSymbols(summary,
                                                                                                 Nothing,
                                                                                                 SymbolInfoOptions.PreferConstructorsToType Or SymbolInfoOptions.ResolveAliases,
                                                                                                 resultKind,
                                                                                                 memberGroup)

            Return FindNameParameterInfo(containingInvocationInfosymbols,
                                         argumentName,
                                         If(resultKind = LookupResultKind.Good, CandidateReason.None, resultKind.ToCandidateReason()))
        End Function

        Private Function FindNameParameterInfo(invocationInfosymbols As ImmutableArray(Of Symbol),
                                               arGumentName As String,
                                               reason As CandidateReason) As SymbolInfo

            Dim symbols As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()

            For Each invocationSym In invocationInfosymbols
                Dim param As ParameterSymbol = FindNamedParameter(invocationSym, arGumentName)
                If param IsNot Nothing Then
                    symbols.Add(param)
                End If
            Next

            If symbols.Count = 0 Then
                symbols.Free()
                Return SymbolInfo.None
            Else
                Return SymbolInfoFactory.Create(StaticCast(Of ISymbol).From(symbols.ToImmutableAndFree()), reason)
            End If
        End Function

        ' Find the first parameter, if any, on method or property symbol named "argumentName"
        Private Function FindNamedParameter(symbol As Symbol, argumentName As String) As ParameterSymbol
            Dim params As ImmutableArray(Of ParameterSymbol)

            If symbol.Kind = SymbolKind.Method Then
                params = DirectCast(symbol, MethodSymbol).Parameters
            ElseIf symbol.Kind = SymbolKind.Property Then
                params = DirectCast(symbol, PropertySymbol).Parameters
            Else
                Return Nothing
            End If

            For Each param In params
                If CaseInsensitiveComparison.Equals(param.Name, argumentName) Then
                    Return param
                End If
            Next

            Return Nothing
        End Function

        ''' <summary> 
        ''' The SyntaxTree that is bound
        ''' </summary> 
        Public MustOverride Shadows ReadOnly Property SyntaxTree As SyntaxTree

        ''' <summary>
        ''' Gets the semantic information of a for each statement.
        ''' </summary>
        ''' <param name="node">The for each syntax node.</param>
        Public Shadows Function GetForEachStatementInfo(node As ForEachStatementSyntax) As ForEachStatementInfo
            If node.Parent IsNot Nothing AndAlso node.Parent.Kind = SyntaxKind.ForEachBlock Then
                Return GetForEachStatementInfoWorker(DirectCast(node.Parent, ForEachBlockSyntax))
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the semantic information of a for each statement.
        ''' </summary>
        ''' <param name="node">The for block syntax node.</param>
        Public Shadows Function GetForEachStatementInfo(node As ForEachBlockSyntax) As ForEachStatementInfo
            If node.Kind = SyntaxKind.ForEachBlock Then
                Return GetForEachStatementInfoWorker(node)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the semantic information of a for each statement.
        ''' </summary>
        ''' <param name="node">The for each syntax node.</param>
        Friend MustOverride Function GetForEachStatementInfoWorker(node As ForEachBlockSyntax) As ForEachStatementInfo

        ''' <summary>
        ''' Gets the semantic information of an Await expression.
        ''' </summary>
        Public Function GetAwaitExpressionInfo(awaitExpression As AwaitExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As AwaitExpressionInfo
            CheckSyntaxNode(awaitExpression)

            If CanGetSemanticInfo(awaitExpression) Then
                Return GetAwaitExpressionInfoWorker(awaitExpression, cancellationToken)
            Else
                Return Nothing
            End If
        End Function

        Friend MustOverride Function GetAwaitExpressionInfoWorker(awaitExpression As AwaitExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As AwaitExpressionInfo

        ''' <summary>
        ''' If the given node is within a preprocessing directive, gets the preprocessing symbol info for it.
        ''' </summary>
        ''' <param name="node">Preprocessing symbol identifier node.</param>
        Public Shadows Function GetPreprocessingSymbolInfo(node As IdentifierNameSyntax) As VisualBasicPreprocessingSymbolInfo
            CheckSyntaxNode(node)

            If SyntaxFacts.IsWithinPreprocessorConditionalExpression(node) Then
                Dim symbolInfo As VisualBasicPreprocessingSymbolInfo = node.SyntaxTree.GetPreprocessingSymbolInfo(node)

                If symbolInfo.Symbol IsNot Nothing Then
                    Debug.Assert(CaseInsensitiveComparison.Equals(symbolInfo.Symbol.Name, node.Identifier.ValueText))
                    Return symbolInfo
                End If

                Return New VisualBasicPreprocessingSymbolInfo(New PreprocessingSymbol(node.Identifier.ValueText), constantValueOpt:=Nothing, isDefined:=False)
            End If

            Return VisualBasicPreprocessingSymbolInfo.None
        End Function

        ''' <summary>
        ''' Options to control the internal working of GetSemanticInfoWorker. Not currently exposed
        ''' to public clients, but could be if desired.
        ''' </summary>
        Friend Enum SymbolInfoOptions
            ''' <summary>
            ''' When binding "C" new C(...), return the type C and do not return information about
            ''' which constructor was bound to. Bind "new C(...)" to get information about which constructor
            ''' was chosen.
            ''' </summary>
            PreferTypeToConstructors = &H1

            ''' <summary>
            ''' When binding "C" new C(...), return the constructor of C that was bound to, if C unambiguously
            ''' binds to a single type with at least one constructor. 
            ''' </summary>
            PreferConstructorsToType = &H2

            ''' <summary>
            ''' When binding a name X that was declared with a "using X=OtherTypeOrNamespace", return OtherTypeOrNamespace.
            ''' </summary>            
            ResolveAliases = &H4

            ''' <summary>
            ''' When binding a name X that was declared with a "using X=OtherTypeOrNamespace", return the alias symbol X.
            ''' </summary>
            PreserveAliases = &H8

            ' Default options
            DefaultOptions = PreferConstructorsToType Or ResolveAliases
        End Enum

        Friend Sub ValidateSymbolInfoOptions(options As SymbolInfoOptions)
            Debug.Assert(((options And SymbolInfoOptions.PreferConstructorsToType) <> 0) <> ((options And SymbolInfoOptions.PreferTypeToConstructors) <> 0), "Options are mutually exclusive")
            Debug.Assert(((options And SymbolInfoOptions.ResolveAliases) <> 0) <> ((options And SymbolInfoOptions.PreserveAliases) <> 0), "Options are mutually exclusive")
        End Sub

        ''' <summary>
        ''' Given a position in the SyntaxTree for this SemanticModel returns the innermost Symbol
        ''' that the position is considered inside of. 
        ''' </summary>
        Public Shadows Function GetEnclosingSymbol(position As Integer, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            CheckPosition(position)
            Dim binder = Me.GetEnclosingBinder(position)
            Return If(binder Is Nothing, Nothing, binder.ContainingMember)
        End Function

        ''' <summary>
        ''' Get the state of Option Strict for the code covered by this semantic model.
        ''' This takes into effect both file-level "Option Strict" statements and the project-level
        ''' defaults.
        ''' </summary>
        Public ReadOnly Property OptionStrict As VisualBasic.OptionStrict
            Get
                ' Since options never change within a file, we can just use the start location.
                Dim binder = Me.GetEnclosingBinder(Root.SpanStart) ' should never return null.
                Return binder.OptionStrict
            End Get
        End Property

        ''' <summary>
        ''' Get the state of Option Infer for the code covered by this semantic model.
        ''' This takes into effect both file-level "Option Infer" statements and the project-level
        ''' defaults.
        ''' </summary>
        ''' <value>True if Option Infer On, False if Option Infer Off.</value>
        Public ReadOnly Property OptionInfer As Boolean
            Get
                ' Since options never change within a file, we can just use the start location.
                Dim binder = Me.GetEnclosingBinder(Root.SpanStart) ' should never return null.
                Return binder.OptionInfer
            End Get
        End Property

        ''' <summary>
        ''' Get the state of Option Explicit for the code covered by this semantic model.
        ''' This takes into effect both file-level "Option Explicit" statements and the project-level
        ''' defaults.
        ''' </summary>
        ''' <value>True if Option Explicit On, False if Option Explicit Off.</value>
        Public ReadOnly Property OptionExplicit As Boolean
            Get
                ' Since options never change within a file, we can just use the start location.
                Dim binder = Me.GetEnclosingBinder(Root.SpanStart) ' should never return null.
                Return binder.OptionExplicit
            End Get
        End Property

        ''' <summary>
        ''' Get the state of Option Compare for the code covered by this semantic model.
        ''' This takes into effect both file-level "Option Compare" statements and the project-level
        ''' defaults.
        ''' </summary>
        ''' <value>True if Option Compare Text, False if Option Compare Binary.</value>
        Public ReadOnly Property OptionCompareText As Boolean
            Get
                ' Since options never change within a file, we can just use the start location.
                Dim binder = Me.GetEnclosingBinder(Root.SpanStart) ' should never return null.
                Return binder.OptionCompareText
            End Get
        End Property

        Friend Shared Function StripSemanticModelBinder(binder As Binder) As Binder
            If binder Is Nothing OrElse Not binder.IsSemanticModelBinder Then
                Return binder
            End If

            Return If(TypeOf binder Is SemanticModelBinder, binder.ContainingBinder, binder)
        End Function

#Region "SemanticModel"

        Public NotOverridable Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property ParentModelCore As SemanticModel
            Get
                Return Me.ParentModel
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property SyntaxTreeCore As SyntaxTree
            Get
                Return Me.SyntaxTree
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property CompilationCore As Compilation
            Get
                Return Me.Compilation
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property RootCore As SyntaxNode
            Get
                Return Me.Root
            End Get
        End Property

        Private Function GetSymbolInfoForNode(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim expressionSyntax = TryCast(node, ExpressionSyntax)
            If expressionSyntax IsNot Nothing Then
                Return Me.GetSymbolInfo(expressionSyntax, cancellationToken)
            End If

            Dim attributeSyntax = TryCast(node, AttributeSyntax)
            If attributeSyntax IsNot Nothing Then
                Return Me.GetSymbolInfo(attributeSyntax, cancellationToken)
            End If

            Dim clauseSyntax = TryCast(node, QueryClauseSyntax)
            If clauseSyntax IsNot Nothing Then
                Return Me.GetSymbolInfo(clauseSyntax, cancellationToken)
            End If

            Dim letVariable = TryCast(node, ExpressionRangeVariableSyntax)
            If letVariable IsNot Nothing Then
                Return Me.GetSymbolInfo(letVariable, cancellationToken)
            End If

            Dim ordering = TryCast(node, OrderingSyntax)
            If ordering IsNot Nothing Then
                Return Me.GetSymbolInfo(ordering, cancellationToken)
            End If

            Dim [function] = TryCast(node, FunctionAggregationSyntax)
            If [function] IsNot Nothing Then
                Return Me.GetSymbolInfo([function], cancellationToken)
            End If

            Dim cref = TryCast(node, CrefReferenceSyntax)
            If cref IsNot Nothing Then
                Return Me.GetSymbolInfo(cref, cancellationToken)
            End If

            Return SymbolInfo.None
        End Function

        Private Function GetTypeInfoForNode(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim expressionSyntax = TryCast(node, ExpressionSyntax)
            If expressionSyntax IsNot Nothing Then
                Return Me.GetTypeInfoWorker(expressionSyntax, cancellationToken)
            End If

            Dim attributeSyntax = TryCast(node, AttributeSyntax)
            If attributeSyntax IsNot Nothing Then
                Return Me.GetTypeInfoWorker(attributeSyntax, cancellationToken)
            End If

            Return VisualBasicTypeInfo.None
        End Function

        Private Function GetMemberGroupForNode(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim expressionSyntax = TryCast(node, ExpressionSyntax)
            If expressionSyntax IsNot Nothing Then
                Return Me.GetMemberGroup(expressionSyntax, cancellationToken)
            End If

            Dim attributeSyntax = TryCast(node, AttributeSyntax)
            If attributeSyntax IsNot Nothing Then
                Return Me.GetMemberGroup(attributeSyntax, cancellationToken)
            End If

            Return ImmutableArray(Of ISymbol).Empty
        End Function

        Protected NotOverridable Overrides Function GetSpeculativeTypeInfoCore(position As Integer, expression As SyntaxNode, bindingOption As SpeculativeBindingOption) As TypeInfo
            Return If(TypeOf expression Is ExpressionSyntax,
                       Me.GetSpeculativeTypeInfo(position, DirectCast(expression, ExpressionSyntax), bindingOption),
                       Nothing)
        End Function

        Protected NotOverridable Overrides Function GetSpeculativeSymbolInfoCore(position As Integer, expression As SyntaxNode, bindingOption As SpeculativeBindingOption) As SymbolInfo
            Return If(TypeOf expression Is ExpressionSyntax,
                       GetSpeculativeSymbolInfo(position, DirectCast(expression, ExpressionSyntax), bindingOption),
                       Nothing)
        End Function

        Protected NotOverridable Overrides Function GetSpeculativeAliasInfoCore(position As Integer, nameSyntax As SyntaxNode, bindingOption As SpeculativeBindingOption) As IAliasSymbol
            Return If(TypeOf nameSyntax Is IdentifierNameSyntax,
                       GetSpeculativeAliasInfo(position, DirectCast(nameSyntax, IdentifierNameSyntax), bindingOption),
                       Nothing)
        End Function

        Protected NotOverridable Overrides Function GetSymbolInfoCore(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return GetSymbolInfoForNode(node, cancellationToken)
        End Function

        Protected NotOverridable Overrides Function GetTypeInfoCore(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As TypeInfo
            Return GetTypeInfoForNode(node, cancellationToken)
        End Function

        Protected NotOverridable Overrides Function GetAliasInfoCore(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As IAliasSymbol
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim nameSyntax = TryCast(node, IdentifierNameSyntax)
            If nameSyntax IsNot Nothing Then
                Return GetAliasInfo(nameSyntax, cancellationToken)
            End If

            Return Nothing
        End Function

        Protected NotOverridable Overrides Function GetPreprocessingSymbolInfoCore(node As SyntaxNode) As PreprocessingSymbolInfo
            Dim nameSyntax = TryCast(node, IdentifierNameSyntax)
            If nameSyntax IsNot Nothing Then
                Return GetPreprocessingSymbolInfo(nameSyntax)
            End If

            Return Nothing
        End Function

        Protected NotOverridable Overrides Function GetMemberGroupCore(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            Return GetMemberGroupForNode(node, cancellationToken)
        End Function

        Protected NotOverridable Overrides Function LookupSymbolsCore(position As Integer, container As INamespaceOrTypeSymbol, name As String, includeReducedExtensionMethods As Boolean) As ImmutableArray(Of ISymbol)
            Return LookupSymbols(position, ToLanguageSpecific(container), name, includeReducedExtensionMethods)
        End Function

        Protected NotOverridable Overrides Function LookupBaseMembersCore(position As Integer, name As String) As ImmutableArray(Of ISymbol)
            Return LookupBaseMembers(position, name)
        End Function

        Protected NotOverridable Overrides Function LookupStaticMembersCore(position As Integer, container As INamespaceOrTypeSymbol, name As String) As ImmutableArray(Of ISymbol)
            Return LookupStaticMembers(position, ToLanguageSpecific(container), name)
        End Function

        Protected NotOverridable Overrides Function LookupNamespacesAndTypesCore(position As Integer, container As INamespaceOrTypeSymbol, name As String) As ImmutableArray(Of ISymbol)
            Return LookupNamespacesAndTypes(position, ToLanguageSpecific(container), name)
        End Function

        Protected NotOverridable Overrides Function LookupLabelsCore(position As Integer, name As String) As ImmutableArray(Of ISymbol)
            Return LookupLabels(position, name)
        End Function

        Private Shared Function ToLanguageSpecific(container As INamespaceOrTypeSymbol) As NamespaceOrTypeSymbol
            If container Is Nothing Then
                Return Nothing
            End If

            Dim result = TryCast(container, NamespaceOrTypeSymbol)
            If result Is Nothing Then
                Throw New ArgumentException(VBResources.NotAVbSymbol, NameOf(container))
            End If
            Return result
        End Function

        Protected NotOverridable Overrides Function GetDeclaredSymbolCore(declaration As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            cancellationToken.ThrowIfCancellationRequested()

            Dim node = DirectCast(declaration, VisualBasicSyntaxNode)

            Select Case node.Kind
                Case SyntaxKind.SimpleImportsClause
                    Return Me.GetDeclaredSymbol(DirectCast(node, SimpleImportsClauseSyntax), cancellationToken)

                Case SyntaxKind.TypedTupleElement,
                     SyntaxKind.NamedTupleElement
                    Return Me.GetDeclaredSymbol(DirectCast(node, TupleElementSyntax), cancellationToken)

                Case SyntaxKind.ModifiedIdentifier
                    Return Me.GetDeclaredSymbol(DirectCast(node, ModifiedIdentifierSyntax), cancellationToken)

                Case SyntaxKind.EnumMemberDeclaration
                    Return Me.GetDeclaredSymbol(DirectCast(node, EnumMemberDeclarationSyntax), cancellationToken)

                Case SyntaxKind.Parameter
                    Return Me.GetDeclaredSymbol(DirectCast(node, ParameterSyntax), cancellationToken)

                Case SyntaxKind.TypeParameter
                    Return Me.GetDeclaredSymbol(DirectCast(node, TypeParameterSyntax), cancellationToken)

                Case SyntaxKind.LabelStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, LabelStatementSyntax), cancellationToken)

                Case SyntaxKind.NamespaceStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, NamespaceStatementSyntax), cancellationToken)

                Case SyntaxKind.ClassStatement, SyntaxKind.StructureStatement, SyntaxKind.InterfaceStatement, SyntaxKind.ModuleStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, TypeStatementSyntax), cancellationToken)

                Case SyntaxKind.EnumStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, EnumStatementSyntax), cancellationToken)

                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, DelegateStatementSyntax), cancellationToken)

                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, MethodStatementSyntax), cancellationToken)

                Case SyntaxKind.PropertyStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, PropertyStatementSyntax), cancellationToken)

                Case SyntaxKind.EventStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, EventStatementSyntax), cancellationToken)

                Case SyntaxKind.SubNewStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, SubNewStatementSyntax), cancellationToken)

                Case SyntaxKind.GetAccessorStatement, SyntaxKind.SetAccessorStatement,
                 SyntaxKind.AddHandlerAccessorStatement, SyntaxKind.RemoveHandlerAccessorStatement, SyntaxKind.RaiseEventAccessorStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, AccessorStatementSyntax), cancellationToken)

                Case SyntaxKind.OperatorStatement,
                 SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, MethodBaseSyntax), cancellationToken)

                Case SyntaxKind.NamespaceBlock
                    Return Me.GetDeclaredSymbol(DirectCast(node, NamespaceBlockSyntax), cancellationToken)

                Case SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock
                    Return Me.GetDeclaredSymbol(DirectCast(node, TypeBlockSyntax), cancellationToken)

                Case SyntaxKind.EnumBlock
                    Return Me.GetDeclaredSymbol(DirectCast(node, EnumBlockSyntax), cancellationToken)

                Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock, SyntaxKind.OperatorBlock,
                 SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock,
                 SyntaxKind.AddHandlerAccessorBlock, SyntaxKind.RemoveHandlerAccessorBlock, SyntaxKind.RaiseEventAccessorBlock
                    Return Me.GetDeclaredSymbol(DirectCast(node, MethodBlockBaseSyntax), cancellationToken)

                Case SyntaxKind.PropertyBlock
                    Return Me.GetDeclaredSymbol(DirectCast(node, PropertyBlockSyntax), cancellationToken)

                Case SyntaxKind.EventBlock
                    Return Me.GetDeclaredSymbol(DirectCast(node, EventBlockSyntax), cancellationToken)

                Case SyntaxKind.CollectionRangeVariable
                    Return Me.GetDeclaredSymbol(DirectCast(node, CollectionRangeVariableSyntax), cancellationToken)

                Case SyntaxKind.ExpressionRangeVariable
                    Return Me.GetDeclaredSymbol(DirectCast(node, ExpressionRangeVariableSyntax), cancellationToken)

                Case SyntaxKind.AggregationRangeVariable
                    Return Me.GetDeclaredSymbol(DirectCast(node, AggregationRangeVariableSyntax), cancellationToken)

                Case SyntaxKind.CatchStatement
                    Return Me.GetDeclaredSymbol(DirectCast(node, CatchStatementSyntax), cancellationToken)

                Case SyntaxKind.InferredFieldInitializer, SyntaxKind.NamedFieldInitializer
                    Return Me.GetDeclaredSymbol(DirectCast(node, FieldInitializerSyntax), cancellationToken)

                Case SyntaxKind.AnonymousObjectCreationExpression
                    Return Me.GetDeclaredSymbol(DirectCast(node, AnonymousObjectCreationExpressionSyntax), cancellationToken)
            End Select

            Dim td = TryCast(node, TypeStatementSyntax)
            If td IsNot Nothing Then
                Return Me.GetDeclaredSymbol(td, cancellationToken)
            End If

            Dim md = TryCast(node, MethodBaseSyntax)
            If md IsNot Nothing Then
                Return Me.GetDeclaredSymbol(md, cancellationToken)
            End If

            Return Nothing
        End Function

        Protected NotOverridable Overrides Function GetDeclaredSymbolsCore(declaration As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            cancellationToken.ThrowIfCancellationRequested()

            Dim field = TryCast(declaration, FieldDeclarationSyntax)
            If field IsNot Nothing Then
                Return Me.GetDeclaredSymbols(field, cancellationToken)
            End If

            Dim symbol = GetDeclaredSymbolCore(declaration, cancellationToken)
            If symbol IsNot Nothing Then
                Return ImmutableArray.Create(symbol)
            End If

            Return ImmutableArray.Create(Of ISymbol)()
        End Function

        Protected NotOverridable Overrides Function AnalyzeDataFlowCore(firstStatement As SyntaxNode, lastStatement As SyntaxNode) As DataFlowAnalysis
            Return Me.AnalyzeDataFlow(SafeCastArgument(Of StatementSyntax)(firstStatement, NameOf(firstStatement)),
                                                SafeCastArgument(Of StatementSyntax)(lastStatement, NameOf(lastStatement)))
        End Function

        Protected NotOverridable Overrides Function AnalyzeDataFlowCore(statementOrExpression As SyntaxNode) As DataFlowAnalysis

            If statementOrExpression Is Nothing Then
                Throw New ArgumentNullException(NameOf(statementOrExpression))
            End If

            If TypeOf statementOrExpression Is ExecutableStatementSyntax Then

                Return Me.AnalyzeDataFlow(DirectCast(statementOrExpression, StatementSyntax))

            ElseIf TypeOf statementOrExpression Is ExpressionSyntax Then

                Return Me.AnalyzeDataFlow(DirectCast(statementOrExpression, ExpressionSyntax))

            Else

                Throw New ArgumentException(VBResources.StatementOrExpressionIsNotAValidType)

            End If
        End Function

        Protected NotOverridable Overrides Function AnalyzeControlFlowCore(firstStatement As SyntaxNode, lastStatement As SyntaxNode) As ControlFlowAnalysis
            Return Me.AnalyzeControlFlow(SafeCastArgument(Of StatementSyntax)(firstStatement, NameOf(firstStatement)),
                                                   SafeCastArgument(Of StatementSyntax)(lastStatement, NameOf(lastStatement)))
        End Function

        Protected NotOverridable Overrides Function AnalyzeControlFlowCore(statement As SyntaxNode) As ControlFlowAnalysis
            Return Me.AnalyzeControlFlow(SafeCastArgument(Of StatementSyntax)(statement, NameOf(statement)))
        End Function

        Private Shared Function SafeCastArgument(Of T As Class)(node As SyntaxNode, argName As String) As T
            If node Is Nothing Then
                Throw New ArgumentNullException(argName)
            End If
            Dim casted = TryCast(node, T)
            If casted Is Nothing Then
                Throw New ArgumentException(argName & " is not an " & GetType(T).Name)
            End If
            Return casted
        End Function

        Protected NotOverridable Overrides Function GetConstantValueCore(node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As [Optional](Of Object)

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            If TypeOf node Is ExpressionSyntax Then
                Return GetConstantValue(DirectCast(node, ExpressionSyntax), cancellationToken)
            End If

            Return Nothing
        End Function

        Protected NotOverridable Overrides Function GetEnclosingSymbolCore(position As Integer, Optional cancellationToken As System.Threading.CancellationToken = Nothing) As ISymbol
            Return GetEnclosingSymbol(position, cancellationToken)
        End Function

        Private Protected NotOverridable Overrides Function GetImportScopesCore(position As Integer, cancellationToken As CancellationToken) As ImmutableArray(Of IImportScope)
            CheckPosition(position)
            Dim binder = Me.GetEnclosingBinder(position)

            Dim importScopes = ArrayBuilder(Of IImportScope).GetInstance()
            AddImportScopes(binder, importScopes)
            Return importScopes.ToImmutableAndFree()
        End Function

        Private Shared Sub AddImportScopes(binder As Binder, scopes As ArrayBuilder(Of IImportScope))
            ' The binder chain has the following in it (walking from the innermost level outwards)
            '
            ' 1. Optional binders for the compilation unit of the present source file.
            ' 2. SourceFileBinder.  Required.
            ' 3. Optional binders for the imports brought in by the compilation options.
            '
            ' Both '1' and '3' are the same binders.  Specifically:
            '
            ' a. XmlNamespaceImportsBinder. Optional.  Present if source file has xml imports present.
            ' b. ImportAliasesBinder. Optional.  Present if source file has import aliases present.
            ' c. TypesOfImportedNamespacesMembersBinder.  Optional.  Present if source file has type or namespace imports present.
            '
            ' As such, we can walk upwards looking for any of these binders if present until we hit the end of the
            ' binder chain.  We know which set we're in depending on if we've seen the SourceFileBinder or not.
            '
            ' This also means that in VB the max length of the import chain is two, while in C# it can be unbounded
            ' in length.

            Dim typesOfImportedNamespacesMembers As TypesOfImportedNamespacesMembersBinder = Nothing
            Dim importAliases As ImportAliasesBinder = Nothing
            Dim xmlNamespaceImports As XmlNamespaceImportsBinder = Nothing

            While binder IsNot Nothing
                If TypeOf binder Is SourceFileBinder Then
                    ' We hit the source file binder.  That means anything we found up till now were the imports for this
                    ' file.  Recurse and try to create the outer optional node, and then create a potential node for
                    ' this level to chain onto that.
                    AddImportScopeNode(
                       typesOfImportedNamespacesMembers, importAliases, xmlNamespaceImports, scopes)

                    AddImportScopes(binder.ContainingBinder, scopes)
                    Return
                End If

                typesOfImportedNamespacesMembers = If(typesOfImportedNamespacesMembers, TryCast(binder, TypesOfImportedNamespacesMembersBinder))
                importAliases = If(importAliases, TryCast(binder, ImportAliasesBinder))
                xmlNamespaceImports = If(xmlNamespaceImports, TryCast(binder, XmlNamespaceImportsBinder))

                binder = binder.ContainingBinder
            End While

            ' We hit the end of the binder chain.  Anything we found up till now are the compilation option imports
            AddImportScopeNode(
                typesOfImportedNamespacesMembers, importAliases, xmlNamespaceImports, scopes)
        End Sub

        Private Shared Sub AddImportScopeNode(
                typesOfImportedNamespacesMembers As TypesOfImportedNamespacesMembersBinder,
                importAliases As ImportAliasesBinder,
                xmlNamespaceImports As XmlNamespaceImportsBinder,
                scopes As ArrayBuilder(Of IImportScope))

            Dim aliases = If(importAliases?.GetImportChainData(), ImmutableArray(Of IAliasSymbol).Empty)
            Dim [imports] = If(typesOfImportedNamespacesMembers?.GetImportChainData(), ImmutableArray(Of ImportedNamespaceOrType).Empty)
            Dim xmlNamespaces = If(xmlNamespaceImports?.GetImportChainData(), ImmutableArray(Of ImportedXmlNamespace).Empty)
            If aliases.Length = 0 AndAlso [imports].Length = 0 AndAlso xmlNamespaces.Length = 0 Then
                Return
            End If

            scopes.Add(New SimpleImportScope(aliases, ExternAliases:=ImmutableArray(Of IAliasSymbol).Empty, [imports], xmlNamespaces))
        End Sub

        Protected NotOverridable Overrides Function IsAccessibleCore(position As Integer, symbol As ISymbol) As Boolean
            Return Me.IsAccessible(position, symbol.EnsureVbSymbolOrNothing(Of Symbol)(NameOf(symbol)))
        End Function

        Protected NotOverridable Overrides Function IsEventUsableAsFieldCore(position As Integer, symbol As IEventSymbol) As Boolean
            Return False
        End Function

        Friend Overrides Sub ComputeDeclarationsInSpan(span As TextSpan, getSymbol As Boolean, builder As ArrayBuilder(Of DeclarationInfo), cancellationToken As CancellationToken)
            VisualBasicDeclarationComputer.ComputeDeclarationsInSpan(Me, span, getSymbol, builder, cancellationToken)
        End Sub

        Friend Overrides Sub ComputeDeclarationsInNode(node As SyntaxNode, associatedSymbol As ISymbol, getSymbol As Boolean, builder As ArrayBuilder(Of DeclarationInfo), cancellationToken As CancellationToken, Optional levelsToCompute As Integer? = Nothing)
            VisualBasicDeclarationComputer.ComputeDeclarationsInNode(Me, node, getSymbol, builder, cancellationToken)
        End Sub

        Protected Overrides Function GetTopmostNodeForDiagnosticAnalysis(symbol As ISymbol, declaringSyntax As SyntaxNode) As SyntaxNode
            Select Case symbol.Kind
                Case SymbolKind.Namespace
                    If TypeOf declaringSyntax Is NamespaceStatementSyntax Then
                        If declaringSyntax.Parent IsNot Nothing AndAlso TypeOf declaringSyntax.Parent Is NamespaceBlockSyntax Then
                            Return declaringSyntax.Parent
                        End If
                    End If
                Case SymbolKind.NamedType
                    If TypeOf declaringSyntax Is TypeStatementSyntax Then
                        If declaringSyntax.Parent IsNot Nothing AndAlso TypeOf declaringSyntax.Parent Is TypeBlockSyntax Then
                            Return declaringSyntax.Parent
                        End If
                    ElseIf TypeOf declaringSyntax Is EnumStatementSyntax Then
                        If declaringSyntax.Parent IsNot Nothing AndAlso TypeOf declaringSyntax.Parent Is EnumBlockSyntax Then
                            Return declaringSyntax.Parent
                        End If
                    End If
                Case SymbolKind.Method
                    If TypeOf declaringSyntax Is MethodBaseSyntax Then
                        If declaringSyntax.Parent IsNot Nothing AndAlso TypeOf declaringSyntax.Parent Is MethodBlockBaseSyntax Then
                            Return declaringSyntax.Parent
                        End If
                    End If
                Case SymbolKind.Event
                    If TypeOf declaringSyntax Is EventStatementSyntax Then
                        If declaringSyntax.Parent IsNot Nothing AndAlso TypeOf declaringSyntax.Parent Is EventBlockSyntax Then
                            Return declaringSyntax.Parent
                        End If
                    End If
                Case SymbolKind.Property
                    If TypeOf declaringSyntax Is PropertyStatementSyntax Then
                        If declaringSyntax.Parent IsNot Nothing AndAlso TypeOf declaringSyntax.Parent Is PropertyBlockSyntax Then
                            Return declaringSyntax.Parent
                        End If
                    End If
                Case SymbolKind.Field
                    Dim fieldDecl = declaringSyntax.FirstAncestorOrSelf(Of FieldDeclarationSyntax)()
                    If fieldDecl IsNot Nothing Then
                        Return fieldDecl
                    End If
            End Select

            Return declaringSyntax
        End Function

        Public NotOverridable Overrides Function GetNullableContext(position As Integer) As NullableContext
            Return NullableContext.Disabled Or NullableContext.ContextInherited
        End Function
#End Region

#Region "Logging Helpers"
        ' Following helpers are used when logging ETW events. These helpers are invoked only if we are running
        ' under an ETW listener that has requested 'verbose' logging. In other words, these helpers will never
        ' be invoked in the 'normal' case (i.e. when the code is running on user's machine and no ETW listener
        ' is involved).

        ' Note: Most of the below helpers are unused at the moment - but we would like to keep them around in
        ' case we decide we need more verbose logging in certain cases for debugging.

        Friend Function GetMessage(position As Integer) As String
            Return String.Format("{0}: at {1}", Me.SyntaxTree.FilePath, position)
        End Function

        Friend Function GetMessage(node As VisualBasicSyntaxNode) As String
            If node Is Nothing Then Return Me.SyntaxTree.FilePath
            Return String.Format("{0}: {1} ({2})", Me.SyntaxTree.FilePath, node.Kind.ToString(), node.Position)
        End Function

        Friend Function GetMessage(node As VisualBasicSyntaxNode, position As Integer) As String
            If node Is Nothing Then Return Me.SyntaxTree.FilePath
            Return String.Format("{0}: {1} ({2}) at {3}", Me.SyntaxTree.FilePath, node.Kind.ToString(), node.Position, position)
        End Function

        Friend Function GetMessage(firstStatement As StatementSyntax, lastStatement As StatementSyntax) As String
            If firstStatement Is Nothing OrElse lastStatement Is Nothing Then Return Me.SyntaxTree.FilePath
            Return String.Format("{0}: {1} to {2}", Me.SyntaxTree.FilePath, firstStatement.Position, lastStatement.EndPosition)
        End Function

        Friend Function GetMessage(expression As ExpressionSyntax, type As TypeSymbol) As String
            If expression Is Nothing OrElse type Is Nothing Then Return Me.SyntaxTree.FilePath
            Return String.Format("{0}: {1} ({2}) -> {3} {4}", Me.SyntaxTree.FilePath, expression.Kind.ToString(), expression.Position, type.TypeKind.ToString(), type.Name)
        End Function

        Friend Function GetMessage(expression As ExpressionSyntax, type As TypeSymbol, position As Integer) As String
            If expression Is Nothing OrElse type Is Nothing Then Return Me.SyntaxTree.FilePath
            Return String.Format("{0}: {1} ({2}) -> {3} {4} at {5}", Me.SyntaxTree.FilePath, expression.Kind.ToString(), expression.Position, type.TypeKind.ToString(), type.Name, position)
        End Function

        Friend Function GetMessage(expression As ExpressionSyntax, [option] As SpeculativeBindingOption, position As Integer) As String
            If expression Is Nothing Then Return Me.SyntaxTree.FilePath
            Return String.Format("{0}: {1} ({2}) at {3} ({4})", Me.SyntaxTree.FilePath, expression.Kind.ToString(), expression.Position, position, [option].ToString())
        End Function

        Friend Function GetMessage(name As String, [option] As LookupOptions, position As Integer) As String
            Return String.Format("{0}: {1} at {2} ({3})", Me.SyntaxTree.FilePath, name, position, [option].ToString())
        End Function

        Friend Function GetMessage(symbol As Symbol, position As Integer) As String
            If symbol Is Nothing Then Return Me.SyntaxTree.FilePath
            Return String.Format("{0}: {1} {2} at {3}", Me.SyntaxTree.FilePath, symbol.Kind.ToString(), symbol.Name, position)
        End Function

        Friend Function GetMessage(stage As CompilationStage) As String
            Return String.Format("{0} ({1})", Me.SyntaxTree.FilePath, stage.ToString())
        End Function
#End Region
    End Class
End Namespace
