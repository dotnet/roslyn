// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.EditorConfig;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle
{
    [ExportConfigurationFixProvider(PredefinedConfigurationFixProviderNames.ConfigureCodeStyleOption, LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    [ExtensionOrder(Before = PredefinedConfigurationFixProviderNames.ConfigureSeverity)]
    [ExtensionOrder(After = PredefinedConfigurationFixProviderNames.Suppression)]
    internal sealed partial class ConfigureCodeStyleOptionCodeFixProvider : IConfigurationFixProvider
    {
        private static readonly ImmutableArray<bool> s_boolValues = ImmutableArray.Create(true, false);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public ConfigureCodeStyleOptionCodeFixProvider()
        {
        }

        public bool IsFixableDiagnostic(Diagnostic diagnostic)
        {
            // We only offer fix for configurable code style diagnostics which have one of more editorconfig based storage locations.
            // Also skip suppressed diagnostics defensively, though the code fix engine should ideally never call us for suppressed diagnostics.
            if (diagnostic.IsSuppressed ||
                SuppressionHelpers.IsNotConfigurableDiagnostic(diagnostic) ||
                diagnostic.Location.SourceTree == null)
            {
                return false;
            }

            var language = diagnostic.Location.SourceTree.Options.Language;
            return IDEDiagnosticIdToOptionMappingHelper.TryGetMappedOptions(diagnostic.Id, language, out var options) &&
               !options.IsEmpty &&
               options.All(o => o.StorageLocations.Any(l => l is IEditorConfigStorageLocation2));
        }

        public FixAllProvider GetFixAllProvider()
            => null;

        public Task<ImmutableArray<CodeFix>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => Task.FromResult(GetConfigurations(document.Project, diagnostics, cancellationToken));

        public Task<ImmutableArray<CodeFix>> GetFixesAsync(Project project, IEnumerable<Diagnostic> diagnostics, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => Task.FromResult(GetConfigurations(project, diagnostics, cancellationToken));

        private static ImmutableArray<CodeFix> GetConfigurations(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<CodeFix>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                // First get all the relevant code style options for the diagnostic.
                var codeStyleOptions = ConfigurationUpdater.GetCodeStyleOptionsForDiagnostic(diagnostic, project);
                if (codeStyleOptions.IsEmpty)
                {
                    continue;
                }

                // For each code style option, create a top level code action with nested code actions for every valid option value.
                // For example, if the option value is CodeStyleOption<bool>, we will have two nested actions, one for 'true' setting and one
                // for 'false' setting. If the option value is CodeStyleOption<SomeEnum>, we will have a nested action for each enum field.
                using var _ = ArrayBuilder<CodeAction>.GetInstance(out var nestedActions);
                var optionSet = project.Solution.Workspace.Options;
                var hasMultipleOptions = codeStyleOptions.Length > 1;
                foreach (var (optionKey, codeStyleOption, editorConfigLocation, perLanguageOption) in codeStyleOptions.OrderBy(t => t.optionKey.Option.Name))
                {
                    var topLevelAction = GetCodeActionForCodeStyleOption(optionKey, codeStyleOption, editorConfigLocation, diagnostic, perLanguageOption, optionSet, hasMultipleOptions);
                    if (topLevelAction != null)
                    {
                        nestedActions.Add(topLevelAction);
                    }
                }

                if (nestedActions.Count != 0)
                {
                    // Wrap actions by another level if the diagnostic ID has multiple associated code style options to reduce clutter.
                    var resultCodeAction = nestedActions.Count > 1
                        ? new TopLevelConfigureCodeStyleOptionCodeAction(diagnostic, nestedActions.ToImmutable())
                        : nestedActions.Single();

                    result.Add(new CodeFix(project, resultCodeAction, diagnostic));
                }
            }

            return result.ToImmutableAndFree();

            // Local functions
            TopLevelConfigureCodeStyleOptionCodeAction GetCodeActionForCodeStyleOption(
                OptionKey optionKey,
                ICodeStyleOption codeStyleOption,
                IEditorConfigStorageLocation2 editorConfigLocation,
                Diagnostic diagnostic,
                bool isPerLanguage,
                OptionSet optionSet,
                bool hasMultipleOptions)
            {
                // Add a code action for every valid value of the given code style option.
                // We only support light-bulb configuration of code style options with boolean or enum values.

                using var _ = ArrayBuilder<CodeAction>.GetInstance(out var nestedActions);

                string optionName = null;
                if (codeStyleOption.Value is bool)
                {
                    foreach (var boolValue in s_boolValues)
                    {
                        AddCodeActionWithOptionValue(codeStyleOption, boolValue);
                    }
                }
                else if (codeStyleOption.Value?.GetType() is Type t && t.IsEnum)
                {
                    foreach (var enumValue in Enum.GetValues(t))
                    {
                        AddCodeActionWithOptionValue(codeStyleOption, enumValue);
                    }
                }

                if (nestedActions.Count > 0)
                {
                    // If this is not a unique code style option for the diagnostic, use the optionName as the code action title.
                    // In that case, we will already have a containing top level action for the diagnostic.
                    // Otherwise, use the diagnostic information in the title.
                    return hasMultipleOptions
                        ? new TopLevelConfigureCodeStyleOptionCodeAction(optionName, nestedActions.ToImmutable())
                        : new TopLevelConfigureCodeStyleOptionCodeAction(diagnostic, nestedActions.ToImmutable());
                }

                return null;

                // Local functions
                void AddCodeActionWithOptionValue(ICodeStyleOption codeStyleOption, object newValue)
                {
                    // Create a new code style option value with the newValue
                    var configuredCodeStyleOption = codeStyleOption.WithValue(newValue);

                    // Try to get the parsed editorconfig string representation of the new code style option value
                    if (ConfigurationUpdater.TryGetEditorConfigStringParts(configuredCodeStyleOption, editorConfigLocation, optionSet, out var parts))
                    {
                        // We expect all code style values for same code style option to have the same editorconfig option name.
                        Debug.Assert(optionName == null || optionName == parts.optionName);
                        optionName ??= parts.optionName;

                        // Add code action to configure the optionValue.
                        nestedActions.Add(
                            SolutionChangeAction.Create(
                                parts.optionValue,
                                solution => ConfigurationUpdater.ConfigureCodeStyleOptionAsync(parts.optionName, parts.optionValue, diagnostic, isPerLanguage, project, cancellationToken),
                                parts.optionValue));
                    }
                }
            }
        }
    }
}
