// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
    internal readonly struct AsyncMethodBuilderMemberCollection
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

        /// <summary>
        /// True if generic method constraints should be checked at the call-site.
        /// </summary>
        internal readonly bool CheckGenericMethodConstraints;

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
            PropertySymbol task,
            bool checkGenericMethodConstraints)
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
            CheckGenericMethodConstraints = checkGenericMethodConstraints;
        }

        internal static bool TryCreate(SyntheticBoundNodeFactory F, MethodSymbol method, TypeMap typeMap, out AsyncMethodBuilderMemberCollection collection)
        {
            if (method.IsIterator)
            {
                var builderType = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder);
                Debug.Assert((object)builderType != null);

                TryGetBuilderMember<MethodSymbol>(
                    F,
                    WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Create,
                    builderType,
                    customBuilder: false,
                    out MethodSymbol createBuilderMethod);

                if (createBuilderMethod is null)
                {
                    collection = default;
                    return false;
                }

                return TryCreate(
                    F,
                    customBuilder: false,
                    builderType: builderType,
                    resultType: F.SpecialType(SpecialType.System_Void),
                    createBuilderMethod: createBuilderMethod,
                    taskProperty: null,
                    setException: null, // unused
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Complete, // AsyncIteratorMethodBuilder.Complete is the corresponding method to AsyncTaskMethodBuilder.SetResult
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__MoveNext_T,
                    setStateMachine: null, // unused
                    collection: out collection);
            }

            if (method.IsAsyncReturningVoid())
            {
                var builderType = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder);
                Debug.Assert((object)builderType != null);
                MethodSymbol createBuilderMethod;
                bool customBuilder = false;
                TryGetBuilderMember<MethodSymbol>(
                    F,
                    WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Create,
                    builderType,
                    customBuilder,
                    out createBuilderMethod);
                if ((object)createBuilderMethod == null)
                {
                    collection = default;
                    return false;
                }
                return TryCreate(
                    F,
                    customBuilder: customBuilder,
                    builderType: builderType,
                    resultType: F.SpecialType(SpecialType.System_Void),
                    createBuilderMethod: createBuilderMethod,
                    taskProperty: null,
                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetStateMachine,
                    collection: out collection);
            }

            TypeSymbol methodLevelBuilder = null;
            if (method.IsAsyncEffectivelyReturningTask(F.Compilation))
            {
                var returnType = (NamedTypeSymbol)method.ReturnType;
                NamedTypeSymbol builderType;
                MethodSymbol createBuilderMethod = null;
                PropertySymbol taskProperty = null;
                bool useMethodLevelBuilder = method.HasAsyncMethodBuilderAttribute(out methodLevelBuilder);
                bool customBuilder;
                TypeSymbol builderArgument;

                if (useMethodLevelBuilder)
                {
                    customBuilder = true;
                    builderArgument = methodLevelBuilder;
                }
                else
                {
                    customBuilder = returnType.IsCustomTaskType(out builderArgument);
                }

                if (customBuilder)
                {
                    builderType = ValidateBuilderType(F, builderArgument, returnType.DeclaredAccessibility, isGeneric: false, useMethodLevelBuilder);
                    if ((object)builderType != null)
                    {
                        taskProperty = GetCustomTaskProperty(F, builderType, returnType);
                        createBuilderMethod = GetCustomCreateMethod(F, builderType);
                    }
                }
                else
                {
                    builderType = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder);
                    Debug.Assert((object)builderType != null);
                    TryGetBuilderMember<MethodSymbol>(
                        F,
                        WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Create,
                        builderType,
                        customBuilder,
                        out createBuilderMethod);
                    TryGetBuilderMember<PropertySymbol>(
                        F,
                        WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Task,
                        builderType,
                        customBuilder,
                        out taskProperty);
                }

                if ((object)builderType == null ||
                    (object)createBuilderMethod == null ||
                    (object)taskProperty == null)
                {
                    collection = default;
                    return false;
                }

                return TryCreate(
                    F,
                    customBuilder: customBuilder,
                    builderType: builderType,
                    resultType: F.SpecialType(SpecialType.System_Void),
                    createBuilderMethod: createBuilderMethod,
                    taskProperty: taskProperty,
                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine,
                    collection: out collection);
            }

            if (method.IsAsyncEffectivelyReturningGenericTask(F.Compilation))
            {
                var returnType = (NamedTypeSymbol)method.ReturnType;
                var resultType = returnType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single().Type;
                if (resultType.IsDynamic())
                {
                    resultType = F.SpecialType(SpecialType.System_Object);
                }
                if (typeMap != null)
                {
                    resultType = typeMap.SubstituteType(resultType).Type;
                }
                returnType = returnType.ConstructedFrom.Construct(resultType);
                NamedTypeSymbol builderType;
                MethodSymbol createBuilderMethod = null;
                PropertySymbol taskProperty = null;
                bool useMethodLevelBuilder = method.HasAsyncMethodBuilderAttribute(out methodLevelBuilder);
                bool customBuilder;
                TypeSymbol builderArgument;

                if (useMethodLevelBuilder)
                {
                    customBuilder = true;
                    builderArgument = methodLevelBuilder;
                }
                else
                {
                    customBuilder = returnType.IsCustomTaskType(out builderArgument);
                }

                if (customBuilder)
                {
                    builderType = ValidateBuilderType(F, builderArgument, returnType.DeclaredAccessibility, isGeneric: true, useMethodLevelBuilder);
                    if ((object)builderType != null)
                    {
                        builderType = builderType.ConstructedFrom.Construct(resultType);
                        taskProperty = GetCustomTaskProperty(F, builderType, returnType);
                        createBuilderMethod = GetCustomCreateMethod(F, builderType);
                    }
                }
                else
                {
                    builderType = F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T);
                    Debug.Assert((object)builderType != null);
                    builderType = builderType.Construct(resultType);
                    TryGetBuilderMember<MethodSymbol>(
                        F,
                        WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Create,
                        builderType,
                        customBuilder,
                        out createBuilderMethod);
                    TryGetBuilderMember<PropertySymbol>(
                        F,
                        WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Task,
                        builderType,
                        customBuilder,
                        out taskProperty);
                }

                if ((object)builderType == null ||
                    (object)taskProperty == null ||
                    (object)createBuilderMethod == null)
                {
                    collection = default;
                    return false;
                }

                return TryCreate(
                    F,
                    customBuilder: customBuilder,
                    builderType: builderType,
                    resultType: resultType,
                    createBuilderMethod: createBuilderMethod,
                    taskProperty: taskProperty,
                    setException: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetException,
                    setResult: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetResult,
                    awaitOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitOnCompleted,
                    awaitUnsafeOnCompleted: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitUnsafeOnCompleted,
                    start: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Start_T,
                    setStateMachine: WellKnownMember.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetStateMachine,
                    collection: out collection);
            }

            throw ExceptionUtilities.UnexpectedValue(method);
        }

        private static NamedTypeSymbol ValidateBuilderType(SyntheticBoundNodeFactory F, TypeSymbol builderAttributeArgument, Accessibility desiredAccessibility, bool isGeneric, bool forMethodLevelBuilder = false)
        {
            var builderType = builderAttributeArgument as NamedTypeSymbol;

            if ((object)builderType != null &&
                 !builderType.IsErrorType() &&
                 !builderType.IsVoidType() &&
                 (forMethodLevelBuilder || builderType.DeclaredAccessibility == desiredAccessibility))
            {
                if (isGeneric)
                {
                    if (builderType.IsUnboundGenericType && builderType.ContainingType?.IsGenericType != true && builderType.Arity == 1)
                    {
                        return builderType;
                    }
                    else
                    {
                        F.Diagnostics.Add(ErrorCode.ERR_WrongArityAsyncReturn, F.Syntax.Location, builderType);
                        return null;
                    }
                }

                if (!builderType.IsGenericType)
                {
                    return builderType;
                }
            }

            F.Diagnostics.Add(ErrorCode.ERR_BadAsyncReturn, F.Syntax.Location);
            return null;
        }

        private static bool TryCreate(
            SyntheticBoundNodeFactory F,
            bool customBuilder,
            NamedTypeSymbol builderType,
            TypeSymbol resultType,
            MethodSymbol createBuilderMethod,
            PropertySymbol taskProperty,
            WellKnownMember? setException,
            WellKnownMember setResult,
            WellKnownMember awaitOnCompleted,
            WellKnownMember awaitUnsafeOnCompleted,
            WellKnownMember start,
            WellKnownMember? setStateMachine,
            out AsyncMethodBuilderMemberCollection collection)
        {
            MethodSymbol setExceptionMethod;
            MethodSymbol setResultMethod;
            MethodSymbol awaitOnCompletedMethod;
            MethodSymbol awaitUnsafeOnCompletedMethod;
            MethodSymbol startMethod;
            MethodSymbol setStateMachineMethod;

            if (TryGetBuilderMember(F, setException, builderType, customBuilder, out setExceptionMethod) &&
                TryGetBuilderMember(F, setResult, builderType, customBuilder, out setResultMethod) &&
                TryGetBuilderMember(F, awaitOnCompleted, builderType, customBuilder, out awaitOnCompletedMethod) &&
                TryGetBuilderMember(F, awaitUnsafeOnCompleted, builderType, customBuilder, out awaitUnsafeOnCompletedMethod) &&
                TryGetBuilderMember(F, start, builderType, customBuilder, out startMethod) &&
                TryGetBuilderMember(F, setStateMachine, builderType, customBuilder, out setStateMachineMethod))
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
                    taskProperty,
                    checkGenericMethodConstraints: customBuilder);

                return true;
            }

            collection = default;
            return false;
        }

        private static bool TryGetBuilderMember<TSymbol>(
            SyntheticBoundNodeFactory F,
            WellKnownMember? member,
            NamedTypeSymbol builderType,
            bool customBuilder,
            out TSymbol symbol)
            where TSymbol : Symbol
        {
            if (!member.HasValue)
            {
                symbol = null;
                return true;
            }

            WellKnownMember memberValue = member.Value;
            if (customBuilder)
            {
                var descriptor = WellKnownMembers.GetDescriptor(memberValue);
                var sym = CSharpCompilation.GetRuntimeMember(
                    builderType.OriginalDefinition,
                    descriptor,
                    F.Compilation.WellKnownMemberSignatureComparer,
                    accessWithinOpt: null);
                if ((object)sym != null)
                {
                    sym = sym.SymbolAsMember(builderType);
                }
                symbol = sym as TSymbol;
            }
            else
            {
                symbol = F.WellKnownMember(memberValue, isOptional: true) as TSymbol;
                if ((object)symbol != null)
                {
                    symbol = (TSymbol)symbol.SymbolAsMember(builderType);
                }
            }
            if ((object)symbol == null)
            {
                var descriptor = WellKnownMembers.GetDescriptor(memberValue);
                var diagnostic = new CSDiagnostic(
                    new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, (customBuilder ? (object)builderType : descriptor.DeclaringTypeMetadataName), descriptor.Name),
                    F.Syntax.Location);
                F.Diagnostics.Add(diagnostic);
                return false;
            }
            return true;
        }

        private static MethodSymbol GetCustomCreateMethod(
            SyntheticBoundNodeFactory F,
            NamedTypeSymbol builderType)
        {
            // The Create method's return type is expected to be builderType.
            // The WellKnownMembers routines aren't able to enforce that, which is why this method exists.
            const string methodName = "Create";
            var members = builderType.GetMembers(methodName);
            foreach (var member in members)
            {
                if (member.Kind != SymbolKind.Method)
                {
                    continue;
                }
                var method = (MethodSymbol)member;
                if ((method.DeclaredAccessibility == Accessibility.Public) &&
                    method.IsStatic &&
                    method.ParameterCount == 0 &&
                    !method.IsGenericMethod &&
                    method.RefKind == RefKind.None &&
                    method.ReturnType.Equals(builderType, TypeCompareKind.AllIgnoreOptions))
                {
                    return method;
                }
            }
            F.Diagnostics.Add(ErrorCode.ERR_MissingPredefinedMember, F.Syntax.Location, builderType, methodName);
            return null;
        }

        private static PropertySymbol GetCustomTaskProperty(
            SyntheticBoundNodeFactory F,
            NamedTypeSymbol builderType,
            NamedTypeSymbol returnType)
        {
            const string propertyName = "Task";
            var members = builderType.GetMembers(propertyName);
            foreach (var member in members)
            {
                if (member.Kind != SymbolKind.Property)
                {
                    continue;
                }
                var property = (PropertySymbol)member;
                if ((property.DeclaredAccessibility == Accessibility.Public) &&
                    !property.IsStatic &&
                    (property.ParameterCount == 0))
                {
                    if (!property.Type.Equals(returnType, TypeCompareKind.AllIgnoreOptions))
                    {
                        var badTaskProperty = new CSDiagnostic(
                            new CSDiagnosticInfo(ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty, builderType, returnType, property.Type),
                            F.Syntax.Location);
                        F.Diagnostics.Add(badTaskProperty);
                        return null;
                    }

                    return property;
                }
            }
            var diagnostic = new CSDiagnostic(
                new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, builderType, propertyName),
                F.Syntax.Location);
            F.Diagnostics.Add(diagnostic);
            return null;
        }
    }
}
