// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using PostSharp.Backstage.Extensibility;
using Xunit;

namespace Caravela.Compiler.UnitTests.Utilities
{
    internal sealed class TestDiagnosticsSink : IDiagnosticsSink
    {
        private readonly List<string> _warnings = new();
        private readonly List<string> _errors = new();

        public void ReportWarning(string message, IDiagnosticsLocation? location = null)
        {
            if (location != null)
            {
                throw new NotImplementedException();
            }

            _warnings.Add(message);
        }

        public void ReportError(string message, IDiagnosticsLocation? location = null)
        {
            if (location != null)
            {
                throw new NotImplementedException();
            }

            _errors.Add(message);
        }

        public IEnumerable<string> GetWarnings() => _warnings;

        public IEnumerable<string> GetErrors() => _errors;

        public void AssertEmptyWarnings()
        {
            Assert.Empty(_warnings);
        }

        public void AssertEmptyErrors()
        {
            Assert.Empty(_errors);
        }

        public void AssertEmpty()
        {
            AssertEmptyWarnings();
            AssertEmptyErrors();
        }
    }
}
