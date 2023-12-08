// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using InternalContracts = Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class ContractWrappers
    {
        public static InternalContracts.ManagedActiveStatementDebugInfo ToContract(this ManagedActiveStatementDebugInfo info)
            => new(ToContract(info.ActiveInstruction), info.DocumentName, ToContract(info.SourceSpan), (InternalContracts.ActiveStatementFlags)info.Flags);

        public static InternalContracts.ManagedInstructionId ToContract(this ManagedInstructionId id)
            => new(ToContract(id.Method), id.ILOffset);

        public static InternalContracts.ManagedMethodId ToContract(this ManagedMethodId id)
            => new(id.Module, id.Token, id.Version);

        public static InternalContracts.SourceSpan ToContract(this SourceSpan id)
            => new(id.StartLine, id.StartColumn, id.EndLine, id.EndColumn);

        public static InternalContracts.ManagedHotReloadAvailability ToContract(this ManagedHotReloadAvailability value)
            => new((InternalContracts.ManagedHotReloadAvailabilityStatus)value.Status, value.LocalizedMessage);

        public static ManagedHotReloadUpdate FromContract(this InternalContracts.ManagedHotReloadUpdate update)
            => new(
                module: update.Module,
                moduleName: update.ModuleName,
                ilDelta: update.ILDelta,
                metadataDelta: update.MetadataDelta,
                pdbDelta: update.PdbDelta,
                updatedTypes: update.UpdatedTypes,
                requiredCapabilities: update.RequiredCapabilities,
                updatedMethods: update.UpdatedMethods,
                sequencePoints: update.SequencePoints.SelectAsArray(FromContract),
                activeStatements: update.ActiveStatements.SelectAsArray(FromContract),
                exceptionRegions: update.ExceptionRegions.SelectAsArray(FromContract));

        public static ManagedHotReloadUpdates FromContract(this InternalContracts.ManagedHotReloadUpdates updates)
            => new(updates.Updates.FromContract(), updates.Diagnostics.FromContract());

        public static ImmutableArray<ManagedHotReloadUpdate> FromContract(this ImmutableArray<InternalContracts.ManagedHotReloadUpdate> diagnostics)
            => diagnostics.SelectAsArray(FromContract);

        public static SequencePointUpdates FromContract(this InternalContracts.SequencePointUpdates updates)
            => new(updates.FileName, updates.LineUpdates.SelectAsArray(FromContract));

        public static SourceLineUpdate FromContract(this InternalContracts.SourceLineUpdate update)
            => new(update.OldLine, update.NewLine);

        public static ManagedActiveStatementUpdate FromContract(this InternalContracts.ManagedActiveStatementUpdate update)
            => new(FromContract(update.Method), update.ILOffset, FromContract(update.NewSpan));

        public static ManagedModuleMethodId FromContract(this InternalContracts.ManagedModuleMethodId update)
            => new(update.Token, update.Version);

        public static SourceSpan FromContract(this InternalContracts.SourceSpan id)
            => new(id.StartLine, id.StartColumn, id.EndLine, id.EndColumn);

        public static ManagedExceptionRegionUpdate FromContract(this InternalContracts.ManagedExceptionRegionUpdate update)
            => new(FromContract(update.Method), update.Delta, FromContract(update.NewSpan));

        public static ImmutableArray<ManagedHotReloadDiagnostic> FromContract(this ImmutableArray<InternalContracts.ManagedHotReloadDiagnostic> diagnostics)
            => diagnostics.SelectAsArray(FromContract);

        public static ManagedHotReloadDiagnostic FromContract(this InternalContracts.ManagedHotReloadDiagnostic diagnostic)
            => new(diagnostic.Id, diagnostic.Message, (ManagedHotReloadDiagnosticSeverity)diagnostic.Severity, diagnostic.FilePath, FromContract(diagnostic.Span));
    }
}
