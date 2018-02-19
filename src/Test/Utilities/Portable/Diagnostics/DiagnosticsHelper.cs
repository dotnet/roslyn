// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class DiagnosticsHelper
    {
        private static TextSpan FindSpan(string source, string pattern)
        {
            var match = Regex.Match(source, pattern);
            Assert.True(match.Success, "Could not find a match for \"" + pattern + "\" in:" + Environment.NewLine + source);
            return new TextSpan(match.Index, match.Length);
        }

        private static void VerifyDiagnostics(IEnumerable<Diagnostic> actualDiagnostics, params string[] expectedDiagnosticIds)
        {
            var actualDiagnosticIds = actualDiagnostics.Select(d => d.Id);
            Assert.True(expectedDiagnosticIds.SequenceEqual(actualDiagnosticIds),
                Environment.NewLine + "Expected: " + string.Join(", ", expectedDiagnosticIds) +
                Environment.NewLine + "Actual: " + string.Join(", ", actualDiagnosticIds));
        }

        public static void VerifyDiagnostics(SemanticModel model, string source, string pattern, params string[] expectedDiagnosticIds)
        {
            VerifyDiagnostics(model.GetDiagnostics(FindSpan(source, pattern)), expectedDiagnosticIds);
        }
    }
}
