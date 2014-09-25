' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a local variable (typically inside a method body). This could also be a local variable implicitly
    ''' declared by a For, Using, etc. When used as a temporary variable, its container can also be a Field or Property Symbol.
    ''' </summary>
    ''' <remarks></remarks>
    Friend MustInherit Class LocalSymbol
        Inherits Symbol
        Implements ILocalSymbol

        Friend Shared ReadOnly UseBeforeDeclarationResultType As ErrorTypeSymbol = New ErrorTypeSymbol()

        Private ReadOnly _container As Symbol ' the method, field or property that contains the declaration of this variable
        Friend ReadOnly DeclarationKind As LocalDeclarationKind

        Friend Overridable ReadOnly Property SynthesizedLocalKind As SynthesizedLocalKind
            Get
                Return SynthesizedLocalKind.None
            End Get
        End Property

        Private _type As TypeSymbol

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

            Return New LocalSymbolWithBinder(container, binder, declaringIdentifier, declarationKind, type)
        End Function

        ''' <summary>
        ''' Create a local symbol associated with an identifier token and a different name (used for operators, etc.)
        ''' </summary>
        Friend Shared Function Create(container As Symbol,
            aliasName As String,
            declaringIdentifier As SyntaxToken,
            declarationKind As LocalDeclarationKind,
            type As TypeSymbol) As LocalSymbol

            Return New AliasLocalSymbol(container, aliasName, declaringIdentifier, declarationKind, type)
        End Function

        ''' <summary>
        ''' Create a local symbol that is not associated with any source.
        ''' </summary>
        ''' <remarks>Generally used for temporary locals past the initial binding phase.</remarks>
        Friend Shared Function Create(container As Symbol,
            name As String,
            declarationKind As LocalDeclarationKind,
            type As TypeSymbol) As LocalSymbol

            Return New NoLocationVariable(container, name, declarationKind, type)
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
        Friend Sub New(container As Symbol,
                    declarationKind As LocalDeclarationKind,
                    type As TypeSymbol)

            Debug.Assert(container IsNot Nothing, "local must belong to a method, field or property")
            Debug.Assert(container.Kind = SymbolKind.Method OrElse
                         container.Kind = SymbolKind.Field OrElse
                         container.Kind = SymbolKind.Property,
                         "Unsupported container. Must be method, field or property.")

            _container = container
            Me.DeclarationKind = declarationKind
            _type = type
        End Sub

        Public Overridable ReadOnly Property Type As TypeSymbol
            Get
                If _type Is Nothing Then
                    Interlocked.CompareExchange(_type, ComputeType(), Nothing)
                End If

                Return _type
            End Get
        End Property

        Friend ReadOnly Property ConstHasType As Boolean
            Get
                Debug.Assert(Me.IsConst)
                Return Me._type IsNot Nothing
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
            If _type Is Nothing Then
                Interlocked.CompareExchange(_type, type, Nothing)
                Debug.Assert((Me.IsFunctionValue AndAlso _container.Kind = SymbolKind.Method AndAlso DirectCast(_container, MethodSymbol).MethodKind = MethodKind.LambdaMethod) OrElse type.Equals(ComputeType()))
            Else
                Debug.Assert(type.Equals(_type), "Attempted to set a local variable with a different type")
            End If
        End Sub

        ' Compute the type of this variable.
        Friend Overridable Function ComputeType(Optional containingBinder As Binder = Nothing) As TypeSymbol
            Debug.Assert(_type IsNot Nothing)
            Return _type
        End Function

        Public MustOverride Overrides ReadOnly Property Name As String

        ' Get the identifier token that defined this local symbol. This is useful for robustly checking
        ' if a local symbol actually matches a particular definition, even in the presence of duplicates.
        Friend MustOverride ReadOnly Property IdentifierToken As SyntaxToken

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Local
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(Of Location)(Me.IdentifierLocation)
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

        Public ReadOnly Property IsFunctionValue As Boolean Implements ILocalSymbol.IsFunctionValue
            Get
                Return Me.DeclarationKind = LocalDeclarationKind.FunctionValue
            End Get
        End Property

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

        Friend Overridable Function GetConstantValueDiagnostics(binder As Binder) As IEnumerable(Of Diagnostic)
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

        Private ReadOnly Property ILocalSymbol_IsConst As Boolean Implements ILocalSymbol.IsConst
            Get
                Return Me.IsConst
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

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitLocal(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitLocal(Me)
        End Function

#End Region

#Region "SourceLocalSymbol"

        ''' <summary>
        ''' Base class for any local symbol that can be referenced in source, might be implicitly declared.
        ''' </summary>
        Private MustInherit Class SourceLocalSymbol
            Inherits LocalSymbol

            Protected ReadOnly _declaringIdentifier As SyntaxToken

            Public Sub New(container As Symbol,
                           declaringIdentifier As SyntaxToken,
                           declarationKind As LocalDeclarationKind,
                           type As TypeSymbol)
                MyBase.New(container, declarationKind, type)

                Debug.Assert(declaringIdentifier.VisualBasicKind <> SyntaxKind.None)
                _declaringIdentifier = declaringIdentifier
            End Sub


            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Dim name = TryCast(Me._declaringIdentifier.Parent, IdentifierNameSyntax)
                    If name IsNot Nothing Then
                        Return ImmutableArray.Create(Of SyntaxReference)(name.GetReference())
                    Else
                        Return ImmutableArray(Of SyntaxReference).Empty
                    End If
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property IdentifierLocation As Location
                Get
                    Return _declaringIdentifier.GetLocation()
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property IdentifierToken As SyntaxToken
                Get
                    Return _declaringIdentifier
                End Get
            End Property

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, SourceLocalSymbol)

                Return other IsNot Nothing AndAlso other._declaringIdentifier.Equals(Me._declaringIdentifier) AndAlso Equals(other._container, Me._container) AndAlso String.Equals(other.Name, Me.Name)
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(_declaringIdentifier.GetHashCode(), Me._container.GetHashCode())
            End Function
        End Class
#End Region

#Region "LocalSymbolWithBinder"
        ''' <summary>
        ''' Base class for any local symbol that needs a binder
        ''' </summary>
        ''' <remarks></remarks>
        Private Class LocalSymbolWithBinder
            Inherits SourceLocalSymbol

            Private ReadOnly _binder As Binder

            ''' <summary>
            ''' Create a local variable symbol. Note: this does not insert it automatically into a
            ''' local binder so that it can be found by lookup.
            ''' </summary>
            Public Sub New(container As Symbol,
                           binder As Binder,
                           declaringIdentifier As SyntaxToken,
                           declarationKind As LocalDeclarationKind,
                           type As TypeSymbol)
                MyBase.New(container, declaringIdentifier, declarationKind, type)

                Debug.Assert(binder IsNot Nothing, "expected a binder")
                _binder = binder
            End Sub

            Public NotOverridable Overrides ReadOnly Property Name As String
                Get
                    Return _declaringIdentifier.GetIdentifierText()
                End Get
            End Property

            Friend Overrides ReadOnly Property SynthesizedLocalKind As SynthesizedLocalKind
                Get
                    Return SynthesizedLocalKind.None
                End Get
            End Property

            Friend ReadOnly Property Binder As Binder
                Get
                    Return _binder
                End Get
            End Property

            Friend Overrides Function ComputeType(Optional containingBinder As Binder = Nothing) As TypeSymbol
                containingBinder = If(containingBinder, Binder)
                Dim type As TypeSymbol = ComputeTypeInternal(If(containingBinder, Binder))
                Return type
            End Function

            Friend Overridable Function ComputeTypeInternal(containingBinder As Binder) As TypeSymbol
                Debug.Assert(_type IsNot Nothing)
                Return _type
            End Function

        End Class

#End Region

#Region "AliasLocalSymbol"
        ''' <summary>
        ''' Class for a local symbol that has a different name than the identifier token.
        ''' In this case the real name is returned by the name property and the "VB User visible name" can be
        ''' obtained by accessing the IdentifierToken.
        ''' </summary>
        ''' <remarks></remarks>
        Private NotInheritable Class AliasLocalSymbol
            Inherits SourceLocalSymbol

            Private ReadOnly _aliasName As String

            ''' <summary>
            ''' Create a local variable symbol. Note: this does not insert it automatically into a
            ''' local binder so that it can be found by lookup.
            ''' </summary>
            Public Sub New(container As Symbol,
                           aliasName As String,
                           declaringIdentifier As SyntaxToken,
                           declarationKind As LocalDeclarationKind,
                           type As TypeSymbol)
                MyBase.New(container, declaringIdentifier, declarationKind, type)

                Debug.Assert(aliasName IsNot Nothing, "expected an alias")
                Debug.Assert(type IsNot Nothing)
                _aliasName = aliasName
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _aliasName
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
            Inherits LocalSymbolWithBinder

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

                Dim diagBag = DiagnosticBag.GetInstance()

                Dim collectionExpression As BoundExpression = Nothing
                Dim elementType As TypeSymbol = Nothing
                Dim isEnumerable = False
                Dim boundGetEnumeratorCall As BoundExpression = Nothing
                Dim boundEnumeratorPlaceholder As BoundLValuePlaceholder = Nothing
                Dim boundMoveNextCall As BoundExpression = Nothing
                Dim boundCurrentCall As BoundExpression = Nothing
                Dim collectionPlaceholder As BoundRValuePlaceholder = Nothing
                Dim needToDispose = False
                Dim isOrInheritsFromOrImplementsIDisposable = False
                Dim type As TypeSymbol = Nothing

                type = localBinder.InferForEachVariableType(Me,
                                                       _collectionExpressionSyntax,
                                                       collectionExpression,
                                                       elementType,
                                                       isEnumerable,
                                                       boundGetEnumeratorCall,
                                                       boundEnumeratorPlaceholder,
                                                       boundMoveNextCall,
                                                       boundCurrentCall,
                                                       collectionPlaceholder,
                                                       needToDispose,
                                                       isOrInheritsFromOrImplementsIDisposable,
                                                       diagBag)
                diagBag.Free()
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
        ''' <remarks></remarks>
        Private NotInheritable Class InferredForFromToLocalSymbol
            Inherits LocalSymbolWithBinder

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

                Dim diagBag = DiagnosticBag.GetInstance()

                Dim fromValueExpression As BoundExpression = Nothing
                Dim toValueExpression As BoundExpression = Nothing
                Dim stepValueExpression As BoundExpression = Nothing

                Dim type As TypeSymbol = Nothing
                Dim localBinder = If(containingBinder, Binder)

                type = localBinder.InferForFromToVariableType(Me,
                                                   _fromValue,
                                                   _toValue,
                                                   _stepClauseOpt,
                                                   fromValueExpression,
                                                   toValueExpression,
                                                   stepValueExpression,
                                                   diagBag)
                diagBag.Free()
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
        ''' <remarks></remarks>
        Private NotInheritable Class VariableLocalSymbol
            Inherits LocalSymbolWithBinder

            Private ReadOnly _modifiedIdentifierOpt As ModifiedIdentifierSyntax ' either Nothing or a modifier identifier containing the type modifiers.
            Private ReadOnly _asClauseOpt As AsClauseSyntax ' can be Nothing if no AsClause
            Private ReadOnly _initializerOpt As EqualsValueSyntax ' can be Nothing is no initializer
            Private _evaluatedConstant As EvaluatedConstant

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

                _modifiedIdentifierOpt = modifiedIdentifierOpt
                _asClauseOpt = asClauseOpt
                _initializerOpt = initializerOpt
            End Sub

            ' Compute the type of this variable.
            Friend Overrides Function ComputeTypeInternal(localBinder As Binder) As TypeSymbol

                Dim diagBag = DiagnosticBag.GetInstance()

                Dim declType As TypeSymbol = Nothing
                Dim type As TypeSymbol = Nothing
                Dim valueExpression As BoundExpression = Nothing

                type = localBinder.ComputeVariableType(Me,
                                                       _modifiedIdentifierOpt,
                                                       _asClauseOpt,
                                                       _initializerOpt,
                                                       valueExpression,
                                                       declType,
                                                       diagBag)

                diagBag.Free()
                Return type
            End Function

            Friend Overrides Function GetConstantExpression(localBinder As Binder) As BoundExpression
                Debug.Assert(localBinder IsNot Nothing)

                If IsConst Then
                    If _evaluatedConstant Is Nothing Then
                        Dim diagBag = DiagnosticBag.GetInstance()

                        ' BindLocalConstantInitializer may be called before or after the constant's type has been set.
                        ' It is called before when we are inferring the constant's type. In that case the constant has no explicit type 
                        ' or the explicit type is object. i.e.
                        '       const x = 1
                        '       const y as object = 2.0
                        ' We do not use the Type property because that would cause the type to be computed.
                        Dim constValue As ConstantValue = Nothing
                        Dim valueExpression As BoundExpression =
                            localBinder.BindLocalConstantInitializer(Me,
                                                                     _type,
                                                                     _modifiedIdentifierOpt,
                                                                     _initializerOpt,
                                                                     diagBag,
                                                                     constValue)

                        Debug.Assert(valueExpression IsNot Nothing)
                        Debug.Assert(constValue Is Nothing OrElse
                                     Not valueExpression.HasErrors AndAlso
                                        (valueExpression.Type Is Nothing OrElse Not valueExpression.Type.IsErrorType))

                        SetConstantExpression(valueExpression.Type, constValue, diagBag.ToReadOnlyAndFree())

                        Return valueExpression
                    End If

                    ' This is here in case GetConstantExpression is called a second time.  Normally this does not happen but one case happens due to the debug.assert in LocalSymbol.SetType()
                    ' which called ComputeType and then GetConstantExpression indirectly.  Because the bound expression is not saved in the symbol.  Create one with the constant value.

                    If _evaluatedConstant.Value IsNot Nothing Then
                        Return New BoundLiteral(_initializerOpt, _evaluatedConstant.Value, _evaluatedConstant.Type)
                    End If

                    Return New BoundBadExpression(If(DirectCast(_initializerOpt, VisualBasicSyntaxNode), _modifiedIdentifierOpt),
                                                  LookupResultKind.Empty, ImmutableArray(Of Symbol).Empty, ImmutableArray(Of BoundNode).Empty, _evaluatedConstant.Type, hasErrors:=True)
                End If

                ' GetConstantExpression should not be called if this is not a constant.
                Throw ExceptionUtilities.Unreachable
            End Function

            Friend Overrides Function GetConstantValue(containingBinder As Binder) As ConstantValue
                If IsConst AndAlso _evaluatedConstant Is Nothing Then
                    Dim localBinder = If(containingBinder, Binder)
                    GetConstantExpression(localBinder)
                End If

                Return If(_evaluatedConstant IsNot Nothing, _evaluatedConstant.Value, Nothing)
            End Function

            Friend Overrides Function GetConstantValueDiagnostics(containingBinder As Binder) As IEnumerable(Of Diagnostic)
                GetConstantValue(containingBinder)
                Return If(_evaluatedConstant IsNot Nothing, _evaluatedConstant.Diagnostics, Nothing)
            End Function

            Private Sub SetConstantExpression(type As TypeSymbol, constantValue As ConstantValue, diagnostics As ImmutableArray(Of Diagnostic))
                If _evaluatedConstant Is Nothing Then
                    Interlocked.CompareExchange(_evaluatedConstant, New EvaluatedConstant(constantValue, type, diagnostics), Nothing)
                End If
            End Sub

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Select Case DeclarationKind
                        Case LocalDeclarationKind.None, LocalDeclarationKind.FunctionValue
                            Return ImmutableArray(Of SyntaxReference).Empty

                        Case Else
                            If _modifiedIdentifierOpt IsNot Nothing Then
                                Return ImmutableArray.Create(Of SyntaxReference)(_modifiedIdentifierOpt.GetReference())
                            Else
                                Return ImmutableArray(Of SyntaxReference).Empty
                            End If
                    End Select
                End Get
            End Property
        End Class

#End Region

#Region "NoLocationVariable"
        ''' <summary>
        ''' Local symbol that is not associated with any source.
        ''' </summary>
        ''' <remarks>Generally used for temporary locals past the initial binding phase.</remarks>
        Private NotInheritable Class NoLocationVariable
            Inherits LocalSymbol

            Private ReadOnly m_Name As String

            Public Sub New(container As Symbol,
                name As String,
                declarationKind As LocalDeclarationKind,
                type As TypeSymbol)
                MyBase.New(container, declarationKind, type)

                Debug.Assert(type IsNot Nothing)
                m_Name = name
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return m_Name
                End Get
            End Property

            Friend Overrides ReadOnly Property IdentifierToken As SyntaxToken
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property IdentifierLocation As Location
                Get
                    Return NoLocation.Singleton
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property
        End Class
#End Region
    End Class

End Namespace
