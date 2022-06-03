' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
#If DEBUG Then
    ''' <summary>
    ''' The ImplicitVariableBinder manages implicitly declared local variables in VB.
    ''' Unlike other Binders, the ImplicitVariableBinder is observably 
    ''' mutable -- as new implicit local variables are declared, those variables will be
    ''' found by lookup operations on the binder. This mutability requires that use of the 
    ''' ImplicitVariableBinder be treated with somewhat more care than other binders.
    ''' 
    ''' The implicit variable binder is placed immediately outside the method body binder
    ''' when the method body binder is created.
    ''' 
    ''' Furthermore, the semantics of binding of implicitly declared local variables in VB
    ''' are order dependent because of type characters:
    '''     x$ = "hello": x = "hi"   ' OK
    '''     x = "hi": X$ = "hello"   ' error: x is of type Object.
    ''' 
    ''' An ImplicitVariableBinder can be frozen, at which point additional variables cannot
    ''' be declared. This should be done once an entire method body is bound.
    ''' 
    ''' Thus, it is important that only one thread at a time be allowed to access the implicit variable binder for
    ''' declaration (once frozen, it is OK for multiple threads to do lookups.)
    ''' 
    ''' In Debug, Asserts validate these rules.
    ''' 
    ''' Additional assert to make sure that declarations are handled in order is handled by 
    ''' <see cref="ExecutableCodeBinder.CheckSimpleNameBindingOrder"/>
    ''' </summary>
#End If
    Friend Class ImplicitVariableBinder
        Inherits Binder

        Private ReadOnly _containerOfLocals As Symbol

        Private _frozen As Boolean
        Private _implicitLocals As Dictionary(Of String, LocalSymbol)
        Private _possiblyShadowingVariables As MultiDictionary(Of String, ShadowedVariableInfo)

#If DEBUG Then
        ' All declarations for a ImplicitVariableBinder should occur on the same thread.
        ' It is OK for lookup to occur on a different thread, if the binder has
        ' been frozen. -1 when not yet initialized.
        Private _threadIdForDeclaration As Integer = -1
#End If

        ''' <summary>
        ''' If Option Explicit is Off for this source file, then implicit variable declaration will be allowed
        ''' in this binder. "containerOfLocals" is the container for implicitly declared variables.
        ''' </summary>
        Public Sub New(containingBinder As Binder, containerOfLocals As Symbol)
            MyBase.New(containingBinder)
            Debug.Assert(containingBinder.OptionExplicit = False)

            _containerOfLocals = containerOfLocals
            _frozen = False
        End Sub

        Friend Overrides Function BindGroupAggregationExpression(group As GroupAggregationSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            ' Need this for ImplicitVariableBinder created by SpeculativeBinder.
            Return Me.ContainingBinder.BindGroupAggregationExpression(group, diagnostics)
        End Function

        Friend Overrides Function BindFunctionAggregationExpression([function] As FunctionAggregationSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            ' Need this for ImplicitVariableBinder created by SpeculativeBinder.
            Return Me.ContainingBinder.BindFunctionAggregationExpression([function], diagnostics)
        End Function

        ''' <summary>
        ''' Disallow additional local variable declaration (make binder frozen)
        ''' and report delayed shadowing diagnostics.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub DisallowFurtherImplicitVariableDeclaration(diagnostics As BindingDiagnosticBag)
#If DEBUG Then
            CheckVariableDeclarationOnSingleThread()
#End If

            If Not _frozen Then
                _frozen = True

                If _implicitLocals IsNot Nothing AndAlso _possiblyShadowingVariables IsNot Nothing Then
                    For Each localName As String In _implicitLocals.Keys
                        For Each shadowingVariableInfo In _possiblyShadowingVariables(localName)
                            ' An already declared variable in an enclosed block is shadowing this new implicit variable. 
                            ' Report an error at THAT declaration's location.
                            ReportDiagnostic(diagnostics, shadowingVariableInfo.Location, shadowingVariableInfo.ErrorId, shadowingVariableInfo.Name)
                        Next
                    Next
                End If
            End If
        End Sub

        ''' <summary>
        ''' True if implicit variable declaration is done (binder is frozen and doesn't
        ''' allow additional implicit variable declaration)
        ''' </summary>
        Public Overrides ReadOnly Property AllImplicitVariableDeclarationsAreHandled As Boolean
            Get
                Return _frozen
            End Get
        End Property

        ''' <summary>
        ''' True if we are in a place that allows implicit variable declaration. This binder
        ''' implies that.
        ''' </summary>
        Public Overrides ReadOnly Property ImplicitVariableDeclarationAllowed As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Get all implicitly declared variables that were declared in this method body. The binder
        ''' must be frozen before this can be obtained.
        ''' </summary>
        Public Overrides ReadOnly Property ImplicitlyDeclaredVariables As ImmutableArray(Of LocalSymbol)
            Get
                Debug.Assert(_frozen)

                If _implicitLocals Is Nothing Then
                    Return ImmutableArray(Of LocalSymbol).Empty
                Else
                    Dim builder As ArrayBuilder(Of LocalSymbol) = ArrayBuilder(Of LocalSymbol).GetInstance()
                    builder.AddRange(_implicitLocals.Values)
                    Return builder.ToImmutableAndFree()
                End If
            End Get
        End Property

        ''' <summary>
        ''' Declare an implicit local variable. The type of the local is determined
        ''' by the type character (if any) on the variable.
        ''' </summary>
        Public Overrides Function DeclareImplicitLocalVariable(nameSyntax As IdentifierNameSyntax, diagnostics As BindingDiagnosticBag) As LocalSymbol
#If DEBUG Then
            Debug.Assert(Not _frozen)
            CheckVariableDeclarationOnSingleThread()
#End If

            ' Type is always Object, unless type character specified.
            Dim localSpecialType As SpecialType = SpecialType.System_Object
            If nameSyntax.Identifier.GetTypeCharacter() <> TypeCharacter.None Then
                Dim unused As String = Nothing
                localSpecialType = GetSpecialTypeForTypeCharacter(nameSyntax.Identifier.GetTypeCharacter(), unused)
            End If

            Dim localVar = LocalSymbol.Create(_containerOfLocals,
                                           Me,
                                           nameSyntax.Identifier,
                                           LocalDeclarationKind.ImplicitVariable,
                                           GetSpecialType(localSpecialType, nameSyntax, diagnostics))

            If _implicitLocals Is Nothing Then
                _implicitLocals = New Dictionary(Of String, LocalSymbol)(IdentifierComparison.Comparer)
            End If

            _implicitLocals.Add(nameSyntax.Identifier.ValueText, localVar)
            Return localVar
        End Function

        ''' <summary>
        ''' A tricky problem is reporting the "Variable 'x' hides a variable in an enclosing block" message if the variable in
        ''' an enclosing block is an implicit variable that hasn't been declared yet. We handle this by remembering any variable
        ''' declarations in enclosed blocks, and then report the error when the implicit variable is declared.
        ''' </summary>
        Public Sub RememberPossibleShadowingVariable(name As String, syntax As SyntaxNodeOrToken, errorId As ERRID)
#If DEBUG Then
            Debug.Assert(Not _frozen)
            CheckVariableDeclarationOnSingleThread()
#End If

            If _possiblyShadowingVariables Is Nothing Then
                _possiblyShadowingVariables = New MultiDictionary(Of String, ShadowedVariableInfo)(IdentifierComparison.Comparer)
            End If

            _possiblyShadowingVariables.Add(name, New ShadowedVariableInfo(name, syntax.GetLocation(), errorId))
        End Sub

        ' Structure for saving information about possible shadowing variables.
        Private Structure ShadowedVariableInfo
            Public ReadOnly Name As String
            Public ReadOnly Location As Location
            Public ReadOnly ErrorId As ERRID
            Public Sub New(name As String, location As Location, errorId As ERRID)
                Me.Name = name
                Me.Location = location
                Me.ErrorId = errorId
            End Sub
        End Structure

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                      name As String,
                                                      arity As Integer,
                                                      options As LookupOptions,
                                                      originalBinder As Binder,
                                                      <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
#If DEBUG Then
            CheckVariableDeclarationOnSingleThread()
#End If
            ' locals are always arity 0, and never types and namespaces.
            Dim localSymbol As LocalSymbol = Nothing
            If _implicitLocals IsNot Nothing AndAlso (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly)) = 0 Then
                If _implicitLocals.TryGetValue(name, localSymbol) Then
                    lookupResult.SetFrom(CheckViability(localSymbol, arity, options, Nothing, useSiteInfo))
                End If
            End If

            Return
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                   options As LookupOptions,
                                                                   originalBinder As Binder)
            Debug.Assert(_frozen)

            If _implicitLocals IsNot Nothing AndAlso
               (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly)) = 0 Then

                For Each localSymbol In _implicitLocals.Values
                    If originalBinder.CanAddLookupSymbolInfo(localSymbol, options, nameSet, Nothing) Then
                        nameSet.AddSymbol(localSymbol, localSymbol.Name, 0)
                    End If
                Next
            End If
        End Sub

#If DEBUG Then
        ' Check that variable declarations all occur in a single thread.
        Private Sub CheckVariableDeclarationOnSingleThread()
            If Not _frozen Then
                ' First time we're called, get the thread id. Subsequent times, check it hasn't changed.
                If _threadIdForDeclaration = -1 Then
                    Interlocked.CompareExchange(_threadIdForDeclaration, Environment.CurrentManagedThreadId, -1)
                End If

                Debug.Assert(_threadIdForDeclaration = Environment.CurrentManagedThreadId)
            End If
        End Sub
#End If

    End Class
End Namespace
