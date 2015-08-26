' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue

    Friend NotInheritable Class TopSyntaxComparer
        Inherits SyntaxComparer

        Friend Shared ReadOnly Instance As TopSyntaxComparer = New TopSyntaxComparer()

        Private Sub New()
        End Sub

#Region "Tree Traversal"

        Protected Overrides Function TryGetParent(node As SyntaxNode, ByRef parent As SyntaxNode) As Boolean
            Dim parentNode = node.Parent
            parent = parentNode
            Return parentNode IsNot Nothing
        End Function

        Protected Overrides Function GetChildren(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Debug.Assert(GetLabel(node) <> IgnoredNode)
            Return If(HasChildren(node), EnumerateChildren(node), Nothing)
        End Function

        Private Iterator Function EnumerateChildren(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            For Each child In node.ChildNodesAndTokens()
                Dim childNode = child.AsNode()
                If childNode IsNot Nothing AndAlso GetLabel(childNode) <> IgnoredNode Then
                    Yield childNode
                End If
            Next
        End Function

        Protected Overrides Iterator Function GetDescendants(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            For Each descendant In node.DescendantNodesAndTokens(
                descendIntoChildren:=AddressOf HasChildren,
                descendIntoTrivia:=False)

                Dim descendantNode = descendant.AsNode()
                If descendantNode IsNot Nothing AndAlso GetLabel(descendantNode) <> IgnoredNode Then
                    Yield descendantNode
                End If
            Next
        End Function

        Private Shared Function HasChildren(node As SyntaxNode) As Boolean
            ' Leaves are labeled statements that don't have a labeled child.
            ' We also return true for non-labeled statements.
            Dim isLeaf As Boolean
            Dim label As Label = Classify(node.Kind, isLeaf, ignoreVariableDeclarations:=False)
            Debug.Assert(label <> Label.Ignored OrElse isLeaf)
            Return Not isLeaf
        End Function

#End Region

#Region "Labels"

        ' Assumptions:
        ' - Each listed label corresponds to one or more syntax kinds.
        ' - Nodes with same labels might produce Update edits, nodes with different labels don't. 
        ' - If IsTiedToParent(label) is true for a label then all its possible parent labels must precede the label.
        '   (i.e. both MethodDeclaration and TypeDeclaration must precede TypeParameter label).
        ' - All descendants of a node whose kind is listed here will be ignored regardless of their labels
        Friend Enum Label
            CompilationUnit
            [Option]                         ' tied to parent
            Import                           ' tied to parent
            Attributes                       ' tied to parent

            NamespaceDeclaration
            TypeDeclaration
            EnumDeclaration
            DelegateDeclaration
            FieldDeclaration                 ' tied to parent
            FieldVariableDeclarator          ' tied to parent

            PInvokeDeclaration               ' tied to parent
            MethodDeclaration                ' tied to parent
            ConstructorDeclaration           ' tied to parent
            OperatorDeclaration              ' tied to parent
            PropertyDeclaration              ' tied to parent
            CustomEventDeclaration           ' tied to parent
            EnumMemberDeclaration            ' tied to parent
            AccessorDeclaration              ' tied to parent

            ' Opening statement of a type, method, operator, constructor, property, and accessor.
            ' We need to represent this node in the graph since attributes, (generic) parameters are its children.
            ' However, we don't need to have a specialized label for each type of declaration statement since 
            ' they are tied to the parent and each parent has a single declaration statement.
            DeclarationStatement             ' tied to parent

            ' Event statement is either a child of a custom event or a stand-alone event field declaration.
            EventStatement                   ' tied to parent

            TypeParameterList                ' tied to parent
            TypeParameter                    ' tied to parent
            TypeParameterConstraintClause    ' tied to parent
            TypeConstraint                   ' tied to parent
            TypeKindConstraint               ' tied to parent
            NewConstraint                    ' tied to parent

            ParameterList                    ' tied to parent
            Parameter                        ' tied to parent
            FieldOrParameterName             ' tied to grandparent (FieldDeclaration or ParameterList)
            SimpleAsClause                   ' tied to parent

            AttributeList                    ' tied to parent
            Attribute                        ' tied to parent

            Count
            Ignored = IgnoredNode
        End Enum

        ''' <summary>
        ''' Return true if it is desirable to report two edits (delete and insert) rather than a move edit
        ''' when the node changes its parent.
        ''' </summary>
        Private Overloads Shared Function TiedToAncestor(label As Label) As Integer
            Select Case label
                Case Label.Option,
                     Label.Import,
                     Label.Attributes,
                     Label.FieldDeclaration,
                     Label.FieldVariableDeclarator,
                     Label.PInvokeDeclaration,
                     Label.MethodDeclaration,
                     Label.OperatorDeclaration,
                     Label.ConstructorDeclaration,
                     Label.PropertyDeclaration,
                     Label.CustomEventDeclaration,
                     Label.EnumMemberDeclaration,
                     Label.AccessorDeclaration,
                     Label.DeclarationStatement,
                     Label.EventStatement,
                     Label.TypeParameterList,
                     Label.TypeParameter,
                     Label.TypeParameterConstraintClause,
                     Label.TypeConstraint,
                     Label.TypeKindConstraint,
                     Label.NewConstraint,
                     Label.ParameterList,
                     Label.Parameter,
                     Label.SimpleAsClause,
                     Label.AttributeList,
                     Label.Attribute
                    Return 1

                Case Label.FieldOrParameterName
                    Return 2 ' FieldDeclaration or ParameterList

                Case Else
                    Return 0
            End Select

            Throw New NotImplementedException()
        End Function

        ' internal for testing
        Friend Shared Function Classify(kind As SyntaxKind, ByRef isLeaf As Boolean, ignoreVariableDeclarations As Boolean) As Label
            Select Case kind
                Case SyntaxKind.CompilationUnit
                    isLeaf = False
                    Return Label.CompilationUnit

                Case SyntaxKind.OptionStatement
                    isLeaf = True
                    Return Label.Option

                Case SyntaxKind.ImportsStatement
                    isLeaf = True
                    Return Label.Import

                Case SyntaxKind.AttributesStatement
                    isLeaf = False
                    Return Label.Attributes

                Case SyntaxKind.NamespaceBlock
                    isLeaf = False
                    Return Label.NamespaceDeclaration

                Case SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock
                    isLeaf = False
                    Return Label.TypeDeclaration

                Case SyntaxKind.EnumBlock
                    isLeaf = False
                    Return Label.EnumDeclaration

                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    isLeaf = False
                    Return Label.DelegateDeclaration

                Case SyntaxKind.FieldDeclaration
                    isLeaf = False
                    Return Label.FieldDeclaration

                Case SyntaxKind.VariableDeclarator
                    isLeaf = ignoreVariableDeclarations
                    Return If(ignoreVariableDeclarations, Label.Ignored, Label.FieldVariableDeclarator)

                Case SyntaxKind.ModifiedIdentifier
                    isLeaf = True
                    Return If(ignoreVariableDeclarations, Label.Ignored, Label.FieldOrParameterName)

                Case SyntaxKind.SimpleAsClause
                    isLeaf = ignoreVariableDeclarations
                    Return If(ignoreVariableDeclarations, Label.Ignored, Label.SimpleAsClause)

                Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock
                    isLeaf = False
                    Return Label.MethodDeclaration

                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    isLeaf = False
                    Return Label.PInvokeDeclaration

                Case SyntaxKind.ConstructorBlock
                    isLeaf = False
                    Return Label.ConstructorDeclaration

                Case SyntaxKind.OperatorBlock
                    isLeaf = False
                    Return Label.OperatorDeclaration

                Case SyntaxKind.PropertyBlock
                    isLeaf = False
                    Return Label.PropertyDeclaration

                Case SyntaxKind.EventBlock
                    isLeaf = False
                    Return Label.CustomEventDeclaration

                Case SyntaxKind.EnumMemberDeclaration
                    isLeaf = False
                    Return Label.EnumMemberDeclaration

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    isLeaf = False
                    Return Label.AccessorDeclaration

                Case SyntaxKind.ClassStatement,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.ModuleStatement,
                     SyntaxKind.NamespaceStatement,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.PropertyStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    isLeaf = False
                    Return Label.DeclarationStatement

                Case SyntaxKind.EventStatement
                    isLeaf = False
                    Return Label.EventStatement

                Case SyntaxKind.TypeParameterList
                    isLeaf = False
                    Return Label.TypeParameterList

                Case SyntaxKind.TypeParameter
                    isLeaf = False
                    Return Label.TypeParameter

                Case SyntaxKind.TypeParameterSingleConstraintClause,
                     SyntaxKind.TypeParameterMultipleConstraintClause
                    isLeaf = False
                    Return Label.TypeParameterConstraintClause

                Case SyntaxKind.StructureConstraint,
                     SyntaxKind.ClassConstraint
                    isLeaf = True
                    Return Label.TypeKindConstraint

                Case SyntaxKind.NewConstraint
                    isLeaf = True
                    Return Label.NewConstraint

                Case SyntaxKind.TypeConstraint
                    isLeaf = True
                    Return Label.TypeConstraint

                Case SyntaxKind.ParameterList
                    isLeaf = False
                    Return Label.ParameterList

                Case SyntaxKind.Parameter
                    isLeaf = False
                    Return Label.Parameter

                Case SyntaxKind.AttributeList
                    isLeaf = False
                    Return Label.AttributeList

                Case SyntaxKind.Attribute
                    isLeaf = True
                    Return Label.Attribute

                Case Else
                    isLeaf = True
                    Return Label.Ignored
            End Select
        End Function

        Protected Overrides Function GetLabel(node As SyntaxNode) As Integer
            Return GetLabelImpl(node)
        End Function

        Friend Shared Function GetLabelImpl(node As SyntaxNode) As Label
            Dim isLeaf As Boolean
            Return Classify(node.Kind, isLeaf, ignoreVariableDeclarations:=False)
        End Function

        ' internal for testing
        Friend Shared Function HasLabel(kind As SyntaxKind, ignoreVariableDeclarations As Boolean) As Boolean
            Dim isLeaf As Boolean
            Return Classify(kind, isLeaf, ignoreVariableDeclarations) <> Label.Ignored
        End Function

        Protected Overrides ReadOnly Property LabelCount As Integer
            Get
                Return Label.Count
            End Get
        End Property

        Protected Overrides Function TiedToAncestor(label As Integer) As Integer
            Return TiedToAncestor(CType(label, Label))
        End Function

#End Region

#Region "Comparisons"

        Public Overrides Function ValuesEqual(left As SyntaxNode, right As SyntaxNode) As Boolean
            Dim ignoreChildFunction As Func(Of SyntaxKind, Boolean)

            Select Case left.Kind()
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.PropertyBlock,
                     SyntaxKind.EventBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    ' When comparing a block containing method body statements we need to not ignore 
                    ' VariableDeclaration, ModifiedIdentifier, and AsClause children.
                    ' But when comparing field definitions we should ignore VariableDeclaration children.
                    ignoreChildFunction = Function(childKind) HasLabel(childKind, ignoreVariableDeclarations:=True)

                Case Else
                    If HasChildren(left) Then
                        ignoreChildFunction = Function(childKind) HasLabel(childKind, ignoreVariableDeclarations:=False)
                    Else
                        ignoreChildFunction = Nothing
                    End If
            End Select

            Return SyntaxFactory.AreEquivalent(left, right, ignoreChildFunction)
        End Function

        Protected Overrides Function TryComputeWeightedDistance(leftNode As SyntaxNode, rightNode As SyntaxNode, ByRef distance As Double) As Boolean
            If leftNode.IsKind(SyntaxKind.VariableDeclarator) Then
                Dim leftIdentifiers = DirectCast(leftNode, VariableDeclaratorSyntax).Names.Select(Function(n) n.Identifier)
                Dim rightIdentifiers = DirectCast(rightNode, VariableDeclaratorSyntax).Names.Select(Function(n) n.Identifier)
                distance = ComputeDistance(leftIdentifiers, rightIdentifiers)
                Return True
            End If

            Dim leftName As SyntaxNodeOrToken? = TryGetName(leftNode)
            Dim rightName As SyntaxNodeOrToken? = TryGetName(rightNode)

            If leftName.HasValue AndAlso rightName.HasValue Then
                distance = ComputeDistance(leftName.Value, rightName.Value)
                Return True
            End If

            distance = 0
            Return False
        End Function

        Private Shared Function TryGetName(node As SyntaxNode) As SyntaxNodeOrToken?
            Select Case node.Kind()
                Case SyntaxKind.OptionStatement
                    Return DirectCast(node, OptionStatementSyntax).OptionKeyword

                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(node, NamespaceBlockSyntax).NamespaceStatement.Name

                Case SyntaxKind.ClassBlock,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock
                    Return DirectCast(node, TypeBlockSyntax).BlockStatement.Identifier

                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement.Identifier

                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).Identifier

                Case SyntaxKind.ModifiedIdentifier
                    Return DirectCast(node, ModifiedIdentifierSyntax).Identifier

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock
                    Return DirectCast(node, MethodBlockSyntax).SubOrFunctionStatement.Identifier

                Case SyntaxKind.SubStatement,     ' interface methods
                     SyntaxKind.FunctionStatement
                    Return DirectCast(node, MethodStatementSyntax).Identifier

                Case SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(node, DeclareStatementSyntax).Identifier

                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(node, ConstructorBlockSyntax).SubNewStatement.NewKeyword

                Case SyntaxKind.OperatorBlock
                    Return DirectCast(node, OperatorBlockSyntax).OperatorStatement.OperatorToken

                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement.Identifier

                Case SyntaxKind.PropertyStatement ' interface properties
                    Return DirectCast(node, PropertyStatementSyntax).Identifier

                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement.Identifier

                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(node, EnumMemberDeclarationSyntax).Identifier

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(node, AccessorBlockSyntax).BlockStatement.DeclarationKeyword

                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).Identifier

                Case SyntaxKind.TypeParameter
                    Return DirectCast(node, TypeParameterSyntax).Identifier

                Case SyntaxKind.StructureConstraint,
                     SyntaxKind.ClassConstraint,
                     SyntaxKind.NewConstraint
                    Return DirectCast(node, SpecialConstraintSyntax).ConstraintKeyword

                Case SyntaxKind.Parameter
                    Return DirectCast(node, ParameterSyntax).Identifier.Identifier

                Case SyntaxKind.Attribute
                    Return DirectCast(node, AttributeSyntax).Name

                Case Else
                    Return Nothing
            End Select
        End Function
#End Region
    End Class
End Namespace
