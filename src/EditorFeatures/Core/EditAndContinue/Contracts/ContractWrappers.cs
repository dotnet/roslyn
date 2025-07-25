// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using InternalContracts = Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static class ContractWrappers
{
    extension(ManagedActiveStatementDebugInfo info)
    {
        public InternalContracts.ManagedActiveStatementDebugInfo ToContract()
        => new(ToContract(info.ActiveInstruction), info.DocumentName, ToContract(info.SourceSpan), (InternalContracts.ActiveStatementFlags)info.Flags);
    }

    extension(ManagedInstructionId id)
    {
        public InternalContracts.ManagedInstructionId ToContract()
        => new(ToContract(id.Method), id.ILOffset);
    }

    extension(ManagedMethodId id)
    {
        public InternalContracts.ManagedMethodId ToContract()
        => new(id.Module, id.Token, id.Version);
    }

    extension(SourceSpan id)
    {
        public InternalContracts.SourceSpan ToContract()
        => new(id.StartLine, id.StartColumn, id.EndLine, id.EndColumn);
    }

    extension(ProjectInstanceId id)
    {
        public InternalContracts.ProjectInstanceId ToContract()
        => new(id.ProjectFilePath, id.TargetFramework);
    }

    extension(RunningProjectInfo id)
    {
        public InternalContracts.RunningProjectInfo ToContract()
        => new(id.ProjectInstanceId.ToContract(), id.RestartAutomatically);
    }

    extension(ManagedHotReloadAvailability value)
    {
        public InternalContracts.ManagedHotReloadAvailability ToContract()
        => new((InternalContracts.ManagedHotReloadAvailabilityStatus)value.Status, value.LocalizedMessage);
    }

    extension(InternalContracts.ManagedHotReloadUpdate update)
    {
        public ManagedHotReloadUpdate FromContract()
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
    }

    extension(InternalContracts.ManagedHotReloadUpdates updates)
    {
        public ManagedHotReloadUpdates FromContract()
        => new(updates.Updates.FromContract(), updates.Diagnostics.FromContract(), updates.ProjectsToRebuild.SelectAsArray(FromContract), updates.ProjectsToRestart.SelectAsArray(FromContract));
    }

    extension(ImmutableArray<InternalContracts.ManagedHotReloadUpdate> diagnostics)
    {
        public ImmutableArray<ManagedHotReloadUpdate> FromContract()
        => diagnostics.SelectAsArray(FromContract);
    }

    extension(InternalContracts.SequencePointUpdates updates)
    {
        public SequencePointUpdates FromContract()
        => new(updates.FileName, updates.LineUpdates.SelectAsArray(FromContract));
    }

    extension(InternalContracts.SourceLineUpdate update)
    {
        public SourceLineUpdate FromContract()
        => new(update.OldLine, update.NewLine);
    }

    extension(InternalContracts.ManagedActiveStatementUpdate update)
    {
        public ManagedActiveStatementUpdate FromContract()
        => new(FromContract(update.Method), update.ILOffset, FromContract(update.NewSpan));
    }

    extension(InternalContracts.ManagedModuleMethodId update)
    {
        public ManagedModuleMethodId FromContract()
        => new(update.Token, update.Version);
    }

    extension(InternalContracts.SourceSpan id)
    {
        public SourceSpan FromContract()
        => new(id.StartLine, id.StartColumn, id.EndLine, id.EndColumn);
    }

    extension(InternalContracts.ProjectInstanceId id)
    {
        public ProjectInstanceId FromContract()
        => new(id.ProjectFilePath, id.TargetFramework);
    }

    extension(InternalContracts.ManagedExceptionRegionUpdate update)
    {
        public ManagedExceptionRegionUpdate FromContract()
        => new(FromContract(update.Method), update.Delta, FromContract(update.NewSpan));
    }

    extension(ImmutableArray<InternalContracts.ManagedHotReloadDiagnostic> diagnostics)
    {
        public ImmutableArray<ManagedHotReloadDiagnostic> FromContract()
        => diagnostics.SelectAsArray(FromContract);
    }

    extension(InternalContracts.ManagedHotReloadDiagnostic diagnostic)
    {
        public ManagedHotReloadDiagnostic FromContract()
        => new(diagnostic.Id, diagnostic.Message, (ManagedHotReloadDiagnosticSeverity)diagnostic.Severity, diagnostic.FilePath, FromContract(diagnostic.Span));
    }
}
