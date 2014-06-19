Imports System.Collections.Generic
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Collections
Imports Roslyn.Compilers.Internal
Imports Microsoft.Cci

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of AsyncRewriter.AsyncStateMachineTypeSymbol)

        ''' <summary>
        ''' Async methods have both a return type (void, Task, or Task(Of T)) and a 'result' type, which is the
        ''' operand type of any return expressions in the async method. The result type is void in the case of
        ''' Task-returning and void-returning async methods, and T in the case of Task(Of T)-returning async
        ''' methods.
        ''' 
        ''' System.Runtime.CompilerServices provides a collection of async method builders that are used in the
        ''' generated code of async methods to create and manipulate the async method's task. There are three
        ''' distinct async method builder types, one of each async return type: AsyncVoidMethodBuilder,
        ''' AsyncTaskMethodBuilder, and AsyncTaskMethodBuilder(Of T). 
        '''
        ''' AsyncMethodBuilderMemberCollection provides a common mechanism for accessing the well-known members of
        ''' each async method builder type. This avoids having to inspect the return style of the current async method
        ''' to pick the right async method builder member during async rewriting.
        ''' </summary>
        Friend Structure AsyncMethodBuilderMemberCollection

            ''' <summary> The builder's constructed type. </summary>
            Friend ReadOnly BuilderType As NamedTypeSymbol

            ''' <summary> The result type of the constructed task: T for Task(Of T), void otherwise. </summary>
            Friend ReadOnly ResultType As TypeSymbol

            ''' <summary> Binds an exception to the method builder. </summary>
            Friend ReadOnly SetException As MethodSymbol

            ''' <summary> Marks the method builder as successfully completed, and sets the result if method is Task(Of T)-returning. </summary>
            Friend ReadOnly SetResult As MethodSymbol

            ''' <summary> Schedules the state machine to proceed to the next action when the specified awaiter completes. </summary>
            Friend ReadOnly AwaitOnCompleted As MethodSymbol

            ''' <summary> Schedules the state machine to proceed to the next action when the specified 
            ''' awaiter completes. This method can be called from partially trusted code. </summary>
            Friend ReadOnly AwaitUnsafeOnCompleted As MethodSymbol

            ''' <summary> Begins running the builder with the associated state machine. </summary>
            Friend ReadOnly Start As MethodSymbol

            ''' <summary> Associates the builder with the specified state machine. </summary>
            Friend ReadOnly SetStateMachine As MethodSymbol

            ''' <summary> Get the constructed task for a Task-returning or Task&lt;T&gt;-returning async method. </summary>
            Friend ReadOnly Task As PropertySymbol

            Friend Sub New(builderType As NamedTypeSymbol,
                           resultType As TypeSymbol,
                           setException As MethodSymbol,
                           setResult As MethodSymbol,
                           awaitOnCompleted As MethodSymbol,
                           awaitUnsafeOnCompleted As MethodSymbol,
                           start As MethodSymbol,
                           setStateMachine As MethodSymbol,
                           task As PropertySymbol)

                Me.BuilderType = builderType
                Me.ResultType = resultType
                Me.SetException = setException
                Me.SetResult = setResult
                Me.AwaitOnCompleted = awaitOnCompleted
                Me.AwaitUnsafeOnCompleted = awaitUnsafeOnCompleted
                Me.Start = start
                Me.SetStateMachine = setStateMachine
                Me.Task = task
            End Sub

            Friend Shared Function Create(F As SyntheticBoundNodeFactory, method As MethodSymbol, typeMap As TypeSubstitution) As AsyncMethodBuilderMemberCollection
                If method.IsVoidReturningAsync() Then
                    Return Create(F:=F,
                                  builderType:=F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder),
                                  resultType:=F.SpecialType(SpecialType.System_Void),
                                  setException:=WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetException,
                                  setResult:=WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetResult,
                                  awaitOnCompleted:=WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitOnCompleted,
                                  awaitUnsafeOnCompleted:=WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitUnsafeOnCompleted,
                                  start:=WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Start_T,
                                  setStateMachine:=WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetStateMachine,
                                  task:=Nothing)
                End If

                If method.IsTaskReturningAsync() Then
                    Dim builderType As NamedTypeSymbol = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder)
                    Dim task As PropertySymbol = F.WellKnownMember(Of PropertySymbol)(WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Task).AsMember(builderType)

                    Return Create(F:=F,
                                  builderType:=F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder),
                                  resultType:=F.SpecialType(SpecialType.System_Void),
                                  setException:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetException,
                                  setResult:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult,
                                  awaitOnCompleted:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted,
                                  awaitUnsafeOnCompleted:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted,
                                  start:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T,
                                  setStateMachine:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine,
                                  task:=task)
                End If

                If method.IsGenericTaskReturningAsync() Then
                    Dim resultType As TypeSymbol = method.ReturnType.GetMemberTypeArguments().Single().InternalSubstituteTypeParameters(typeMap)
                    Dim builderType As NamedTypeSymbol = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T).Construct(resultType)
                    Dim task As PropertySymbol = F.WellKnownMember(Of PropertySymbol)(WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Task).AsMember(builderType)

                    Return Create(F:=F,
                                  builderType:=builderType,
                                  resultType:=resultType,
                                  setException:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetException,
                                  setResult:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetResult,
                                  awaitOnCompleted:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitOnCompleted,
                                  awaitUnsafeOnCompleted:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitUnsafeOnCompleted,
                                  start:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Start_T,
                                  setStateMachine:=WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetStateMachine,
                                  task:=task)
                End If

                Throw Contract.Unreachable
            End Function

            Private Shared Function Create(F As SyntheticBoundNodeFactory,
                                           builderType As NamedTypeSymbol,
                                           resultType As TypeSymbol,
                                           setException As WellKnownMember,
                                           setResult As WellKnownMember,
                                           awaitOnCompleted As WellKnownMember,
                                           awaitUnsafeOnCompleted As WellKnownMember,
                                           start As WellKnownMember,
                                           setStateMachine As WellKnownMember,
                                           task As PropertySymbol) As AsyncMethodBuilderMemberCollection

                Return New AsyncMethodBuilderMemberCollection(
                                builderType:=builderType,
                                resultType:=resultType,
                                setException:=F.WellKnownMember(Of MethodSymbol)(setException).AsMember(builderType),
                                setResult:=F.WellKnownMember(Of MethodSymbol)(setResult).AsMember(builderType),
                                awaitOnCompleted:=F.WellKnownMember(Of MethodSymbol)(awaitOnCompleted).AsMember(builderType),
                                awaitUnsafeOnCompleted:=F.WellKnownMember(Of MethodSymbol)(awaitUnsafeOnCompleted).AsMember(builderType),
                                start:=F.WellKnownMember(Of MethodSymbol)(start).AsMember(builderType),
                                setStateMachine:=F.WellKnownMember(Of MethodSymbol)(setStateMachine).AsMember(builderType),
                                task:=task)
            End Function

        End Structure

    End Class

End Namespace
