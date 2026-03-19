' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Represents the state of compilation of one particular type.
    ''' This includes, for example, a collection of synthesized methods created during lowering.
    ''' WARNING: Note that the underlying collection classes are not thread-safe and this will
    ''' need to be revised if emit phase is changed to support multithreading when
    ''' translating a particular type.
    ''' </summary>
    Friend Class TypeCompilationState

        ''' <summary> Method's information </summary>
        Public Structure MethodWithBody
            Public ReadOnly Method As MethodSymbol
            Public ReadOnly Body As BoundStatement
            Public ReadOnly StateMachineType As StateMachineTypeSymbol
            Public ReadOnly StateMachineStatesDebugInfo As ImmutableArray(Of StateMachineStateDebugInfo)

            Friend Sub New(method As MethodSymbol,
                           body As BoundStatement,
                           stateMachineType As StateMachineTypeSymbol,
                           stateMachineStatesDebugInfo As ImmutableArray(Of StateMachineStateDebugInfo))

                Debug.Assert(Not stateMachineStatesDebugInfo.IsDefault)

                Me.Method = method
                Me.Body = body
                Me.StateMachineType = stateMachineType
                Me.StateMachineStatesDebugInfo = stateMachineStatesDebugInfo
            End Sub
        End Structure

        Public ReadOnly Compilation As VisualBasicCompilation

        Public staticLambdaFrame As LambdaFrame

        ' Can be Nothing if we're not emitting.
        ' During the lowering phase, however, it cannot be Nothing.
        Public ReadOnly ModuleBuilderOpt As PEModuleBuilder

        ''' <summary> Flat array of created methods, non-empty if not-nothing </summary>
        Private _synthesizedMethods As ArrayBuilder(Of MethodWithBody) = Nothing

        Public ReadOnly InitializeComponentOpt As MethodSymbol

        ''' <summary>
        ''' A mapping from (source) iterator or async methods to the compiler-generated classes that implement them.
        ''' </summary>
        Public ReadOnly StateMachineImplementationClass As New Dictionary(Of MethodSymbol, NamedTypeSymbol)(ReferenceEqualityComparer.Instance)

        ''' <summary>
        ''' Map of 'MyBase' or 'MyClass' call wrappers; actually each method symbol will
        ''' only need one wrapper to call it non-virtually;
        '''
        ''' Indeed, if the type have a virtual method M1 overridden, MyBase.M1 will use
        ''' a wrapper for base type's method and MyClass.M1 a wrapper for this type's method.
        '''
        ''' And if the type does not override a virtual method M1, both MyBase.M1
        ''' and MyClass.M1 will use a wrapper for base type's method.
        ''' </summary>
        Private _methodWrappers As Dictionary(Of MethodSymbol, MethodSymbol) = Nothing

        Private _initializeComponentCallTree As Dictionary(Of MethodSymbol, ImmutableArray(Of MethodSymbol)) = Nothing

        Public Sub New(compilation As VisualBasicCompilation, moduleBuilderOpt As PEModuleBuilder, initializeComponentOpt As MethodSymbol)
            Me.Compilation = compilation
            Me.ModuleBuilderOpt = moduleBuilderOpt
            Me.InitializeComponentOpt = initializeComponentOpt
        End Sub

        ''' <summary>
        ''' Is there any content in the methods collection.
        ''' </summary>
        Public ReadOnly Property HasSynthesizedMethods As Boolean
            Get
                Return _synthesizedMethods IsNot Nothing
            End Get
        End Property

        ''' <summary> Method created with their bodies </summary>
        Public ReadOnly Property SynthesizedMethods As ArrayBuilder(Of MethodWithBody)
            Get
                Return _synthesizedMethods
            End Get
        End Property

        Public Sub AddSynthesizedMethod(method As MethodSymbol,
                                        body As BoundStatement,
                                        stateMachineType As StateMachineTypeSymbol,
                                        stateMachineStatesDebugInfo As ImmutableArray(Of StateMachineStateDebugInfo))
            If _synthesizedMethods Is Nothing Then
                _synthesizedMethods = ArrayBuilder(Of MethodWithBody).GetInstance()
            End If

            _synthesizedMethods.Add(New MethodWithBody(method, body, stateMachineType, stateMachineStatesDebugInfo))
        End Sub

        Public Function HasMethodWrapper(method As MethodSymbol) As Boolean
            Return _methodWrappers IsNot Nothing AndAlso _methodWrappers.ContainsKey(method)
        End Function

        Public Sub AddMethodWrapper(method As MethodSymbol, wrapper As MethodSymbol, body As BoundStatement)
            If _methodWrappers Is Nothing Then
                _methodWrappers = New Dictionary(Of MethodSymbol, MethodSymbol)()
            End If

            _methodWrappers(method) = wrapper
            AddSynthesizedMethod(wrapper, body, stateMachineType:=Nothing, ImmutableArray(Of StateMachineStateDebugInfo).Empty)
        End Sub

        Public Function GetMethodWrapper(method As MethodSymbol) As MethodSymbol
            Dim wrapper As MethodSymbol = Nothing
            Return If(_methodWrappers IsNot Nothing AndAlso _methodWrappers.TryGetValue(method, wrapper), wrapper, Nothing)
        End Function

        ''' <summary> Free resources </summary>
        Public Sub Free()
            If Me._synthesizedMethods IsNot Nothing Then
                Me._synthesizedMethods.Free()
                Me._synthesizedMethods = Nothing
            End If

            If _methodWrappers IsNot Nothing Then
                _methodWrappers = Nothing
            End If

        End Sub

        Public Sub AddToInitializeComponentCallTree(method As MethodSymbol, callees As ImmutableArray(Of MethodSymbol))
#If DEBUG Then
            Debug.Assert(method.IsDefinition)
            For Each m In callees
                Debug.Assert(m.IsDefinition)
            Next
#End If

            If _initializeComponentCallTree Is Nothing Then
                _initializeComponentCallTree = New Dictionary(Of MethodSymbol, ImmutableArray(Of MethodSymbol))(ReferenceEqualityComparer.Instance)
            End If

            _initializeComponentCallTree.Add(method, callees)
        End Sub

        Public Function CallsInitializeComponent(method As MethodSymbol) As Boolean
            Debug.Assert(method.IsDefinition)

            If _initializeComponentCallTree Is Nothing Then
                Return False
            End If

            Return CallsInitializeComponent(method, New HashSet(Of MethodSymbol)(ReferenceEqualityComparer.Instance))
        End Function

        Private Function CallsInitializeComponent(method As MethodSymbol, visited As HashSet(Of MethodSymbol)) As Boolean
            Dim added = visited.Add(method)
            Debug.Assert(added)

            Dim callees As ImmutableArray(Of MethodSymbol) = Nothing

            If _initializeComponentCallTree.TryGetValue(method, callees) Then
                For Each m In callees
                    If m Is InitializeComponentOpt Then
                        Return True
                    ElseIf Not visited.Contains(m) AndAlso CallsInitializeComponent(m, visited) Then
                        Return True
                    End If
                Next
            End If

            Return False
        End Function

    End Class

End Namespace

