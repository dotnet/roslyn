// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var log = new TraceLog(5, "log");

            var projectId = ProjectId.CreateFromSerialized(Guid.Parse("5E40F37C-5AB3-495E-A3F2-4A244D177674"));
            var diagnostic = Diagnostic.Create(EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile), Location.None, "file", "error");

            log.Write("a");
            log.Write("b {0} {1} {2}", 1, "x", 3);
            log.Write("c");
            log.Write("d str={0} projectId={1} summary={2} diagnostic=`{3}`", (string)null, projectId, ProjectAnalysisSummary.RudeEdits, diagnostic);
            log.Write("e");
            log.Write("f");

            AssertEx.Equal(new[]
            {
               "f",
               "b 1 x 3",
               "c",
               $"d str=<null> projectId=1324595798 summary=RudeEdits diagnostic=`{diagnostic.ToString()}`",
               "e"
            }, log.GetTestAccessor().Entries.Select(e => e.GetDebuggerDisplay()));
        }
    }
}
