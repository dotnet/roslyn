' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class InitializerSemanticModel
        Inherits MemberSemanticModel

        Private Sub New(root As VisualBasicSyntaxNode,
                        binder As Binder,
                        Optional containingSemanticModelOpt As SyntaxTreeSemanticModel = Nothing,
                        Optional parentSemanticModelOpt As SyntaxTreeSemanticModel = Nothing,
                        Optional speculatedPosition As Integer = 0,
                        Optional ignoreAccessibility As Boolean = False)
            MyBase.New(root, binder, containingSemanticModelOpt, parentSemanticModelOpt, speculatedPosition, ignoreAccessibility)
        End Sub

        ''' <summary>
        ''' Creates an InitializerSemanticModel that allows asking semantic questions about an initializer node.
        ''' </summary>
        Friend Shared Function Create(containingSemanticModel As SyntaxTreeSemanticModel, binder As DeclarationInitializerBinder, Optional ignoreAccessibility As Boolean = False) As InitializerSemanticModel
            Debug.Assert(containingSemanticModel IsNot Nothing)
            Return New InitializerSemanticModel(binder.Root, binder, containingSemanticModel, ignoreAccessibility:=ignoreAccessibility)
        End Function

        ''' <summary>
        ''' Creates a speculative InitializerSemanticModel that allows asking semantic questions about an initializer node that did not appear in the original source code.
        ''' </summary>
        Friend Shared Function CreateSpeculative(parentSemanticModel As SyntaxTreeSemanticModel, root As EqualsValueSyntax, binder As Binder, position As Integer) As InitializerSemanticModel
            Debug.Assert(parentSemanticModel IsNot Nothing)
            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            Return New InitializerSemanticModel(root, binder, parentSemanticModelOpt:=parentSemanticModel, speculatedPosition:=position)
        End Function

        Friend Overrides Function Bind(binder As Binder, node As SyntaxNode, diagnostics As DiagnosticBag) As BoundNode
            Debug.Assert(binder.IsSemanticModelBinder)

            Dim boundInitializer As BoundNode = Nothing

            Select Case node.Kind
                Case SyntaxKind.FieldDeclaration
                    '  get field symbol
                    If Me.MemberSymbol.Kind = SymbolKind.Field Then
                        Dim fieldSymbol = DirectCast(Me.MemberSymbol, SourceFieldSymbol)
                        boundInitializer = BindInitializer(binder, fieldSymbol.EqualsValueOrAsNewInitOpt, diagnostics)
                    Else
                        Dim propertySymbol = DirectCast(Me.MemberSymbol, SourcePropertySymbol)
                        Dim declSyntax As ModifiedIdentifierSyntax = DirectCast(propertySymbol.Syntax, ModifiedIdentifierSyntax)
                        Dim declarator = DirectCast(declSyntax.Parent, VariableDeclaratorSyntax)

                        Dim initSyntax As VisualBasicSyntaxNode = declarator.AsClause
                        If initSyntax Is Nothing OrElse initSyntax.Kind <> SyntaxKind.AsNewClause Then
                            initSyntax = declarator.Initializer
                        End If

                        boundInitializer = BindInitializer(binder, initSyntax, diagnostics)
                    End If

                Case SyntaxKind.PropertyStatement
                    '  get property symbol
                    Dim propertySymbol = DirectCast(Me.MemberSymbol, SourcePropertySymbol)
                    Dim declSyntax As PropertyStatementSyntax = DirectCast(propertySymbol.DeclarationSyntax, PropertyStatementSyntax)
                    Dim initSyntax As VisualBasicSyntaxNode = declSyntax.AsClause
                    If initSyntax Is Nothing OrElse initSyntax.Kind <> SyntaxKind.AsNewClause Then
                        initSyntax = declSyntax.Initializer
                    End If

                    boundInitializer = BindInitializer(binder, initSyntax, diagnostics)
                Case SyntaxKind.Parameter
                    Dim parameterSyntax = DirectCast(node, ParameterSyntax)
                    boundInitializer = BindInitializer(binder, parameterSyntax.Default, diagnostics)

                Case SyntaxKind.EnumMemberDeclaration
                    Dim enumSyntax = DirectCast(node, EnumMemberDeclarationSyntax)
                    boundInitializer = BindInitializer(binder, enumSyntax.Initializer, diagnostics)

                Case SyntaxKind.EqualsValue, SyntaxKind.AsNewClause
                    boundInitializer = BindInitializer(binder, node, diagnostics)
            End Select

            If boundInitializer IsNot Nothing Then
                Return boundInitializer
            Else
                Return MyBase.Bind(binder, node, diagnostics)
            End If
        End Function

        Private Iterator Function GetInitializedFieldsOrProperties(binder As Binder) As IEnumerable(Of Symbol)
            Yield Me.MemberSymbol

            For Each additionalSymbol In binder.AdditionalContainingMembers
                Yield additionalSymbol
            Next
        End Function

        Private Function BindInitializer(binder As Binder, initializer As SyntaxNode, diagnostics As DiagnosticBag) As BoundNode
            Dim boundInitializer As BoundNode = Nothing

            Select Case Me.MemberSymbol.Kind
                Case SymbolKind.Field
                    '  try to get enum field symbol
                    Dim enumFieldSymbol = TryCast(Me.MemberSymbol, SourceEnumConstantSymbol)
                    If enumFieldSymbol IsNot Nothing Then
                        Debug.Assert(initializer IsNot Nothing)
                        If initializer.Kind = SyntaxKind.EqualsValue Then
                            Dim enumSymbol = DirectCast(Me.MemberSymbol, SourceEnumConstantSymbol)
                            boundInitializer = binder.BindFieldAndEnumConstantInitializer(enumSymbol, DirectCast(initializer, EqualsValueSyntax), isEnum:=True, diagnostics:=diagnostics, constValue:=Nothing)
                        End If
                    Else
                        '  get field symbol
                        Dim fieldSymbol = DirectCast(Me.MemberSymbol, SourceFieldSymbol)
                        Dim boundInitializers = ArrayBuilder(Of BoundInitializer).GetInstance
                        If initializer IsNot Nothing Then
                            ' bind const and non const field initializers the same to get a bound expression back and not a constant value.
                            Dim fields = ImmutableArray.CreateRange(GetInitializedFieldsOrProperties(binder).Cast(Of FieldSymbol))
                            binder.BindFieldInitializer(fields, initializer, boundInitializers, diagnostics, bindingForSemanticModel:=True)
                        Else
                            binder.BindArrayFieldImplicitInitializer(fieldSymbol, boundInitializers, diagnostics)
                        End If

                        boundInitializer = boundInitializers.First
                        boundInitializers.Free()
                    End If

                    Dim expressionInitializer = TryCast(boundInitializer, BoundExpression)
                    If expressionInitializer IsNot Nothing Then
                        Return New BoundFieldInitializer(initializer, ImmutableArray.Create(DirectCast(Me.MemberSymbol, FieldSymbol)), Nothing, expressionInitializer)
                    End If

                Case SymbolKind.Property
                    '  get property symbol
                    Dim propertySymbols = ImmutableArray.CreateRange(GetInitializedFieldsOrProperties(binder).Cast(Of PropertySymbol))
                    Dim boundInitializers = ArrayBuilder(Of BoundInitializer).GetInstance
                    binder.BindPropertyInitializer(propertySymbols, initializer, boundInitializers, diagnostics)
                    boundInitializer = boundInitializers.First
                    boundInitializers.Free()

                    Dim expressionInitializer = TryCast(boundInitializer, BoundExpression)
                    If expressionInitializer IsNot Nothing Then
                        Return New BoundPropertyInitializer(initializer, propertySymbols, Nothing, expressionInitializer)
                    End If

                Case SymbolKind.Parameter
                    Debug.Assert(initializer IsNot Nothing)
                    If initializer.Kind = SyntaxKind.EqualsValue Then
                        Dim parameterSymbol = DirectCast(Me.RootBinder.ContainingMember, SourceComplexParameterSymbol)
                        boundInitializer = binder.BindParameterDefaultValue(parameterSymbol.Type, DirectCast(initializer, EqualsValueSyntax), diagnostics, constValue:=Nothing)

                        Dim expressionInitializer = TryCast(boundInitializer, BoundExpression)
                        If expressionInitializer IsNot Nothing Then
                            Return New BoundParameterEqualsValue(initializer, parameterSymbol, expressionInitializer)
                        End If
                    End If

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(Me.MemberSymbol.Kind)
            End Select

            Return boundInitializer
        End Function

        Friend Overrides Function GetBoundRoot() As BoundNode
            ' In initializer the root bound node is sometimes not associated with Me.Root. Get the
            ' syntax that the root bound node should be associated with.
            Dim rootSyntax = Me.Root

            If rootSyntax.Kind = SyntaxKind.FieldDeclaration Then
                Dim fieldSymbol = TryCast(Me.RootBinder.ContainingMember, SourceFieldSymbol)
                If fieldSymbol IsNot Nothing Then
                    rootSyntax = If(fieldSymbol.EqualsValueOrAsNewInitOpt, fieldSymbol.Syntax)
                Else
                    ' 'WithEvents x As Y = ...'
                    Dim propertySymbol = TryCast(Me.RootBinder.ContainingMember, SourcePropertySymbol)
                    Debug.Assert(rootSyntax Is propertySymbol.DeclarationSyntax)

                    Dim propertyNameId = DirectCast(propertySymbol.Syntax, ModifiedIdentifierSyntax) ' serves as an assert
                    Dim declarator = DirectCast(propertyNameId.Parent, VariableDeclaratorSyntax) ' serves as an assert

                    Dim initSyntax As VisualBasicSyntaxNode = declarator.AsClause
                    If initSyntax Is Nothing OrElse initSyntax.Kind <> SyntaxKind.AsNewClause Then
                        initSyntax = declarator.Initializer
                    End If
                    If initSyntax IsNot Nothing Then
                        rootSyntax = initSyntax
                    End If
                End If
            ElseIf rootSyntax.Kind = SyntaxKind.PropertyStatement Then
                Dim declSyntax As PropertyStatementSyntax = DirectCast(rootSyntax, PropertyStatementSyntax)
                Dim initSyntax As VisualBasicSyntaxNode = declSyntax.AsClause
                If initSyntax Is Nothing OrElse initSyntax.Kind <> SyntaxKind.AsNewClause Then
                    initSyntax = declSyntax.Initializer
                End If
                If initSyntax IsNot Nothing Then
                    rootSyntax = initSyntax
                End If
            End If

            'TODO - Do parameters need to do anything here?

            Return GetUpperBoundNode(rootSyntax)
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Dim binder = Me.GetEnclosingBinder(position)
            If binder Is Nothing Then
                speculativeModel = Nothing
                Return False
            End If

            ' wrap the binder with a Speculative binder
            binder = SpeculativeBinder.Create(binder)

            speculativeModel = CreateSpeculative(parentModel, initializer, binder, position)
            Return True
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            speculativeModel = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, body As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            speculativeModel = Nothing
            Return False
        End Function
    End Class

End Namespace
