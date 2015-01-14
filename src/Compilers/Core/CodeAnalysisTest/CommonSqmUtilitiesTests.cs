// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CommonSqmUtilitiesTests
    {
        [Fact]
        public void TryGetCompilerDiagnosticCode_Valid()
        {
            string diagnosticId = "CS1011";

            uint code;
            var result = CommonCompiler.TryGetCompilerDiagnosticCode(diagnosticId, "CS", out code);

            Assert.True(result);
            Assert.Equal(expected: 1011U, actual: code);
        }

        [Fact]
        public void TryGetCompilerDiagnosticCode_Invalid()
        {
            string diagnosticId = "MyAwesomeDiagnostic";

            uint code;
            var result = CommonCompiler.TryGetCompilerDiagnosticCode(diagnosticId, "CS", out code);

            Assert.False(result);
        }

        [Fact]
        public void TryGetCompilerDiagnosticCode_WrongPrefix()
        {
            string diagnosticId = "AB1011";

            uint code;
            var result = CommonCompiler.TryGetCompilerDiagnosticCode(diagnosticId, "CS", out code);

            Assert.False(result);
        }
    }
}
