// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    internal static class ModuleUtilities
    {
        internal static bool TryGetModuleInfo(this DkmClrModuleInstance module, out DebuggeeModuleInfo info)
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA, "SymReader requires MTA");

            IntPtr metadataPtr;
            uint metadataSize;
            try
            {
                metadataPtr = module.GetBaselineMetaDataBytesPtr(out metadataSize);
            }
            catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
            {
                info = null;
                return false;
            }

            var symReader = module.GetSymUnmanagedReader() as ISymUnmanagedReader5;
            if (symReader == null)
            {
                info = null;
                return false;
            }

            var metadata = ModuleMetadata.CreateFromMetadata(metadataPtr, (int)metadataSize);
            info = new DebuggeeModuleInfo(metadata, symReader);
            return true;
        }

        internal static ManagedModuleUpdate ToModuleUpdate(this Deltas delta)
        {
            var sequencePointUpdates = delta.LineEdits.SelectAsArray(documentChanges => new SequencePointsUpdate(
                fileName: documentChanges.SourceFilePath,
                lineUpdates: documentChanges.Deltas.SelectAsArray(lineChange => new SourceLineUpdate(lineChange.OldLine, lineChange.NewLine))));

            TextManager.Interop.TextSpan toDebuggerSpan(LinePositionSpan span, int lineDelta)
                => new TextManager.Interop.TextSpan()
                {
                    // the debugger expects these to be 0-based
                    iStartLine = span.Start.Line + lineDelta,
                    iStartIndex = span.Start.Character,
                    iEndLine = span.End.Line + lineDelta,
                    iEndIndex = span.End.Character,
                };

            var activeStatementUpdates = delta.ActiveStatementsInUpdatedMethods.SelectAsArray(activeStatement => new ActiveStatementUpdate(
                threadId: activeStatement.ThreadId,
                methodToken: activeStatement.OldInstructionId.MethodId.Token,
                methodVersion: activeStatement.OldInstructionId.MethodId.Version,
                ilOffset: activeStatement.OldInstructionId.ILOffset,
                newSpan: toDebuggerSpan(activeStatement.NewSpan, 0)));

            var exceptionRegions = delta.NonRemappableRegions.SelectAsArray(
                predicate: regionInfo => regionInfo.Region.IsExceptionRegion,
                selector: regionInfo => new ExceptionRegionUpdate(
                   methodToken: regionInfo.Method.Token,
                   methodVersion: regionInfo.Method.Version,
                   newSpan: toDebuggerSpan(regionInfo.Region.Span, regionInfo.Region.LineDelta),
                   // The range span is the new span. Deltas are inverse.
                   //   old = new + delta
                   //   new = old – delta
                   delta: -regionInfo.Region.LineDelta));

            return new ManagedModuleUpdate(
                delta.Mvid,
                delta.IL.Value,
                delta.Metadata.Bytes,
                delta.Pdb.Stream,
                sequencePointUpdates,
                delta.Pdb.UpdatedMethods,
                activeStatementUpdates,
                exceptionRegions);
        }

        internal static ManagedModuleUpdateStatus ToModuleUpdateStatus(this SolutionUpdateStatus status)
        {
            switch (status)
            {
                case SolutionUpdateStatus.None:
                    return ManagedModuleUpdateStatus.None;

                case SolutionUpdateStatus.Ready:
                    return ManagedModuleUpdateStatus.Ready;

                case SolutionUpdateStatus.Blocked:
                    return ManagedModuleUpdateStatus.Blocked;

                default:
                    throw ExceptionUtilities.UnexpectedValue(status);
            }
        }
    }
}
