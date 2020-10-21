// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Symbols;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

using EnC = Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    internal static class ModuleUtilities
    {
        internal static EnC.SourceSpan ToSourceSpan(this DkmTextSpan span)
        {
            // ignore invalid/unsupported spans - they might come from stack frames of non-managed languages
            if (span.StartLine <= 0 || span.EndLine <= 0)
            {
                return default;
            }

            // C++ produces spans without columns
            if (span.StartColumn == 0 && span.EndColumn == 0)
            {
                return new(span.StartLine - 1, 0, span.EndLine - 1, 0);
            }

            // ignore invalid/unsupported spans - they might come from stack frames of non-managed languages
            if (span.StartColumn <= 0 || span.EndColumn <= 0)
            {
                return default;
            }

            return new(span.StartLine - 1, span.StartColumn - 1, span.EndLine - 1, span.EndColumn - 1);
        }

        internal static DkmTextSpan ToDebuggerSpan(this EnC.SourceSpan span, int lineDelta = 0)
            => new(
                StartLine: span.StartLine + lineDelta + 1,
                EndLine: span.EndLine + lineDelta + 1,
                StartColumn: span.StartColumn + 1,
                EndColumn: span.EndColumn + 1);

        internal static EnC.ManagedActiveStatementDebugInfo ToActiveStatementDebugInfo(this ActiveStatementDebugInfo info)
            => new EnC.ManagedActiveStatementDebugInfo(
                new EnC.ManagedInstructionId(new EnC.ManagedMethodId(info.InstructionId.MethodId.ModuleId, info.InstructionId.MethodId.Token, info.InstructionId.MethodId.Version), info.InstructionId.ILOffset),
                info.DocumentNameOpt,
                info.TextSpan.ToSourceSpan(),
                (EnC.ActiveStatementFlags)info.Flags);

        internal static DkmManagedModuleUpdate ToModuleUpdate(this EnC.ManagedModuleUpdate delta)
        {
            var sequencePointUpdates = delta.SequencePoints.SelectAsArray(documentChanges => DkmSequencePointsUpdate.Create(
                FileName: documentChanges.FileName,
                LineUpdates: documentChanges.LineUpdates.SelectAsArray(lineChange => DkmSourceLineUpdate.Create(lineChange.OldLine, lineChange.NewLine)).ToReadOnlyCollection()));

            var activeStatementUpdates = delta.ActiveStatements.SelectAsArray(activeStatement => DkmActiveStatementUpdate.Create(
                ThreadId: Guid.Empty, // no longer needed
                MethodId: new DkmClrMethodId(Token: activeStatement.Method.Token, Version: (uint)activeStatement.Method.Version),
                ILOffset: activeStatement.ILOffset,
                NewSpan: activeStatement.NewSpan.ToDebuggerSpan()));

            var exceptionRegions = delta.ExceptionRegions.SelectAsArray(regionInfo => DkmExceptionRegionUpdate.Create(
                   new DkmClrMethodId(Token: regionInfo.Method.Token, Version: (uint)regionInfo.Method.Version),
                   NewSpan: regionInfo.NewSpan.ToDebuggerSpan(regionInfo.LineDelta),
                   // The range span is the new span. Deltas are inverse.
                   //   old = new + delta
                   //   new = old – delta
                   Delta: -regionInfo.LineDelta));

            return DkmManagedModuleUpdate.Create(
                delta.Module,
                delta.ILDelta.ToReadOnlyCollection(),
                delta.MetadataDelta.ToReadOnlyCollection(),
                delta.PdbDelta.ToReadOnlyCollection(),
                sequencePointUpdates.ToReadOnlyCollection(),
                delta.UpdatedMethods.ToReadOnlyCollection(),
                activeStatementUpdates.ToReadOnlyCollection(),
                exceptionRegions.ToReadOnlyCollection());
        }

        internal static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this ImmutableArray<T> array)
            => new(array.DangerousGetUnderlyingArray());

        internal static ManagedModuleUpdateStatus ToModuleUpdateStatus(this EnC.ManagedModuleUpdateStatus status)
            => status switch
            {
                EnC.ManagedModuleUpdateStatus.None => ManagedModuleUpdateStatus.None,
                EnC.ManagedModuleUpdateStatus.Ready => ManagedModuleUpdateStatus.Ready,
                EnC.ManagedModuleUpdateStatus.Blocked => ManagedModuleUpdateStatus.Blocked,
                _ => throw ExceptionUtilities.UnexpectedValue(status),
            };
    }
}
