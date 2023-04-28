﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class ContractWrappers
    {
        public static Contracts.ManagedActiveStatementDebugInfo ToContract(this ManagedActiveStatementDebugInfo info)
            => new(ToContract(info.ActiveInstruction), info.DocumentName, ToContract(info.SourceSpan), (Contracts.ActiveStatementFlags)info.Flags);

        public static Contracts.ManagedInstructionId ToContract(this ManagedInstructionId id)
            => new(ToContract(id.Method), id.ILOffset);

        public static Contracts.ManagedMethodId ToContract(this ManagedMethodId id)
            => new(id.Module, id.Token, id.Version);

        public static Contracts.SourceSpan ToContract(this SourceSpan id)
            => new(id.StartLine, id.StartColumn, id.EndLine, id.EndColumn);

        public static Contracts.ManagedHotReloadAvailability ToContract(this ManagedHotReloadAvailability value)
            => new((Contracts.ManagedHotReloadAvailabilityStatus)value.Status, value.LocalizedMessage);

        public static ManagedHotReloadUpdate FromContract(this Contracts.ManagedHotReloadUpdate update)
            => new(
                module: update.Module,
                ilDelta: update.ILDelta,
                metadataDelta: update.MetadataDelta,
                pdbDelta: update.PdbDelta,
                updatedTypes: update.UpdatedTypes,
                requiredCapabilities: update.RequiredCapabilities,
                updatedMethods: update.UpdatedMethods,
                sequencePoints: update.SequencePoints.SelectAsArray(FromContract),
                activeStatements: update.ActiveStatements.SelectAsArray(FromContract),
                exceptionRegions: update.ExceptionRegions.SelectAsArray(FromContract));

        public static ImmutableArray<ManagedHotReloadUpdate> FromContract(this ImmutableArray<Contracts.ManagedHotReloadUpdate> diagnostics)
            => diagnostics.SelectAsArray(FromContract);

        public static SequencePointUpdates FromContract(this Contracts.SequencePointUpdates updates)
            => new(updates.FileName, updates.LineUpdates.SelectAsArray(FromContract));

        public static SourceLineUpdate FromContract(this Contracts.SourceLineUpdate update)
            => new(update.OldLine, update.NewLine);

        public static ManagedActiveStatementUpdate FromContract(this Contracts.ManagedActiveStatementUpdate update)
            => new(FromContract(update.Method), update.ILOffset, FromContract(update.NewSpan));

        public static ManagedModuleMethodId FromContract(this Contracts.ManagedModuleMethodId update)
            => new(update.Token, update.Version);

        public static SourceSpan FromContract(this Contracts.SourceSpan id)
            => new(id.StartLine, id.StartColumn, id.EndLine, id.EndColumn);

        public static ManagedExceptionRegionUpdate FromContract(this Contracts.ManagedExceptionRegionUpdate update)
            => new(FromContract(update.Method), update.Delta, FromContract(update.NewSpan));

        public static ImmutableArray<ManagedHotReloadDiagnostic> FromContract(this ImmutableArray<Contracts.ManagedHotReloadDiagnostic> diagnostics)
            => diagnostics.SelectAsArray(FromContract);

        public static ManagedHotReloadDiagnostic FromContract(this Contracts.ManagedHotReloadDiagnostic diagnostic)
            => new(diagnostic.Id, diagnostic.Message, (ManagedHotReloadDiagnosticSeverity)diagnostic.Severity, diagnostic.FilePath, FromContract(diagnostic.Span));
    }
}
