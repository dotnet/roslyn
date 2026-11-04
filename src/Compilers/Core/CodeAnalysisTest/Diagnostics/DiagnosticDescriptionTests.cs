// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class DiagnosticDescriptionTests
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
    }
}
