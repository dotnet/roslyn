// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal abstract class VariableSlotAllocator
    {
        public abstract void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder);

        public abstract LocalDefinition? GetPreviousLocal(
            Cci.ITypeReference type,
            ILocalSymbolInternal symbol,
            string? name,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            LocalVariableAttributes pdbAttributes,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags,
            ImmutableArray<string> tupleElementNames);

        public abstract string? PreviousStateMachineTypeName { get; }

        /// <summary>
        /// Returns an index of a slot that stores specified hoisted local variable in the previous generation.
        /// </summary>
        public abstract bool TryGetPreviousHoistedLocalSlotIndex(
            SyntaxNode currentDeclarator,
            Cci.ITypeReference currentType,
            SynthesizedLocalKind synthesizedKind,
            LocalDebugId currentId,
            DiagnosticBag diagnostics,
            out int slotIndex);

        /// <summary>
        /// Number of slots reserved for hoisted local variables.
        /// </summary>
        /// <remarks>
        /// Some of the slots might not be used anymore (a variable might have been deleted or its type changed).
        /// Still, new hoisted variables are assigned slots starting with <see cref="PreviousHoistedLocalSlotCount"/>.
        /// </remarks>
        public abstract int PreviousHoistedLocalSlotCount { get; }

        /// <summary>
        /// Returns true and an index of a slot that stores an awaiter of a specified type in the previous generation, if any. 
        /// </summary>
        public abstract bool TryGetPreviousAwaiterSlotIndex(Cci.ITypeReference currentType, DiagnosticBag diagnostics, out int slotIndex);

        /// <summary>
        /// Number of slots reserved for awaiters.
        /// </summary>
        /// <remarks>
        /// Some of the slots might not be used anymore (the type of an awaiter might have changed).
        /// Still, new awaiters are assigned slots starting with <see cref="PreviousAwaiterSlotCount"/>.
        /// </remarks>
        public abstract int PreviousAwaiterSlotCount { get; }

        /// <summary>
        /// The id of the method, or null if the method wasn't assigned one.
        /// </summary>
        public abstract DebugId? MethodId { get; }

        /// <summary>
        /// Finds a closure in the previous generation that corresponds to the specified syntax.
        /// </summary>
        /// <param name="closureSyntax">Syntax of the closure scope.</param>
        /// <param name="parentClosureId">Id of the parent closure.</param>
        /// <param name="structCaptures">Names of variables hoisted into the closure, or default if the closure is a class.</param>
        /// <param name="closureId">Id of the closure assigned in the generation that defined the closure.</param>
        /// <param name="runtimeRudeEdit">
        /// Not-null if the previous closure is found, but is incompatible with the shape of the closure in the current source.
        /// The previous closure can't be reused (a new one needs to be emitted) and IL bodies of lambdas of the previous closure are updated to throw an exception.
        /// The exception message is specified in <see cref="RuntimeRudeEdit.Message"/>.
        /// </param>
        /// <remarks>
        /// See LambdaFrame.AssertIsLambdaScopeSyntax for kinds of syntax nodes that represent closures.
        /// </remarks>
        public abstract bool TryGetPreviousClosure(SyntaxNode closureSyntax, DebugId? parentClosureId, ImmutableArray<string> structCaptures, out DebugId closureId, out RuntimeRudeEdit? runtimeRudeEdit);

        /// <summary>
        /// Finds a lambda in the previous generation that corresponds to the specified syntax.
        /// </summary>
        /// <param name="lambdaOrLambdaBodySyntax">Syntax of the lambda or its body.</param>
        /// <param name="isLambdaBody">True if <paramref name="lambdaOrLambdaBodySyntax"/> is a lambda body syntax, false if it is a lambda syntax.</param>
        /// <param name="closureOrdinal">The ordinal of the closure the lambda is emitted to.</param>
        /// <param name="lambdaId">Id of the lambda assigned in the generation that defined the lambda.</param>
        /// <param name="runtimeRudeEdit">
        /// Not-null if the previous lambda is found, but is incompatible with the shape of the lambda in the current source.
        /// The previous lambda can't be reused (a new one needs to be emitted) and the previous lambda IL body is updated to throw an exception.
        /// The exception message is specified in <see cref="RuntimeRudeEdit.Message"/>.
        /// </param>
        public abstract bool TryGetPreviousLambda(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, int closureOrdinal, ImmutableArray<DebugId> structClosureIds, out DebugId lambdaId, out RuntimeRudeEdit? runtimeRudeEdit);

        /// <summary>
        /// State number to be used for next state of the state machine,
        /// or <see langword="null"/> if none of the previous versions of the method was a state machine with an increasing state
        /// </summary>
        /// <param name="increasing">True if the state number increases with progress, false if it decreases (e.g. states for iterator try-finally blocks, or iterator states of async iterators).</param>
        public abstract StateMachineState? GetFirstUnusedStateMachineState(bool increasing);

        /// <summary>
        /// For a given node associated with entering a state of a state machine in the new compilation,
        /// returns the ordinal of the corresponding state in the previous version of the state machine.
        /// </summary>
        /// <param name="syntax">Await expression, await foreach statement, yield return statement, or try block syntax node.</param>
        /// <returns>
        /// True if there is a corresponding node in the previous code version that matches the given <paramref name="syntax"/>.
        /// </returns>
        public abstract bool TryGetPreviousStateMachineState(SyntaxNode syntax, AwaitDebugId awaitId, out StateMachineState state);
    }
}
