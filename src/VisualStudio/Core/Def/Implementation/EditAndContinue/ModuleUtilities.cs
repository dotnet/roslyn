// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Symbols;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

using EnC = Microsoft.CodeAnalysis.EditAndContinue;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    internal static class ModuleUtilities
    {
        internal static LinePositionSpan ToLinePositionSpan(this DkmTextSpan span)
        {
            // ignore invalid/unsupported spans - they might come from stack frames of non-managed languages
            if (span.StartLine <= 0 || span.EndLine <= 0)
            {
                return default;
            }

            // C++ produces spans without columns
            if (span.StartColumn == 0 && span.EndColumn == 0)
            {
                return new LinePositionSpan(new LinePosition(span.StartLine - 1, 0), new LinePosition(span.EndLine - 1, 0));
            }

            // ignore invalid/unsupported spans - they might come from stack frames of non-managed languages
            if (span.StartColumn <= 0 || span.EndColumn <= 0)
            {
                return default;
            }

            return new LinePositionSpan(new LinePosition(span.StartLine - 1, span.StartColumn - 1), new LinePosition(span.EndLine - 1, span.EndColumn - 1));
        }

        internal static DkmTextSpan ToDebuggerSpan(this LinePositionSpan span, int lineDelta = 0)
            => new DkmTextSpan(
                StartLine: span.Start.Line + lineDelta + 1,
                EndLine: span.End.Line + lineDelta + 1,
                StartColumn: span.Start.Character + 1,
                EndColumn: span.End.Character + 1);

        internal static EnC.ActiveStatementDebugInfo.Data ToActiveStatementDebugInfoData(this ActiveStatementDebugInfo info)
            => new EnC.ActiveStatementDebugInfo(
                new EnC.ActiveInstructionId(info.InstructionId.MethodId.ModuleId, info.InstructionId.MethodId.Token, info.InstructionId.MethodId.Version, info.InstructionId.ILOffset),
                info.DocumentNameOpt,
                info.TextSpan.ToLinePositionSpan(),
                info.ThreadIds,
                (EnC.ActiveStatementFlags)info.Flags).Serialize();

        internal static DkmManagedModuleUpdate ToModuleUpdate(this EnC.Deltas delta)
        {
            var sequencePointUpdates = delta.LineEdits.SelectAsArray(documentChanges => DkmSequencePointsUpdate.Create(
                FileName: documentChanges.SourceFilePath,
                LineUpdates: documentChanges.Deltas.SelectAsArray(lineChange => DkmSourceLineUpdate.Create(lineChange.OldLine, lineChange.NewLine)).ToReadOnlyCollection()));

            var activeStatementUpdates = delta.ActiveStatementsInUpdatedMethods.SelectAsArray(activeStatement => DkmActiveStatementUpdate.Create(
                ThreadId: activeStatement.ThreadId,
                MethodId: new DkmClrMethodId(Token: activeStatement.OldInstructionId.MethodId.Token, Version: (uint)activeStatement.OldInstructionId.MethodId.Version),
                ILOffset: activeStatement.OldInstructionId.ILOffset,
                NewSpan: activeStatement.NewSpan.ToDebuggerSpan()));

            var exceptionRegions = delta.NonRemappableRegions.SelectAsArray(
                predicate: regionInfo => regionInfo.Region.IsExceptionRegion,
                selector: regionInfo => DkmExceptionRegionUpdate.Create(
                   new DkmClrMethodId(Token: regionInfo.Method.Token, Version: (uint)regionInfo.Method.Version),
                   NewSpan: regionInfo.Region.Span.ToDebuggerSpan(regionInfo.Region.LineDelta),
                   // The range span is the new span. Deltas are inverse.
                   //   old = new + delta
                   //   new = old – delta
                   Delta: -regionInfo.Region.LineDelta));

            return DkmManagedModuleUpdate.Create(
                delta.Mvid,
                delta.IL.ToReadOnlyCollection(),
                delta.Metadata.ToReadOnlyCollection(),
                delta.Pdb.ToReadOnlyCollection(),
                sequencePointUpdates.ToReadOnlyCollection(),
                delta.UpdatedMethods.ToReadOnlyCollection(),
                activeStatementUpdates.ToReadOnlyCollection(),
                exceptionRegions.ToReadOnlyCollection());
        }

        internal static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this ImmutableArray<T> array)
            => new ReadOnlyCollection<T>(array.DangerousGetUnderlyingArray());

        internal static ManagedModuleUpdateStatus ToModuleUpdateStatus(this EnC.SolutionUpdateStatus status)
            => status switch
            {
                EnC.SolutionUpdateStatus.None => ManagedModuleUpdateStatus.None,
                EnC.SolutionUpdateStatus.Ready => ManagedModuleUpdateStatus.Ready,
                EnC.SolutionUpdateStatus.Blocked => ManagedModuleUpdateStatus.Blocked,
                _ => throw ExceptionUtilities.UnexpectedValue(status),
            };
    }
}
