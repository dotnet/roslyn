// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.IO;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal static class ActiveStatementTestHelpers
    {
        internal static ImmutableArray<ManagedActiveStatementDebugInfo> GetActiveStatementDebugInfos(
            string[] markedSources,
            string extension = ".cs",
            int[]? methodRowIds = null,
            Guid[]? modules = null,
            int[]? methodVersions = null,
            int[]? ilOffsets = null,
            ActiveStatementFlags[]? flags = null)
        {
            IEnumerable<(TextSpan Span, int Id, SourceText Text, string DocumentName, DocumentId DocumentId)> EnumerateAllSpans()
            {
                var sourceIndex = 0;
                foreach (var markedSource in markedSources)
                {
                    var documentName = Path.Combine(TempRoot.Root, TestWorkspace.GetDefaultTestSourceDocumentName(sourceIndex, extension));
                    var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId(), documentName);
                    var text = SourceText.From(markedSource);

                    foreach (var (span, id) in ActiveStatementsDescription.GetActiveSpans(markedSource))
                    {
                        yield return (span, id, text, documentName, documentId);
                    }

                    sourceIndex++;
                }
            }

            IEnumerable<ManagedActiveStatementDebugInfo> Enumerate()
            {
                var moduleId = new Guid("00000000-0000-0000-0000-000000000001");
                var threadId = new Guid("00000000-0000-0000-0000-000000000010");

                var index = 0;
                foreach (var (span, id, text, documentName, documentId) in EnumerateAllSpans().OrderBy(s => s.Id))
                {
                    yield return new ManagedActiveStatementDebugInfo(
                        new ManagedInstructionId(
                            new ManagedMethodId(
                                (modules != null) ? modules[index] : moduleId,
                                new ManagedModuleMethodId(
                                    token: 0x06000000 | (methodRowIds != null ? methodRowIds[index] : index + 1),
                                    version: (methodVersions != null) ? methodVersions[index] : 1)),
                            ilOffset: (ilOffsets != null) ? ilOffsets[index] : 0),
                        documentName: documentName,
                        sourceSpan: text.Lines.GetLinePositionSpan(span).ToSourceSpan(),
                        flags: (flags != null) ? flags[index] : ((id == 0 ? ActiveStatementFlags.IsLeafFrame : ActiveStatementFlags.IsNonLeafFrame) | ActiveStatementFlags.MethodUpToDate));

                    index++;
                }
            }

            return Enumerate().ToImmutableArray();
        }

        public static string Delete(string src, string marker)
        {
            while (true)
            {
                var startStr = "/*delete" + marker;
                var endStr = "*/";
                var start = src.IndexOf(startStr);
                if (start == -1)
                {
                    return src;
                }

                var end = src.IndexOf(endStr, start + startStr.Length) + endStr.Length;
                src = src.Substring(0, start) + src.Substring(end);
            }
        }

        /// <summary>
        /// Inserts new lines into the text at the position indicated by /*insert<paramref name="marker"/>[{number-of-lines-to-insert}]*/.
        /// </summary>
        public static string InsertNewLines(string src, string marker)
        {
            while (true)
            {
                var startStr = "/*insert" + marker + "[";
                var endStr = "*/";

                var start = src.IndexOf(startStr);
                if (start == -1)
                {
                    return src;
                }

                var startOfLineCount = start + startStr.Length;
                var endOfLineCount = src.IndexOf(']', startOfLineCount);
                var lineCount = int.Parse(src.Substring(startOfLineCount, endOfLineCount - startOfLineCount));

                var end = src.IndexOf(endStr, endOfLineCount) + endStr.Length;

                src = src.Substring(0, start) + string.Join("", Enumerable.Repeat(Environment.NewLine, lineCount)) + src.Substring(end);
            }
        }

        public static string Update(string src, string marker)
            => InsertNewLines(Delete(src, marker), marker);

        public static string InspectActiveStatement(ActiveStatement statement)
            => $"{statement.Ordinal}: {statement.FileSpan} flags=[{statement.Flags}] #{statement.DocumentOrdinal}";

        public static string InspectActiveStatementAndInstruction(ActiveStatement statement)
            => InspectActiveStatement(statement) + " " + statement.InstructionId.GetDebuggerDisplay();

        public static string InspectActiveStatementAndInstruction(ActiveStatement statement, SourceText text)
            => InspectActiveStatementAndInstruction(statement) + $" '{GetFirstLineText(statement.Span, text)}'";

        public static string InspectActiveStatementUpdate(ManagedActiveStatementUpdate update)
            => $"{update.Method.GetDebuggerDisplay()} IL_{update.ILOffset:X4}: {update.NewSpan.GetDebuggerDisplay()}";

        public static IEnumerable<string> InspectNonRemappableRegions(ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> regions)
            => regions.OrderBy(r => r.Key.Token).Select(r => $"{r.Key.Method.GetDebuggerDisplay()} | {string.Join(", ", r.Value.Select(r => r.GetDebuggerDisplay()))}");

        public static string InspectExceptionRegionUpdate(ManagedExceptionRegionUpdate r)
            => $"{r.Method.GetDebuggerDisplay()} | {r.NewSpan.GetDebuggerDisplay()} Delta={r.Delta}";

        public static string GetFirstLineText(LinePositionSpan span, SourceText text)
            => text.Lines[span.Start.Line].ToString().Trim();
    }
}
