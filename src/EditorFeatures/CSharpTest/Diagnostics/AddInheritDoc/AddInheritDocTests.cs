// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritDoc;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddInheritDoc
{
    public class AddInheritDocTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public AddInheritDocTests(ITestOutputHelper logger) : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new AddInheritDocCodeFixProvider());

        private async Task TestAsync(string initialMarkup, string expectedMarkup)
        {
            await TestAsync(initialMarkup, expectedMarkup, CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddInheritDoc)]
        public async Task AddMissingInheritDocOnClass()
        {
            await TestAsync(
            @"
/// Some doc.
public class BaseClass
{
}
public class [|Derived|]: BaseClass
{
}",
            @"
/// Some doc.
public class BaseClass
{
}
///<inheritdoc/> public class Derived: BaseClass
{
}");
        }
    }
}
