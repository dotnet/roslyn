// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Async methods have both a return type (void, Task, or Task&lt;T&gt;) and a 'result' type, which is the
    /// operand type of any return expressions in the async method. The result type is void in the case of
    /// Task-returning and void-returning async methods, and T in the case of Task&lt;T&gt;-returning async
    /// methods.
    /// 
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
        /// Create an instance of the method builder.
        /// </summary>
        internal readonly MethodSymbol CreateBuilder;

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

        private AsyncMethodBuilderMemberCollection(
            NamedTypeSymbol builderType,
            TypeSymbol resultType,
            MethodSymbol createBuilder,
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
            CreateBuilder = createBuilder;
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
                    F,
                    builderType: F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder),
                    resultType: F.SpecialType(SpecialType.System_Void),
                    createBuilderMethod: null,
                    createBuilder: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Create,
                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetStateMachine,
                    task: WellKnownMember.Count,
                    collection: out collection);
            }

            if (method.IsTaskReturningAsync(F.Compilation))
            {
                MethodSymbol createBuilderMethod;
                var builderType = ((NamedTypeSymbol)method.ReturnType).GetAsyncMethodBuilderType(out createBuilderMethod) ??
                    F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder);
                return TryCreate(
                    F,
                    builderType: builderType,
                    resultType: F.SpecialType(SpecialType.System_Void),
                    createBuilderMethod: createBuilderMethod,
                    createBuilder: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Create,
                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine,
                    task: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Task,
                    collection: out collection);
            }

            if (method.IsGenericTaskReturningAsync(F.Compilation))
            {
                MethodSymbol createBuilderMethod;
                var builderType = ((NamedTypeSymbol)method.ReturnType).GetAsyncMethodBuilderType(out createBuilderMethod) ??
                    F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T);
                var resultType = method.ReturnType.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();
                if (resultType.IsDynamic())
                {
                    resultType = F.SpecialType(SpecialType.System_Object);
                }
                if (typeMap != null)
                {
                    resultType = typeMap.SubstituteType(resultType).Type;
                }
                return TryCreate(
                    F,
                    builderType: builderType.ConstructedFrom.Construct(resultType),
                    resultType: resultType,
                    createBuilderMethod: createBuilderMethod,
                    createBuilder: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Create,
                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetStateMachine,
                    task: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Task,
                    collection: out collection);
            }

            throw ExceptionUtilities.UnexpectedValue(method);
        }

        private static bool TryCreate(
            SyntheticBoundNodeFactory F,
            NamedTypeSymbol builderType,
            TypeSymbol resultType,
            MethodSymbol createBuilderMethod,
            WellKnownMember createBuilder,
            WellKnownMember setException,
            WellKnownMember setResult,
            WellKnownMember awaitOnCompleted,
            WellKnownMember awaitUnsafeOnCompleted,
            WellKnownMember start,
            WellKnownMember setStateMachine,
            WellKnownMember task,
            out AsyncMethodBuilderMemberCollection collection)
        {
            bool customBuilder = (object)createBuilderMethod != null;

            MethodSymbol setExceptionMethod;
            MethodSymbol setResultMethod;
            MethodSymbol awaitOnCompletedMethod;
            MethodSymbol awaitUnsafeOnCompletedMethod;
            MethodSymbol startMethod;
            MethodSymbol setStateMachineMethod;
            PropertySymbol taskProperty;

            if ((customBuilder || TryGetBuilderMember(F, createBuilder, builderType, customBuilder, out createBuilderMethod)) &&
                TryGetBuilderMember(F, setException, builderType, customBuilder, out setExceptionMethod) &&
                TryGetBuilderMember(F, setResult, builderType, customBuilder, out setResultMethod) &&
                TryGetBuilderMember(F, awaitOnCompleted, builderType, customBuilder, out awaitOnCompletedMethod) &&
                TryGetBuilderMember(F, awaitUnsafeOnCompleted, builderType, customBuilder, out awaitUnsafeOnCompletedMethod) &&
                TryGetBuilderMember(F, start, builderType, customBuilder, out startMethod) &&
                TryGetBuilderMember(F, setStateMachine, builderType, customBuilder, out setStateMachineMethod) &&
                TryGetBuilderMember(F, task, builderType, customBuilder, out taskProperty))
            {
                collection = new AsyncMethodBuilderMemberCollection(
                    builderType,
                    resultType,
                    createBuilderMethod,
                    setExceptionMethod,
                    setResultMethod,
                    awaitOnCompletedMethod,
                    awaitUnsafeOnCompletedMethod,
                    startMethod,
                    setStateMachineMethod,
                    taskProperty);

                return true;
            }

            collection = default(AsyncMethodBuilderMemberCollection);
            return false;
        }

        private static bool TryGetBuilderMember<TSymbol>(
            SyntheticBoundNodeFactory F,
            WellKnownMember member,
            NamedTypeSymbol builderType,
            bool customBuilder,
            out TSymbol symbol)
            where TSymbol : Symbol
        {
            if (member == WellKnownMember.Count)
            {
                symbol = null;
                return true;
            }
            if (customBuilder)
            {
                var descriptor = WellKnownMembers.GetDescriptor(member);
                symbol = GetMember(builderType, descriptor) as TSymbol;
            }
            else
            {
                symbol = F.WellKnownMember(member, isOptional: true) as TSymbol;
                if ((object)symbol != null)
                {
                    symbol = (TSymbol)symbol.SymbolAsMember(builderType);
                }
            }
            if ((object)symbol == null)
            {
                var descriptor = WellKnownMembers.GetDescriptor(member);
                var diagnostic = new CSDiagnostic(
                    new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, (customBuilder ? (object)builderType : descriptor.DeclaringTypeMetadataName), descriptor.Name),
                    F.Syntax.Location);
                F.Diagnostics.Add(diagnostic);
                return false;
            }
            return true;
        }

        private static Symbol GetMember(NamedTypeSymbol containingType, MemberDescriptor descriptor)
        {
            // PROTOTYPE(tasklike): Look on base types.
            // PROTOTYPE(tasklike): Compare kind.
            // PROTOTYPE(tasklike): Compare signatures.
            // PROTOTYPE(tasklike): Compare constraints.
            var members = containingType.GetMembers(descriptor.Name);
            foreach (var member in members)
            {
                switch (member.Kind)
                {
                    case SymbolKind.Method:
                    case SymbolKind.Property:
                        if ((member.DeclaredAccessibility == Accessibility.Public) &&
                            member.IsStatic == ((descriptor.Flags & MemberFlags.Static) != 0))
                        {
                            return member;
                        }
                        break;
                }
            }
            return null;
        }
    }
}
