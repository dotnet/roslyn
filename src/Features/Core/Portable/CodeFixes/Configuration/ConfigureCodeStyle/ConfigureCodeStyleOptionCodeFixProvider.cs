// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle;

[ExportConfigurationFixProvider(PredefinedConfigurationFixProviderNames.ConfigureCodeStyleOption, LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
[ExtensionOrder(Before = PredefinedConfigurationFixProviderNames.ConfigureSeverity)]
[ExtensionOrder(After = PredefinedConfigurationFixProviderNames.Suppression)]
internal sealed partial class ConfigureCodeStyleOptionCodeFixProvider : IConfigurationFixProvider
{
    private static readonly ImmutableArray<bool> s_boolValues = [true, false];

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
        return IDEDiagnosticIdToOptionMappingHelper.TryGetMappedOptions(diagnostic.Id, language, out _);
    }

    public FixAllProvider? GetFixAllProvider()
        => null;

    public Task<ImmutableArray<CodeFix>> GetFixesAsync(TextDocument document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        => Task.FromResult(GetConfigurations(document.Project, diagnostics));

    public Task<ImmutableArray<CodeFix>> GetFixesAsync(Project project, IEnumerable<Diagnostic> diagnostics, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        => Task.FromResult(GetConfigurations(project, diagnostics));

    private static ImmutableArray<CodeFix> GetConfigurations(Project project, IEnumerable<Diagnostic> diagnostics)
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
            var hasMultipleOptions = codeStyleOptions.Length > 1;
            foreach (var option in codeStyleOptions)
            {
                var topLevelAction = GetCodeActionForCodeStyleOption(option, diagnostic, hasMultipleOptions);
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
        TopLevelConfigureCodeStyleOptionCodeAction? GetCodeActionForCodeStyleOption(IOption2 option, Diagnostic diagnostic, bool hasMultipleOptions)
        {
            // Add a code action for every valid value of the given code style option.
            // We only support light-bulb configuration of code style options with boolean or enum values.

            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var nestedActions);

            // Try to get the parsed editorconfig string representation of the new code style option value
            var optionName = option.Definition.ConfigName;
            var defaultValue = (ICodeStyleOption?)option.DefaultValue;
            Contract.ThrowIfNull(defaultValue);

            if (defaultValue.Value is bool)
            {
                foreach (var boolValue in s_boolValues)
                {
                    AddCodeActionWithOptionValue(defaultValue, boolValue);
                }
            }
            else if (defaultValue.Value?.GetType() is Type t && t.IsEnum)
            {
                foreach (var enumValue in Enum.GetValues(t))
                {
                    AddCodeActionWithOptionValue(defaultValue, enumValue!);
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
                var optionValue = option.Definition.Serializer.Serialize(configuredCodeStyleOption);

                // Add code action to configure the optionValue.
                nestedActions.Add(
                    SolutionChangeAction.Create(
                        optionValue,
                        cancellationToken => ConfigurationUpdater.ConfigureCodeStyleOptionAsync(optionName, optionValue, diagnostic, option.IsPerLanguage, project, cancellationToken),
                        optionValue));
            }
        }
    }
}
