// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.LanguageServer;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.LanguageServer
{
    public abstract class AbstractLspBuildOnlyDiagnosticsTests
    {
        protected abstract Type ErrorCodeType { get; }
        protected abstract Type LspBuildOnlyDiagnosticsType { get; }
        protected abstract ImmutableArray<string> ExpectedDiagnosticCodes { get; }

        [Fact]
        public void TestExportedDiagnosticIds()
        {
            var attribute = this.LspBuildOnlyDiagnosticsType.GetCustomAttribute<LspBuildOnlyDiagnosticsAttribute>();

            var actualDiagnosticCodes = attribute.BuildOnlyDiagnostics;
            var missing = ExpectedDiagnosticCodes.Except(actualDiagnosticCodes).OrderBy(k => k).ToList();

            var errorMessage = new StringBuilder();
            foreach (var missingItem in missing)
            {
                var code = missingItem.Substring(2).TrimStart('0'); // trim off CS or VB and any leading zeros
                var codeValue = int.Parse(code);

                var enumMembers = ErrorCodeType.GetFields(BindingFlags.Public | BindingFlags.Static);
                var enumMember = enumMembers.First(m => Convert.ToInt32(m.GetValue(null)) == codeValue);

                errorMessage.AppendLine($@"Missing: ""{missingItem}, // {ErrorCodeType.Name}.{enumMember.Name}""");
            }

            var extra = actualDiagnosticCodes.Except(ExpectedDiagnosticCodes);
            foreach (var extraItem in extra)
            {
                errorMessage.AppendLine($@"Extra: ""{extraItem}"" not in IsBuildOnlyDiagnostic");
            }

            if (errorMessage.Length > 0)
                AssertEx.Fail(errorMessage.ToString());
        }
    }
}
