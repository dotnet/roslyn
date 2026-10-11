// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class NamedTypeFullNameNotNullSuppressorTests
    {
        [Fact]
        public void Suppressor_SupportedLanguages_IsCSharpOnly()
        {
            var attribute = typeof(NamedTypeFullNameNotNullSuppressor)
                .GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), inherit: false)
                .Cast<DiagnosticAnalyzerAttribute>()
                .Single();

            Assert.Single(attribute.Languages, LanguageNames.CSharp);
        }

        [Fact]
        public void Suppressor_SupportedSuppressions_ContainsExpectedRules()
        {
            var suppressor = new NamedTypeFullNameNotNullSuppressor();
            var ids = suppressor.SupportedSuppressions.Select(s => s.SuppressedDiagnosticId).ToList();

            Assert.Contains("CS8600", ids);
            Assert.Contains("CS8603", ids);
            Assert.Contains("CS8604", ids);
        }
    }
}
