// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal abstract class VariableSlotAllocator
    {
        public abstract void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder);

        public abstract LocalDefinition GetPreviousLocal(
            Cci.ITypeReference type,
            ILocalSymbolInternal symbol,
            string? nameOpt,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            LocalVariableAttributes pdbAttributes,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags,
            ImmutableArray<string> tupleElementNames);

        public abstract string PreviousStateMachineTypeName { get; }

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
        /// <remarks>
        /// See LambdaFrame.AssertIsLambdaScopeSyntax for kinds of syntax nodes that represent closures.
        /// </remarks>
        public abstract bool TryGetPreviousClosure(SyntaxNode closureSyntax, out DebugId closureId);

        /// <summary>
        /// Finds a lambda in the previous generation that corresponds to the specified syntax.
        /// The <paramref name="lambdaOrLambdaBodySyntax"/> is either a lambda syntax (<paramref name="isLambdaBody"/> is false),
        /// or lambda body syntax (<paramref name="isLambdaBody"/> is true).
        /// </summary>
        public abstract bool TryGetPreviousLambda(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, out DebugId lambdaId);
    }
}
