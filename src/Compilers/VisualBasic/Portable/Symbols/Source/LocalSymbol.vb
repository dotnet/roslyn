' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a local variable (typically inside a method body). This could also be a local variable implicitly
    ''' declared by a For, Using, etc. When used as a temporary variable, its container can also be a Field or Property Symbol.
    ''' </summary>
    Friend MustInherit Class LocalSymbol
        Inherits Symbol
        Implements ILocalSymbol, ILocalSymbolInternal

        Friend Shared ReadOnly UseBeforeDeclarationResultType As ErrorTypeSymbol = New ErrorTypeSymbol()

        Private ReadOnly _container As Symbol ' the method, field or property that contains the declaration of this variable

        Private _lazyType As TypeSymbol

        ''' <summary>
        '''  Create a local symbol from a local variable declaration.
        ''' </summary>
        Friend Shared Function Create(container As Symbol,
            binder As Binder,
            declaringIdentifier As SyntaxToken,
            modifiedIdentifierOpt As ModifiedIdentifierSyntax,
            asClauseOpt As AsClauseSyntax,
            initializerOpt As EqualsValueSyntax,
            declarationKind As LocalDeclarationKind) As LocalSymbol

            Return New VariableLocalSymbol(container, binder, declaringIdentifier, modifiedIdentifierOpt, asClauseOpt, initializerOpt, declarationKind)
        End Function

        ''' <summary>
        ''' Create a local symbol associated with an identifier token.
        ''' </summary>
        Friend Shared Function Create(container As Symbol,
            binder As Binder,
            declaringIdentifier As SyntaxToken,
            declarationKind As LocalDeclarationKind,
            type As TypeSymbol) As LocalSymbol

            Return New SourceLocalSymbol(container, binder, declaringIdentifier, declarationKind, type)
        End Function

        ''' <summary>
        ''' Create a local symbol associated with an identifier token and a different name.
        ''' Used for WinRT event handler return value variable).
        ''' </summary>
        Friend Shared Function Create(container As Symbol,
            binder As Binder,
            declaringIdentifier As SyntaxToken,
            declarationKind As LocalDeclarationKind,
            type As TypeSymbol,
            name As String) As LocalSymbol

            Return New SourceLocalSymbolWithNonstandardName(container, binder, declaringIdentifier, declarationKind, type, name)
        End Function

        ''' <summary>
        ''' Create a local symbol with substituted type.
        ''' </summary>
        Friend Shared Function Create(originalVariable As LocalSymbol, type As TypeSymbol) As LocalSymbol
            Return New TypeSubstitutedLocalSymbol(originalVariable, type)
        End Function

        ''' <summary>
        ''' Create an inferred local symbol from a For from-to statement.
        ''' </summary>
        Friend Shared Function CreateInferredForFromTo(container As Symbol,
            binder As Binder,
            declaringIdentifier As SyntaxToken,
            fromValue As ExpressionSyntax,
            toValue As ExpressionSyntax,
            stepClauseOpt As ForStepClauseSyntax) As LocalSymbol

            Return New InferredForFromToLocalSymbol(container, binder, declaringIdentifier, fromValue, toValue, stepClauseOpt)
        End Function

        ''' <summary>
        ''' Create an inferred local symbol from a For-each statement.
        ''' </summary>
        Friend Shared Function CreateInferredForEach(container As Symbol,
            binder As Binder,
            declaringIdentifier As SyntaxToken,
            expression As ExpressionSyntax) As LocalSymbol

            Return New InferredForEachLocalSymbol(container, binder, declaringIdentifier, expression)
        End Function

        ''' <summary>
        ''' Create a local variable symbol. Note: this does not insert it automatically into a
        ''' local binder so that it can be found by lookup.
        ''' </summary>
        Friend Sub New(container As Symbol, type As TypeSymbol)

            Debug.Assert(container IsNot Nothing, "local must belong to a method, field or property")
            Debug.Assert(container.Kind = SymbolKind.Method OrElse
                         container.Kind = SymbolKind.Field OrElse
                         container.Kind = SymbolKind.Property,
                         "Unsupported container. Must be method, field or property.")

            _container = container
            _lazyType = type
        End Sub

        Friend Overridable ReadOnly Property IsImportedFromMetadata As Boolean
            Get
                Return False
            End Get
        End Property

        Friend MustOverride ReadOnly Property DeclarationKind As LocalDeclarationKind
        Friend MustOverride ReadOnly Property SynthesizedKind As SynthesizedLocalKind

        Public Overridable ReadOnly Property Type As TypeSymbol
            Get
                If _lazyType Is Nothing Then
                    Interlocked.CompareExchange(_lazyType, ComputeType(), Nothing)
                End If

                Return _lazyType
            End Get
        End Property

        Friend ReadOnly Property ConstHasType As Boolean
            Get
                Debug.Assert(Me.IsConst)
                Return Me._lazyType IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this local is a ReadOnly local. Compiler has a concept of ReadOnly locals.
        ''' </summary>
        Friend Overridable ReadOnly Property IsReadOnly As Boolean
            Get
                ' locals declared in the resource list of a using statement are considered to be read only. 
                Return IsUsing OrElse IsConst
            End Get
        End Property

        ' Set the type of this variable. This is used by the expression binder to set the type of the variable.
        ' If the type has already been computed, it should have been computed to be the same type.
        Public Sub SetType(type As TypeSymbol)
            If _lazyType Is Nothing Then
                Interlocked.CompareExchange(_lazyType, type, Nothing)
                Debug.Assert((Me.IsFunctionValue AndAlso _container.Kind = SymbolKind.Method AndAlso DirectCast(_container, MethodSymbol).MethodKind = MethodKind.LambdaMethod) OrElse type.Equals(ComputeType()))
            Else
                Debug.Assert(type.Equals(_lazyType), "Attempted to set a local variable with a different type")
            End If
        End Sub

        ' Compute the type of this variable.
        Friend Overridable Function ComputeType(Optional containingBinder As Binder = Nothing) As TypeSymbol
            Debug.Assert(_lazyType IsNot Nothing)
            Return _lazyType
        End Function

        Public MustOverride Overrides ReadOnly Property Name As String

        ' Get the identifier token that defined this local symbol. This is useful for robustly checking
        ' if a local symbol actually matches a particular definition, even in the presence of duplicates.
        Friend MustOverride ReadOnly Property IdentifierToken As SyntaxToken

        ''' <summary>
        ''' Returns the syntax node that declares the variable.
        ''' </summary>
        ''' <remarks>
        ''' All user-defined and long-lived synthesized variables must return a reference to a node that is 
        ''' tracked by the EnC diffing algorithm. For example, for <see cref="LocalDeclarationKind.Catch"/> variable
        ''' the declarator is the <see cref="CatchStatementSyntax"/> node, not the <see cref="IdentifierNameSyntax"/>
        ''' that immediately contains the variable.
        ''' 
        ''' The location of the declarator is used to calculate <see cref="LocalDebugId.SyntaxOffset"/> during emit.
        ''' </remarks>
        Friend MustOverride Function GetDeclaratorSyntax() As SyntaxNode

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Local
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(Me.IdentifierLocation)
            End Get
        End Property

        Friend MustOverride ReadOnly Property IdentifierLocation As Location

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IsUsing As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.Using
            End Get
        End Property

        Public ReadOnly Property IsCatch As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.Catch
            End Get
        End Property

        Public ReadOnly Property IsConst As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.Constant
            End Get
        End Property

        Private ReadOnly Property ILocalSymbol_IsFixed As Boolean Implements ILocalSymbol.IsFixed
            Get
                Return False
            End Get
        End Property

        Friend Overridable ReadOnly Property CanScheduleToStack As Boolean
            Get
                ' cannot schedule constants and catch variables
                ' in theory catch vars could be scheduled, but are not worth the trouble.
                Return Not IsConst AndAlso Not IsCatch
            End Get
        End Property

        Public ReadOnly Property IsStatic As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.Static
            End Get
        End Property

        Public ReadOnly Property IsFor As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.For
            End Get
        End Property

        Public ReadOnly Property IsForEach As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.ForEach
            End Get
        End Property

        Public ReadOnly Property IsRef As Boolean Implements ILocalSymbol.IsRef
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property RefKind As RefKind Implements ILocalSymbol.RefKind
            Get
                Return RefKind.None
            End Get
        End Property

        Private ReadOnly Property ILocalSymbol_ScopedKind As ScopedKind Implements ILocalSymbol.ScopedKind
            Get
                Return ScopedKind.None
            End Get
        End Property

        Public MustOverride ReadOnly Property IsFunctionValue As Boolean Implements ILocalSymbol.IsFunctionValue

        Friend ReadOnly Property IsCompilerGenerated As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.None
            End Get
        End Property

        ''' <summary>
        ''' Was this local variable implicitly declared, because Option Explicit Off
        ''' was in effect, and no other symbol was found with this name.
        ''' </summary>
        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.ImplicitVariable
            End Get
        End Property

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitLocal(Me, arg)
        End Function

        Friend Overridable ReadOnly Property IsByRef As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overridable ReadOnly Property IsPinned As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property HasConstantValue As Boolean Implements ILocalSymbol.HasConstantValue
            Get
                If Not Me.IsConst Then
                    Return Nothing
                End If

                Return GetConstantValue(Nothing) IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property ConstantValue As Object Implements ILocalSymbol.ConstantValue
            Get
                If Not Me.IsConst Then
                    Return Nothing
                End If

                Dim constant As ConstantValue = Me.GetConstantValue(Nothing)
                Return If(constant Is Nothing, Nothing, constant.Value)
            End Get
        End Property

        Friend Overridable Function GetConstantValueDiagnostics(binder As Binder) As ReadOnlyBindingDiagnostic(Of AssemblySymbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overridable Function GetConstantExpression(binder As Binder) As BoundExpression
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overridable Function GetConstantValue(binder As Binder) As ConstantValue
            Return Nothing
        End Function

        Friend Overridable ReadOnly Property HasInferredType As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        ''' This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

#Region "ILocalSymbol"

        Private ReadOnly Property ILocalSymbol_Type As ITypeSymbol Implements ILocalSymbol.Type
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property ILocalSymbol_NullableAnnotation As NullableAnnotation Implements ILocalSymbol.NullableAnnotation
            Get
                Return NullableAnnotation.None
            End Get
        End Property

        Private ReadOnly Property ILocalSymbol_IsConst As Boolean Implements ILocalSymbol.IsConst
            Get
                Return Me.IsConst
            End Get
        End Property

        Private ReadOnly Property ILocalSymbol_IsForEach As Boolean Implements ILocalSymbol.IsForEach
            Get
                Return Me.IsForEach
            End Get
        End Property

        Private ReadOnly Property ILocalSymbol_IsUsing As Boolean Implements ILocalSymbol.IsUsing
            Get
                Return Me.IsUsing
            End Get
        End Property

#End Region

#Region "ISymbol"

        Protected Overrides ReadOnly Property ISymbol_IsStatic As Boolean
            Get
                Return Me.IsStatic
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitLocal(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitLocal(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLocal(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitLocal(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitLocal(Me)
        End Function

#End Region

#Region "ILocalSymbolInternal"

        Private ReadOnly Property ILocalSymbolInternal_IsImportedFromMetadata As Boolean Implements ILocalSymbolInternal.IsImportedFromMetadata
            Get
                Return Me.IsImportedFromMetadata
            End Get
        End Property

        Private ReadOnly Property ILocalSymbolInternal_SynthesizedKind As SynthesizedLocalKind Implements ILocalSymbolInternal.SynthesizedKind
            Get
                Return Me.SynthesizedKind
            End Get
        End Property

        Private Function ILocalSymbolInternal_GetDeclaratorSyntax() As SyntaxNode Implements ILocalSymbolInternal.GetDeclaratorSyntax
            Return Me.GetDeclaratorSyntax()
        End Function

#End Region

#Region "SourceLocalSymbol"

        ''' <summary>
        ''' Base class for any local symbol that can be referenced in source, might be implicitly declared.
        ''' </summary>
        Private Class SourceLocalSymbol
            Inherits LocalSymbol

            Private ReadOnly _declarationKind As LocalDeclarationKind
            Protected ReadOnly _identifierToken As SyntaxToken
            Protected ReadOnly _binder As Binder

            Public Sub New(containingSymbol As Symbol,
                           binder As Binder,
                           identifierToken As SyntaxToken,
                           declarationKind As LocalDeclarationKind,
                           type As TypeSymbol)
                MyBase.New(containingSymbol, type)

                Debug.Assert(identifierToken.Kind <> SyntaxKind.None)
                Debug.Assert(declarationKind <> LocalDeclarationKind.None)
                Debug.Assert(binder IsNot Nothing)

                _identifierToken = identifierToken
                _declarationKind = declarationKind
                _binder = binder
            End Sub

            Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
                Get
                    Return _declarationKind
                End Get
            End Property

            Public Overrides ReadOnly Property IsFunctionValue As Boolean
                Get
                    Return _declarationKind = LocalDeclarationKind.FunctionValue
                End Get
            End Property

            Friend Overrides ReadOnly Property SynthesizedKind As SynthesizedLocalKind
                Get
                    Return SynthesizedLocalKind.UserDefined
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _identifierToken.GetIdentifierText()
                End Get
            End Property

            Friend Overrides Function GetDeclaratorSyntax() As SyntaxNode
                Dim node As SyntaxNode

                Select Case Me.DeclarationKind
                    Case LocalDeclarationKind.Variable,
                         LocalDeclarationKind.Constant,
                         LocalDeclarationKind.Using,
                         LocalDeclarationKind.Static
                        node = _identifierToken.Parent
                        Debug.Assert(TypeOf node Is ModifiedIdentifierSyntax)

                    Case LocalDeclarationKind.ImplicitVariable
                        node = _identifierToken.Parent
                        Debug.Assert(TypeOf node Is IdentifierNameSyntax)

                    Case LocalDeclarationKind.FunctionValue
                        node = _identifierToken.Parent

                        If node.IsKind(SyntaxKind.PropertyStatement) Then
                            Dim propertyBlock = DirectCast(node.Parent, PropertyBlockSyntax)
                            Return propertyBlock.Accessors.Where(Function(a) a.IsKind(SyntaxKind.GetAccessorBlock)).Single().BlockStatement
                        ElseIf node.IsKind(SyntaxKind.EventStatement) Then
                            Dim eventBlock = DirectCast(node.Parent, EventBlockSyntax)
                            Return eventBlock.Accessors.Where(Function(a) a.IsKind(SyntaxKind.AddHandlerAccessorBlock)).Single().BlockStatement
                        End If

                        Debug.Assert(node.IsKind(SyntaxKind.FunctionStatement))

                    Case LocalDeclarationKind.Catch
                        node = _identifierToken.Parent.Parent
                        Debug.Assert(TypeOf node Is CatchStatementSyntax)

                    Case LocalDeclarationKind.For
                        node = _identifierToken.Parent
                        If Not node.IsKind(SyntaxKind.ModifiedIdentifier) Then
                            node = node.Parent
                            Debug.Assert(node.IsKind(SyntaxKind.ForStatement))
                        End If

                    Case LocalDeclarationKind.ForEach
                        node = _identifierToken.Parent
                        If Not node.IsKind(SyntaxKind.ModifiedIdentifier) Then
                            node = node.Parent
                            Debug.Assert(node.IsKind(SyntaxKind.ForEachStatement))
                        End If

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(Me.DeclarationKind)
                End Select

                Return node
            End Function

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    If Me.DeclarationKind = LocalDeclarationKind.FunctionValue Then
                        Return ImmutableArray(Of SyntaxReference).Empty
                    End If

                    Return ImmutableArray.Create(_identifierToken.Parent.GetReference())
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property IdentifierLocation As Location
                Get
                    Return _identifierToken.GetLocation()
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property IdentifierToken As SyntaxToken
                Get
                    Return _identifierToken
                End Get
            End Property

            Friend Overrides Function ComputeType(Optional containingBinder As Binder = Nothing) As TypeSymbol
                containingBinder = If(containingBinder, _binder)
                Dim type As TypeSymbol = ComputeTypeInternal(If(containingBinder, _binder))
                Return type
            End Function

            Friend Overridable Function ComputeTypeInternal(containingBinder As Binder) As TypeSymbol
                Debug.Assert(_lazyType IsNot Nothing)
                Return _lazyType
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, SourceLocalSymbol)

                Return other IsNot Nothing AndAlso other._identifierToken.Equals(Me._identifierToken) AndAlso Equals(other._container, Me._container) AndAlso String.Equals(other.Name, Me.Name)
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(_identifierToken.GetHashCode(), Me._container.GetHashCode())
            End Function
        End Class
#End Region

#Region "SourceLocalSymbolWithNonstandardName"
        ''' <summary>
        ''' Class for a local symbol that has a different name than the identifier token.
        ''' In this case the real name is returned by the name property and the "VB User visible name" can be
        ''' obtained by accessing the IdentifierToken.
        ''' </summary>
        Private NotInheritable Class SourceLocalSymbolWithNonstandardName
            Inherits SourceLocalSymbol

            Private ReadOnly _name As String

            ''' <summary>
            ''' Create a local variable symbol. Note: this does not insert it automatically into a
            ''' local binder so that it can be found by lookup.
            ''' </summary>
            Public Sub New(container As Symbol,
                           binder As Binder,
                           declaringIdentifier As SyntaxToken,
                           declarationKind As LocalDeclarationKind,
                           type As TypeSymbol,
                           name As String)
                MyBase.New(container, binder, declaringIdentifier, declarationKind, type)

                Debug.Assert(name IsNot Nothing)
                Debug.Assert(type IsNot Nothing)
                _name = name
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _name
                End Get
            End Property
        End Class

#End Region

#Region "InferredForEachLocalSymbol"
        ''' <summary>
        ''' A local symbol created by a for-each statement when Option Infer is on.
        ''' </summary>
        ''' <remarks></remarks>
        Private NotInheritable Class InferredForEachLocalSymbol
            Inherits SourceLocalSymbol

            Private ReadOnly _collectionExpressionSyntax As ExpressionSyntax

            ''' <summary>
            ''' Create a local variable symbol. Note: this does not insert it automatically into a
            ''' local binder so that it can be found by lookup.
            ''' </summary>
            Public Sub New(container As Symbol,
                           binder As Binder,
                           declaringIdentifier As SyntaxToken,
                           collectionExpressionSyntax As ExpressionSyntax)

                MyBase.New(container, binder, declaringIdentifier, LocalDeclarationKind.ForEach, Nothing)
                Debug.Assert(collectionExpressionSyntax IsNot Nothing)

                _collectionExpressionSyntax = collectionExpressionSyntax
            End Sub

            ' Compute the type of this variable.
            Friend Overrides Function ComputeTypeInternal(localBinder As Binder) As TypeSymbol

                Dim type As TypeSymbol = Nothing

                type = localBinder.InferForEachVariableType(Me,
                                                       _collectionExpressionSyntax,
                                                       collectionExpression:=Nothing,
                                                       currentType:=Nothing,
                                                       elementType:=Nothing,
                                                       isEnumerable:=Nothing,
                                                       boundGetEnumeratorCall:=Nothing,
                                                       boundEnumeratorPlaceholder:=Nothing,
                                                       boundMoveNextCall:=Nothing,
                                                       boundCurrentAccess:=Nothing,
                                                       collectionPlaceholder:=Nothing,
                                                       needToDispose:=Nothing,
                                                       isOrInheritsFromOrImplementsIDisposable:=Nothing,
                                                       BindingDiagnosticBag.Discarded)
                Return type
            End Function

            Friend Overrides ReadOnly Property HasInferredType As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return GetDeclaringSyntaxReferenceHelper(Of ForEachStatementSyntax)(Me.Locations)
                End Get
            End Property
        End Class

#End Region

#Region "InferredForFromToLocalSymbol"

        ''' <summary>
        ''' A local symbol created by For from-to statement when Option Infer is on.
        ''' </summary>
        Private NotInheritable Class InferredForFromToLocalSymbol
            Inherits SourceLocalSymbol

            Private ReadOnly _fromValue As ExpressionSyntax
            Private ReadOnly _toValue As ExpressionSyntax
            Private ReadOnly _stepClauseOpt As ForStepClauseSyntax

            ''' <summary>
            ''' Create a local variable symbol. Note: this does not insert it automatically into a
            ''' local binder so that it can be found by lookup.
            ''' </summary>
            Public Sub New(container As Symbol,
                           binder As Binder,
                           declaringIdentifier As SyntaxToken,
                           fromValue As ExpressionSyntax,
                           toValue As ExpressionSyntax,
                           stepClauseOpt As ForStepClauseSyntax)

                MyBase.New(container, binder, declaringIdentifier, LocalDeclarationKind.For, Nothing)
                Debug.Assert(fromValue IsNot Nothing AndAlso toValue IsNot Nothing)

                _fromValue = fromValue
                _toValue = toValue
                _stepClauseOpt = stepClauseOpt
            End Sub

            ' Compute the type of this variable.
            Friend Overrides Function ComputeType(Optional containingBinder As Binder = Nothing) As TypeSymbol

                Dim fromValueExpression As BoundExpression = Nothing
                Dim toValueExpression As BoundExpression = Nothing
                Dim stepValueExpression As BoundExpression = Nothing

                Dim type As TypeSymbol = Nothing
                Dim localBinder = If(containingBinder, _binder)

                type = localBinder.InferForFromToVariableType(Me,
                                                   _fromValue,
                                                   _toValue,
                                                   _stepClauseOpt,
                                                   fromValueExpression,
                                                   toValueExpression,
                                                   stepValueExpression,
                                                   BindingDiagnosticBag.Discarded)
                Return type
            End Function

            Friend Overrides ReadOnly Property HasInferredType As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return GetDeclaringSyntaxReferenceHelper(Of ForStatementSyntax)(Me.Locations)
                End Get
            End Property
        End Class

#End Region

#Region "VariableLocalSymbol"
        ''' <summary>
        ''' A local symbol created from a variable declaration or a for statement with an as clause.
        ''' </summary>
        Private NotInheritable Class VariableLocalSymbol
            Inherits SourceLocalSymbol

            Private ReadOnly _modifiedIdentifierOpt As ModifiedIdentifierSyntax ' either Nothing or a modifier identifier containing the type modifiers.
            Private ReadOnly _asClauseOpt As AsClauseSyntax ' can be Nothing if no AsClause
            Private ReadOnly _initializerOpt As EqualsValueSyntax ' can be Nothing if no initializer
            Private _evaluatedConstant As EvaluatedConstantInfo

            Private NotInheritable Class EvaluatedConstantInfo
                Inherits EvaluatedConstant

                Public Sub New(value As ConstantValue, type As TypeSymbol, expression As BoundExpression, diagnostics As ReadOnlyBindingDiagnostic(Of AssemblySymbol))
                    MyBase.New(value, type)

                    Debug.Assert(expression IsNot Nothing)

                    Me.Expression = expression
                    Me.Diagnostics = diagnostics
                End Sub

                Public ReadOnly Expression As BoundExpression
                Public ReadOnly Diagnostics As ReadOnlyBindingDiagnostic(Of AssemblySymbol)
            End Class

            ''' <summary>
            ''' Create a local variable symbol. Note: this does not insert it automatically into a
            ''' local binder so that it can be found by lookup.
            ''' </summary>
            Public Sub New(container As Symbol,
                           binder As Binder,
                           declaringIdentifier As SyntaxToken,
                           modifiedIdentifierOpt As ModifiedIdentifierSyntax,
                           asClauseOpt As AsClauseSyntax,
                           initializerOpt As EqualsValueSyntax,
                           declarationKind As LocalDeclarationKind)

                MyBase.New(container, binder, declaringIdentifier, declarationKind, Nothing)

                Debug.Assert(modifiedIdentifierOpt IsNot Nothing OrElse declarationKind = LocalDeclarationKind.Catch,
                             "Only catch variables should have Nothing for modifiedIdentifierOpt")

                _modifiedIdentifierOpt = modifiedIdentifierOpt
                _asClauseOpt = asClauseOpt
                _initializerOpt = initializerOpt
            End Sub

            ' Compute the type of this variable.
            Friend Overrides Function ComputeTypeInternal(localBinder As Binder) As TypeSymbol

                Dim declType As TypeSymbol = Nothing
                Dim type As TypeSymbol = Nothing
                Dim valueExpression As BoundExpression = Nothing

                type = localBinder.ComputeVariableType(Me,
                                                       _modifiedIdentifierOpt,
                                                       _asClauseOpt,
                                                       _initializerOpt,
                                                       valueExpression,
                                                       declType,
                                                       BindingDiagnosticBag.Discarded)

                Return type
            End Function

            Friend Overrides Function GetConstantExpression(localBinder As Binder) As BoundExpression
                Debug.Assert(localBinder IsNot Nothing)

                If IsConst Then
                    If _evaluatedConstant Is Nothing Then
                        Dim diagBag = BindingDiagnosticBag.GetInstance()

                        ' BindLocalConstantInitializer may be called before or after the constant's type has been set.
                        ' It is called before when we are inferring the constant's type. In that case the constant has no explicit type 
                        ' or the explicit type is object. i.e.
                        '       const x = 1
                        '       const y as object = 2.0
                        ' We do not use the Type property because that would cause the type to be computed.
                        Dim constValue As ConstantValue = Nothing
                        Dim valueExpression As BoundExpression =
                            localBinder.BindLocalConstantInitializer(Me,
                                                                     _lazyType,
                                                                     _modifiedIdentifierOpt,
                                                                     _initializerOpt,
                                                                     diagBag,
                                                                     constValue)

                        Debug.Assert(valueExpression IsNot Nothing)
                        Debug.Assert(constValue Is Nothing OrElse
                                     Not valueExpression.HasErrors AndAlso
                                        (valueExpression.Type Is Nothing OrElse Not valueExpression.Type.IsErrorType))

                        SetConstantExpression(valueExpression.Type, constValue, valueExpression, diagBag.ToReadOnlyAndFree())

                        Return valueExpression
                    End If

                    Return _evaluatedConstant.Expression
                End If

                ' GetConstantExpression should not be called if this is not a constant.
                Throw ExceptionUtilities.Unreachable
            End Function

            Friend Overrides Function GetConstantValue(containingBinder As Binder) As ConstantValue
                If IsConst AndAlso _evaluatedConstant Is Nothing Then
                    Dim localBinder = If(containingBinder, _binder)
                    GetConstantExpression(localBinder)
                End If

                Return If(_evaluatedConstant IsNot Nothing, _evaluatedConstant.Value, Nothing)
            End Function

            Friend Overrides Function GetConstantValueDiagnostics(containingBinder As Binder) As ReadOnlyBindingDiagnostic(Of AssemblySymbol)
                GetConstantValue(containingBinder)
                Return If(_evaluatedConstant IsNot Nothing, _evaluatedConstant.Diagnostics, ReadOnlyBindingDiagnostic(Of AssemblySymbol).Empty)
            End Function

            Private Sub SetConstantExpression(type As TypeSymbol, constantValue As ConstantValue, expression As BoundExpression, diagnostics As ReadOnlyBindingDiagnostic(Of AssemblySymbol))
                If _evaluatedConstant Is Nothing Then
                    Interlocked.CompareExchange(_evaluatedConstant, New EvaluatedConstantInfo(constantValue, type, expression, diagnostics), Nothing)
                End If
            End Sub

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    If _modifiedIdentifierOpt IsNot Nothing Then
                        Return ImmutableArray.Create(_modifiedIdentifierOpt.GetReference())
                    Else
                        Debug.Assert(DeclarationKind = LocalDeclarationKind.Catch, "Only catch variables should have Nothing for _modifiedIdentifierOpt")
                        Return MyBase.DeclaringSyntaxReferences
                    End If
                End Get
            End Property
        End Class

#End Region

#Region "TypeSubstitutedLocalSymbol"
        ''' <summary>
        ''' Local symbol that is not associated with any source.
        ''' </summary>
        ''' <remarks>Generally used for temporary locals past the initial binding phase.</remarks>
        Private NotInheritable Class TypeSubstitutedLocalSymbol
            Inherits LocalSymbol

            Private ReadOnly _originalVariable As LocalSymbol

            Public Sub New(originalVariable As LocalSymbol, type As TypeSymbol)
                MyBase.New(originalVariable._container, type)

                Debug.Assert(originalVariable IsNot Nothing)
                Debug.Assert(type IsNot Nothing)

                _originalVariable = originalVariable
            End Sub

            Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
                Get
                    Return _originalVariable.DeclarationKind
                End Get
            End Property

            Friend Overrides ReadOnly Property SynthesizedKind As SynthesizedLocalKind
                Get
                    Return _originalVariable.SynthesizedKind
                End Get
            End Property

            Public Overrides ReadOnly Property IsFunctionValue As Boolean
                Get
                    Return _originalVariable.IsFunctionValue
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _originalVariable.Name
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return _originalVariable.DeclaringSyntaxReferences
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return _originalVariable.Locations
                End Get
            End Property

            Friend Overrides ReadOnly Property IdentifierToken As SyntaxToken
                Get
                    Return _originalVariable.IdentifierToken
                End Get
            End Property

            Friend Overrides ReadOnly Property IdentifierLocation As Location
                Get
                    Return _originalVariable.IdentifierLocation
                End Get
            End Property

            Friend Overrides ReadOnly Property IsByRef As Boolean
                Get
                    Return _originalVariable.IsByRef
                End Get
            End Property

            Friend Overrides Function GetConstantValue(binder As Binder) As ConstantValue
                Return _originalVariable.GetConstantValue(binder)
            End Function

            Friend Overrides Function GetConstantValueDiagnostics(binder As Binder) As ReadOnlyBindingDiagnostic(Of AssemblySymbol)
                Return _originalVariable.GetConstantValueDiagnostics(binder)
            End Function

            Friend Overrides Function GetDeclaratorSyntax() As SyntaxNode
                Return _originalVariable.GetDeclaratorSyntax()
            End Function
        End Class
#End Region
    End Class

End Namespace
