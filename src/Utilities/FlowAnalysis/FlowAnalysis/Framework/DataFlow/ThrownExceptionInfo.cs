// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public sealed class ThrownExceptionInfo : IEquatable<ThrownExceptionInfo?>
    {
        private ThrownExceptionInfo(
            BasicBlock block,
            INamedTypeSymbol exceptionType,
            ImmutableStack<IOperation>? interproceduralCallStackOpt,
            bool isDefaultExceptionForExceptionsPathAnalysis)
        {
            BasicBlockOrdinal = block.Ordinal;
            HandlingCatchRegionOpt = GetHandlerRegion(block, exceptionType);
            ContainingFinallyRegionOpt = block.GetContainingRegionOfKind(ControlFlowRegionKind.Finally);
            ExceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
            InterproceduralCallStack = interproceduralCallStackOpt ?? ImmutableStack<IOperation>.Empty;
            IsDefaultExceptionForExceptionsPathAnalysis = isDefaultExceptionForExceptionsPathAnalysis;
        }

        internal static ThrownExceptionInfo Create(BasicBlock block, INamedTypeSymbol exceptionType, ImmutableStack<IOperation>? interproceduralCallStackOpt)
        {
            return new ThrownExceptionInfo(block, exceptionType, interproceduralCallStackOpt, isDefaultExceptionForExceptionsPathAnalysis: false);
        }

        internal static ThrownExceptionInfo CreateDefaultInfoForExceptionsPathAnalysis(BasicBlock block, WellKnownTypeProvider wellKnownTypeProvider, ImmutableStack<IOperation>? interproceduralCallStackOpt)
        {
            var exceptionNamedType = wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemException);
            RoslynDebug.Assert(exceptionNamedType != null);
            return new ThrownExceptionInfo(block, exceptionNamedType, interproceduralCallStackOpt, isDefaultExceptionForExceptionsPathAnalysis: true);
        }

        private static ControlFlowRegion? GetHandlerRegion(BasicBlock block, INamedTypeSymbol exceptionType)
        {
            var enclosingRegion = block.EnclosingRegion;
            while (enclosingRegion != null)
            {
                if (enclosingRegion.Kind == ControlFlowRegionKind.TryAndCatch)
                {
                    Debug.Assert(enclosingRegion.NestedRegions[0].Kind == ControlFlowRegionKind.Try);
                    foreach (var nestedRegion in enclosingRegion.NestedRegions.Skip(1))
                    {
                        if (nestedRegion.Kind == ControlFlowRegionKind.Catch &&
                            (nestedRegion.ExceptionType == null ||
                             nestedRegion.ExceptionType.SpecialType == SpecialType.System_Object ||
                             exceptionType.DerivesFrom(nestedRegion.ExceptionType, baseTypesOnly: true)))
                        {
                            return nestedRegion;
                        }
                    }
                }

                enclosingRegion = enclosingRegion.EnclosingRegion;
            }

            return null;
        }

        internal ThrownExceptionInfo With(BasicBlock block, ImmutableStack<IOperation>? interproceduralCallStackOpt)
        {
            Debug.Assert(interproceduralCallStackOpt != InterproceduralCallStack);
            return new ThrownExceptionInfo(block, ExceptionType, interproceduralCallStackOpt, IsDefaultExceptionForExceptionsPathAnalysis);
        }

        /// <summary>
        /// Ordinal of the basic block where this exception is thrown.
        /// </summary>
        internal int BasicBlockOrdinal { get; }

        /// <summary>
        /// Optional catch handler that handles this exception.
        /// </summary>
        internal ControlFlowRegion? HandlingCatchRegionOpt { get; }

        /// <summary>
        /// If the exception happens within a finally region, this points to that finally.
        /// </summary>
        internal ControlFlowRegion? ContainingFinallyRegionOpt { get; }

        internal INamedTypeSymbol ExceptionType { get; }
        internal ImmutableStack<IOperation> InterproceduralCallStack { get; }
        internal bool IsDefaultExceptionForExceptionsPathAnalysis { get; }

        public bool Equals(ThrownExceptionInfo? other)
        {
            return other != null &&
                BasicBlockOrdinal == other.BasicBlockOrdinal &&
                HandlingCatchRegionOpt == other.HandlingCatchRegionOpt &&
                ContainingFinallyRegionOpt == other.ContainingFinallyRegionOpt &&
                Equals(ExceptionType, other.ExceptionType) &&
                InterproceduralCallStack.SequenceEqual(other.InterproceduralCallStack) &&
                IsDefaultExceptionForExceptionsPathAnalysis == other.IsDefaultExceptionForExceptionsPathAnalysis;
        }

        public override bool Equals(object obj)
            => Equals(obj as ThrownExceptionInfo);

        public override int GetHashCode()
            => HashUtilities.Combine(InterproceduralCallStack,
                HashUtilities.Combine(BasicBlockOrdinal.GetHashCodeOrDefault(),
                HashUtilities.Combine(HandlingCatchRegionOpt.GetHashCodeOrDefault(),
                HashUtilities.Combine(ContainingFinallyRegionOpt.GetHashCodeOrDefault(),
                HashUtilities.Combine(ExceptionType.GetHashCode(), IsDefaultExceptionForExceptionsPathAnalysis.GetHashCode())))));
    }
}
