// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues
{
    public abstract class RemoveUnusedValuesTestsBase : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), new CSharpRemoveUnusedValuesCodeFixProvider());

        protected abstract IDictionary<OptionKey, object> PreferNone { get; }
        protected abstract IDictionary<OptionKey, object> PreferDiscard { get; }
        protected abstract IDictionary<OptionKey, object> PreferUnusedLocal { get; }

        protected IDictionary<OptionKey, object> GetOptions(string optionName)
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

        protected Task TestMissingInRegularAndScriptAsync(string initialMarkup, IDictionary<OptionKey, object> options)
            => TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options));
        protected Task TestMissingInRegularAndScriptAsync(string initialMarkup, string optionName)
             => TestMissingInRegularAndScriptAsync(initialMarkup, GetOptions(optionName));
        protected Task TestInRegularAndScriptAsync(string initialMarkup, string expectedMarkup, string optionName)
            => TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: GetOptions(optionName));

        // Helpers to test all options - only used by tests which already have InlineData for custom input test code snippets.
        protected async Task TestInRegularAndScriptWithAllOptionsAsync(string initialMarkup, string expectedMarkup)
        {
            foreach (var options in new[] { PreferDiscard, PreferUnusedLocal })
            {
                await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: options);
            }
        }

        protected async Task TestMissingInRegularAndScriptWithAllOptionsAsync(string initialMarkup)
        {
            foreach (var options in new[] { PreferDiscard, PreferUnusedLocal })
            {
                await TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options));
            }
        }
    }
}
