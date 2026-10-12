// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class DiagnosticCreationTests
    {
        [Theory]
        [InlineData(246, "Diagnostic(246, \"Util2\").WithArguments(\"Util2\").WithLocation(1, 5)")]
        [InlineData("CS0246", "Diagnostic(\"CS0246\", \"Util2\").WithArguments(\"Util2\").WithLocation(1, 5)")]
        [InlineData(ErrorCode.ERR_SingleTypeNameNotFound, "Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, \"Util2\").WithArguments(\"Util2\").WithLocation(1, 5)")]
        public void TestDiagnosticDescriptionToString(object code, string expected)
        {
            var description = TestHelpers.Diagnostic(code, "Util2").WithArguments("Util2").WithLocation(1, 5);
            Assert.Equal(expected, description.ToString());
        }

        [Fact]
        public void TestDiagnosticVerificationReportsNumericCode()
        {
            var exception = Assert.ThrowsAny<XunitException>(() =>
                Array.Empty<Diagnostic>().Verify(TestHelpers.Diagnostic(246, "Util2").WithArguments("Util2").WithLocation(2, 5)));

            Assert.Contains("Diagnostic(246, \"Util2\").WithArguments(\"Util2\").WithLocation(2, 5)", exception.Message);
        }

        [Fact, WorkItem(547049, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547049")]
        public void TestDiagnosticCreationWithOverriddenSeverity()
        {
            var defaultSeverity = DiagnosticSeverity.Info;
            var effectiveSeverity = DiagnosticSeverity.Error;
            var descriptor = new DiagnosticDescriptor("ID", "Title", "MessageFormat", "Category", defaultSeverity, isEnabledByDefault: true);
            var diagnostic = Diagnostic.Create(descriptor, Location.None, effectiveSeverity, additionalLocations: null, properties: null);
            Assert.Equal(effectiveSeverity, diagnostic.Severity);
            Assert.Equal(0, diagnostic.WarningLevel);
        }
    }
}
