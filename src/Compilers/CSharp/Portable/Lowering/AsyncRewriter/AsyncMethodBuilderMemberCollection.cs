// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.RuntimeMembers;
using static Microsoft.CodeAnalysis.CSharp.CSharpCompilation;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Async methods have both a return type (void, nongeneric Tasklike, or generic Tasklike&lt;T&gt;)
    /// and a 'result' type, which is the
    /// operand type of any return expressions in the async method. The result type is void in the case of
    /// Nongeneric Tasklike-returning and void-returning async methods, and T in the case of 
    /// generic Tasklike&lt;T&gt;-returning async     /// methods.
    /// 
    /// The async method builder to be used is picked up by the compiler based on the [Tasklike]
    /// attribute of the return type of the async method if it has one. If the return type
    /// is void, or the return type lacks this attribute and is one of Task/Task&lt;T&gt; then
    /// System.Runtime.CompilerServices provides a collection of async method builders that are used in the
    /// generated code of async methods to create and manipulate the async method's task. There are three
    /// distinct async method builder types, one of each async return type: AsyncVoidMethodBuilder,
    /// AsyncTaskMethodBuilder, and AsyncTaskMethodBuilder&lt;T&gt;. 
    /// 
    /// AsyncMethodBuilderMemberCollection provides a common mechanism for accessing the well-known members of
    /// each async method builder type. This avoids having to inspect the return style of the current async method
    /// to pick the right async method builder member during async rewriting.
    /// </summary>
    internal struct AsyncMethodBuilderMemberCollection
    {
        /// <summary>
        /// The builder's constructed type.
        /// </summary>
        internal readonly NamedTypeSymbol BuilderType;

        /// <summary>
        /// The result type of the constructed task: T for Task&lt;T&gt;, void otherwise.
        /// </summary>
        internal readonly TypeSymbol ResultType;

        /// <summary>
        /// Binds an exception to the method builder.
        /// </summary>
        internal readonly MethodSymbol SetException;

        /// <summary>
        /// Marks the method builder as successfully completed, and sets the result if method is Task&lt;T&gt;-returning.
        /// </summary>
        internal readonly MethodSymbol SetResult;

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        internal readonly MethodSymbol AwaitOnCompleted;

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes. This method can be called from partially trusted code.
        /// </summary>
        internal readonly MethodSymbol AwaitUnsafeOnCompleted;

        /// <summary>
        /// Begins running the builder with the associated state machine.
        /// </summary>
        internal readonly MethodSymbol Start;

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        internal readonly MethodSymbol SetStateMachine;

        /// <summary>
        /// Get the constructed task for a Task-returning or Task&lt;T&gt;-returning async method.
        /// </summary>
        internal readonly PropertySymbol Task;

        internal AsyncMethodBuilderMemberCollection(
            NamedTypeSymbol builderType,
            TypeSymbol resultType,
            MethodSymbol setException,
            MethodSymbol setResult,
            MethodSymbol awaitOnCompleted,
            MethodSymbol awaitUnsafeOnCompleted,
            MethodSymbol start,
            MethodSymbol setStateMachine,
            PropertySymbol task)
        {
            BuilderType = builderType;
            ResultType = resultType;
            SetException = setException;
            SetResult = setResult;
            AwaitOnCompleted = awaitOnCompleted;
            AwaitUnsafeOnCompleted = awaitUnsafeOnCompleted;
            Start = start;
            SetStateMachine = setStateMachine;
            Task = task;
        }

        internal static bool TryCreate(SyntheticBoundNodeFactory F, MethodSymbol method, TypeMap typeMap, out AsyncMethodBuilderMemberCollection collection)
        {
            if (method.IsVoidReturningAsync())
            {
                return TryCreate(
                    F: F,

                    builderType: F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder),
                    resultType: F.SpecialType(SpecialType.System_Void),

                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetStateMachine,
                    task: null,
                    requireWellKnownType:true,
                    collection: out collection);
            }

            NamedTypeSymbol builderType = method.GetTasklikeBuilderFromAttribute(F.Compilation);
            if (builderType != null)
            {
                System.Diagnostics.Debug.Assert(method.ReturnType.GetArity() <= 1, "By lowering-async-method time, arity of return type should be 0 or 1");
                if (builderType.Arity != method.ReturnType.GetArity() || builderType.Arity > 1)
                {
                    // TASKLIKE todo: verify that this does emit an error somewhere or other
                    collection = default(AsyncMethodBuilderMemberCollection);
                    return false;
                }
                else if (builderType.Arity == 0)
                {

                    // TASKLIKE todo: verify that failures to bind to the pattern result in an error
                    PropertySymbol task = null;
                    foreach (var member in builderType.GetMembers())
                    {
                        if (member.Name == "Task")
                        {
                            task = member as PropertySymbol;
                            if (task == null || task.IsStatic || task.DeclaredAccessibility != Accessibility.Public || task.IsIndexedProperty()
                                || task.Type != method.ReturnType)
                            {
                                task = null;
                            }
                            break;
                        }
                    }


                    return TryCreate(
                        F: F,

                        builderType: builderType, // TASKLIKE: does this have to be generic-instantiated? is it already? is it open?
                        resultType: F.SpecialType(SpecialType.System_Void),

                        setException: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetException,
                        setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult,
                        awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted,
                        awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted,
                        start: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T,
                        setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine,
                        task: task,
                        requireWellKnownType: false,
                        collection: out collection);
                }
                else
                {
                    throw new System.NotImplementedException("TASKLIKE generics");
                }

            }

            if (method.IsNongenericTasklikeReturningAsync(F.Compilation))
            {
               builderType = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder);

                PropertySymbol task;
                if (!TryGetWellKnownPropertyAsMember(F, WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Task, builderType, true, out task))
                {
                    collection = default(AsyncMethodBuilderMemberCollection);
                    return false;
                }

                return TryCreate(
                    F: F,

                    builderType: F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder),
                    resultType: F.SpecialType(SpecialType.System_Void),

                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine,
                    task: task,
                    requireWellKnownType: true,
                    collection: out collection);
            }

            if (method.IsGenericTasklikeReturningAsync(F.Compilation))
            {
                TypeSymbol resultType = method.ReturnType.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();

                if (resultType.IsDynamic())
                {
                    resultType = F.SpecialType(SpecialType.System_Object);
                }

                if (typeMap != null)
                {
                    resultType = typeMap.SubstituteType(resultType).Type;
                }

                builderType = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T).Construct(resultType);

                PropertySymbol task;
                if (!TryGetWellKnownPropertyAsMember(F, WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Task, builderType, true, out task))
                {
                    collection = default(AsyncMethodBuilderMemberCollection);
                    return false;
                }

                return TryCreate(
                    F: F,

                    builderType: builderType,
                    resultType: resultType,

                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetStateMachine,
                    task: task,
                    requireWellKnownType: true,
                    collection: out collection);
            }

            throw ExceptionUtilities.UnexpectedValue(method);
        }

        private static bool TryCreate(
            SyntheticBoundNodeFactory F,
            NamedTypeSymbol builderType,
            TypeSymbol resultType,
            WellKnownMember setException,
            WellKnownMember setResult,
            WellKnownMember awaitOnCompleted,
            WellKnownMember awaitUnsafeOnCompleted,
            WellKnownMember start,
            WellKnownMember setStateMachine,
            PropertySymbol task,
            bool requireWellKnownType,
            out AsyncMethodBuilderMemberCollection collection)
        {
            MethodSymbol setExceptionMethod;
            MethodSymbol setResultMethod;
            MethodSymbol awaitOnCompletedMethod;
            MethodSymbol awaitUnsafeOnCompletedMethod;
            MethodSymbol startMethod;
            MethodSymbol setStateMachineMethod;

            if (task == null)
            {
                collection = default(AsyncMethodBuilderMemberCollection);
                return false;
            }

            if (TryGetWellKnownMethodAsMember(F, setException, builderType, requireWellKnownType, out setExceptionMethod) &&
                TryGetWellKnownMethodAsMember(F, setResult, builderType, requireWellKnownType, out setResultMethod) &&
                TryGetWellKnownMethodAsMember(F, awaitOnCompleted, builderType, requireWellKnownType, out awaitOnCompletedMethod) &&
                TryGetWellKnownMethodAsMember(F, awaitUnsafeOnCompleted, builderType, requireWellKnownType, out awaitUnsafeOnCompletedMethod) &&
                TryGetWellKnownMethodAsMember(F, start, builderType, requireWellKnownType, out startMethod) &&
                TryGetWellKnownMethodAsMember(F, setStateMachine, builderType, requireWellKnownType, out setStateMachineMethod))
            {
                collection = new AsyncMethodBuilderMemberCollection(
                    builderType,
                    resultType,
                    setExceptionMethod,
                    setResultMethod,
                    awaitOnCompletedMethod,
                    awaitUnsafeOnCompletedMethod,
                    startMethod,
                    setStateMachineMethod,
                    task);

                return true;
            }

            collection = default(AsyncMethodBuilderMemberCollection);
            return false;
        }

        private static bool TryGetWellKnownMethodAsMember(SyntheticBoundNodeFactory F, WellKnownMember wellKnownMethod, NamedTypeSymbol containingType, bool requireWellKnownType, out MethodSymbol methodSymbol)
        {
            if (requireWellKnownType)
            {
                methodSymbol = F.WellKnownMember(wellKnownMethod) as MethodSymbol;
            }
            else
            {
                MemberDescriptor descriptor = WellKnownMembers.GetDescriptor(wellKnownMethod);
                methodSymbol = GetRuntimeMember(containingType, ref descriptor, SpecialMembersSignatureComparer.Instance, accessWithinOpt: null) as MethodSymbol;
                // TASKLIKE todo: this is failing. 
                // Apparently my ValueTask SetException signature "void SetException(System.Exception ex)"
                // didn't match the descriptor for some reason.

            }
            if ((object)methodSymbol == null) return false;

            methodSymbol = methodSymbol.AsMember(containingType);
            return true;
        }

        private static bool TryGetWellKnownPropertyAsMember(SyntheticBoundNodeFactory F, WellKnownMember wellKnownProperty, NamedTypeSymbol containingType, bool requireWellKnownType, out PropertySymbol propertySymbol)
        {
            if (requireWellKnownType)
            {
                propertySymbol = F.WellKnownMember(wellKnownProperty) as PropertySymbol;
            }
            else
            {
                MemberDescriptor descriptor = WellKnownMembers.GetDescriptor(wellKnownProperty);
                propertySymbol = GetRuntimeMember(containingType, ref descriptor, SpecialMembersSignatureComparer.Instance, accessWithinOpt: null) as PropertySymbol;
            }

            if ((object)propertySymbol == null) return false;

            propertySymbol = propertySymbol.AsMember(containingType);
            return true;
        }


    }
}
