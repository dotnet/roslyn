// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public sealed class ThrownExceptionInfo : IEquatable<ThrownExceptionInfo?>
    {
        private ThrownExceptionInfo(
            BasicBlock block,
            INamedTypeSymbol exceptionType,
            ImmutableStack<IOperation>? interproceduralCallStack,
            bool isDefaultExceptionForExceptionsPathAnalysis)
        {
            BasicBlockOrdinal = block.Ordinal;
            HandlingCatchRegion = GetHandlerRegion(block, exceptionType);
            ContainingFinallyRegion = block.GetContainingRegionOfKind(ControlFlowRegionKind.Finally);
            ExceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
            InterproceduralCallStack = interproceduralCallStack ?? ImmutableStack<IOperation>.Empty;
            IsDefaultExceptionForExceptionsPathAnalysis = isDefaultExceptionForExceptionsPathAnalysis;
        }

        internal static ThrownExceptionInfo Create(BasicBlock block, INamedTypeSymbol exceptionType, ImmutableStack<IOperation>? interproceduralCallStack)
        {
            return new ThrownExceptionInfo(block, exceptionType, interproceduralCallStack, isDefaultExceptionForExceptionsPathAnalysis: false);
        }

        internal static ThrownExceptionInfo CreateDefaultInfoForExceptionsPathAnalysis(BasicBlock block, WellKnownTypeProvider wellKnownTypeProvider, ImmutableStack<IOperation>? interproceduralCallStack)
        {
            var exceptionNamedType = wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemException);
            RoslynDebug.Assert(exceptionNamedType != null);
            return new ThrownExceptionInfo(block, exceptionNamedType, interproceduralCallStack, isDefaultExceptionForExceptionsPathAnalysis: true);
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

        internal ThrownExceptionInfo With(BasicBlock block, ImmutableStack<IOperation>? interproceduralCallStack)
        {
            Debug.Assert(interproceduralCallStack != InterproceduralCallStack);
            return new ThrownExceptionInfo(block, ExceptionType, interproceduralCallStack, IsDefaultExceptionForExceptionsPathAnalysis);
        }

        /// <summary>
        /// Ordinal of the basic block where this exception is thrown.
        /// </summary>
        internal int BasicBlockOrdinal { get; }

        /// <summary>
        /// Optional catch handler that handles this exception.
        /// </summary>
        internal ControlFlowRegion? HandlingCatchRegion { get; }

        /// <summary>
        /// If the exception happens within a finally region, this points to that finally.
        /// </summary>
        internal ControlFlowRegion? ContainingFinallyRegion { get; }

        internal INamedTypeSymbol ExceptionType { get; }
        internal ImmutableStack<IOperation> InterproceduralCallStack { get; }
        internal bool IsDefaultExceptionForExceptionsPathAnalysis { get; }

        public bool Equals(ThrownExceptionInfo? other)
        {
            return other != null &&
                BasicBlockOrdinal == other.BasicBlockOrdinal &&
                HandlingCatchRegion == other.HandlingCatchRegion &&
                ContainingFinallyRegion == other.ContainingFinallyRegion &&
                SymbolEqualityComparer.Default.Equals(ExceptionType, other.ExceptionType) &&
                InterproceduralCallStack.SequenceEqual(other.InterproceduralCallStack) &&
                IsDefaultExceptionForExceptionsPathAnalysis == other.IsDefaultExceptionForExceptionsPathAnalysis;
        }

        public override bool Equals(object obj)
            => Equals(obj as ThrownExceptionInfo);

        public override int GetHashCode()
        {
            var hashCode = new RoslynHashCode();
            HashUtilities.Combine(InterproceduralCallStack, ref hashCode);
            hashCode.Add(BasicBlockOrdinal.GetHashCode());
            hashCode.Add(HandlingCatchRegion.GetHashCodeOrDefault());
            hashCode.Add(ContainingFinallyRegion.GetHashCodeOrDefault());
            hashCode.Add(ExceptionType.GetHashCode());
            hashCode.Add(IsDefaultExceptionForExceptionsPathAnalysis.GetHashCode());
            return hashCode.ToHashCode();
        }
    }
}
