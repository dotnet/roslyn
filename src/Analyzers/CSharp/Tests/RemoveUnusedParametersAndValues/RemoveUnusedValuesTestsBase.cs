// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues
{
    public abstract class RemoveUnusedValuesTestsBase : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        protected RemoveUnusedValuesTestsBase(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), new CSharpRemoveUnusedValuesCodeFixProvider());

        private protected abstract OptionsCollection PreferNone { get; }
        private protected abstract OptionsCollection PreferDiscard { get; }
        private protected abstract OptionsCollection PreferUnusedLocal { get; }

        private protected OptionsCollection GetOptions(string optionName)
        {
            switch (optionName)
            {
                case nameof(PreferDiscard):
                    return PreferDiscard;

                case nameof(PreferUnusedLocal):
                    return PreferUnusedLocal;

                default:
                    return PreferNone;
            }
        }

        private protected Task TestMissingInRegularAndScriptAsync(string initialMarkup, OptionsCollection options, ParseOptions parseOptions = null)
            => TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options, parseOptions: parseOptions));
        private protected Task TestMissingInRegularAndScriptAsync(string initialMarkup, string optionName, ParseOptions parseOptions = null)
             => TestMissingInRegularAndScriptAsync(initialMarkup, GetOptions(optionName), parseOptions);
        protected Task TestInRegularAndScriptAsync(string initialMarkup, string expectedMarkup, string optionName, ParseOptions parseOptions = null)
            => TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: GetOptions(optionName), parseOptions: parseOptions);

        // Helpers to test all options - only used by tests which already have InlineData for custom input test code snippets.
        protected async Task TestInRegularAndScriptWithAllOptionsAsync(string initialMarkup, string expectedMarkup, ParseOptions parseOptions = null)
        {
            foreach (var options in new[] { PreferDiscard, PreferUnusedLocal })
            {
                await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: options, parseOptions: parseOptions);
            }
        }

        protected async Task TestMissingInRegularAndScriptWithAllOptionsAsync(string initialMarkup, ParseOptions parseOptions = null)
        {
            foreach (var options in new[] { PreferDiscard, PreferUnusedLocal })
            {
                await TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options, parseOptions: parseOptions));
            }
        }
    }
}
