// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public class TraceLogTests
    {
        [Fact]
        public void Write()
        {
            var log = new TraceLog(5, "log", "File.log");

            var projectId = ProjectId.CreateFromSerialized(Guid.Parse("5E40F37C-5AB3-495E-A3F2-4A244D177674"), debugName: "MyProject");
            var diagnostic = Diagnostic.Create(EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile), Location.None, "file", "error");

            log.Write("a");
            log.Write("b {0} {1} 0x{2:X8}", 1, "x", 255);
            log.Write("c");
            log.Write("d str={0} projectId={1} summary={2} diagnostic=`{3}`", (string?)null, projectId, ProjectAnalysisSummary.RudeEdits, diagnostic);
            log.Write("e");
            log.Write("f");

            AssertEx.Equal(new[]
            {
               "f",
               "b 1 x 0x000000FF",
               "c",
               $"d str=<null> projectId=MyProject summary=RudeEdits diagnostic=`{diagnostic}`",
               "e"
            }, log.GetTestAccessor().Entries.Select(e => e.GetDebuggerDisplay()));
        }
    }
}
