// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(ICSharpCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class CSharpCodeActionProvider(LanguageServerFeatureOptions languageServerFeatureOptions, ILoggerFactory loggerFactory) : ICSharpCodeActionProvider
{
    // Internal for testing
    internal static readonly HashSet<string> SupportedDefaultCodeActionNames =
    [
        PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers,
        PredefinedCodeRefactoringProviderNames.AddAwait,
        PredefinedCodeRefactoringProviderNames.AddDebuggerDisplay,
        PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter, // Create and assign (property|field)
        PredefinedCodeRefactoringProviderNames.AddParameterCheck, // Add Null checks
        PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers,
        PredefinedCodeFixProviderNames.GenerateDefaultConstructors,
        PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
        PredefinedCodeRefactoringProviderNames.UseExpressionBody,
        PredefinedCodeRefactoringProviderNames.IntroduceVariable,
        PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimInterpolatedString,
        PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimString,
        PredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString,
        PredefinedCodeRefactoringProviderNames.ConvertPlaceholderToInterpolatedString,
        PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString,
        PredefinedCodeFixProviderNames.ImplementAbstractClass,
        PredefinedCodeFixProviderNames.ImplementInterface,
        PredefinedCodeFixProviderNames.RemoveUnusedVariable,
        PredefinedCodeFixProviderNames.GenerateConversion,
        PredefinedCodeFixProviderNames.GenerateConstructor,
        PredefinedCodeFixProviderNames.GenerateDeconstructMethod,
        PredefinedCodeFixProviderNames.GenerateMethod,
        PredefinedCodeFixProviderNames.GenerateType,
        PredefinedCodeFixProviderNames.GenerateVariable,
    ];

    internal static readonly HashSet<string> SupportedImplicitExpressionCodeActionNames =
    [
        PredefinedCodeFixProviderNames.GenerateConstructor,
        PredefinedCodeFixProviderNames.GenerateMethod,
        PredefinedCodeFixProviderNames.GenerateType,
        PredefinedCodeFixProviderNames.GenerateVariable,
    ];

    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CSharpCodeActionProvider>();

    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        CancellationToken cancellationToken)
    {
        LogCodeActionTrace($"CSharpProvider.Entry: SupportsResolve={context.SupportsCodeActionResolve}, ShowAllCSharpCodeActions={_languageServerFeatureOptions.ShowAllCSharpCodeActions}, InputCount={codeActions.Length}, Input=[{FormatCodeActions(codeActions)}]");

        // Used to identify if this is VSCode which doesn't support
        // code action resolve.
        if (!context.SupportsCodeActionResolve)
        {
            LogCodeActionTrace("CSharpProvider.ExitNoResolveSupport");
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var root = context.CodeDocument.GetRequiredSyntaxRoot();
        var node = root.FindInnermostNode(context.StartAbsoluteIndex);
        var isInImplicitExpression = node?.AncestorsAndSelf().Any(n => n is CSharpImplicitExpressionSyntax) ?? false;

        var allowList = isInImplicitExpression
            ? SupportedImplicitExpressionCodeActionNames
            : SupportedDefaultCodeActionNames;

        LogCodeActionTrace($"CSharpProvider.Context: Node='{node?.GetType().Name ?? "<null>"}', IsInImplicitExpression={isInImplicitExpression}, AllowList=[{string.Join(", ", allowList)}]");

        using var results = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var codeAction in codeActions)
        {
            var isOnAllowList = codeAction.Name is not null && allowList.Contains(codeAction.Name);
            LogCodeActionTrace($"CSharpProvider.Consider: IsOnAllowList={isOnAllowList}, Action={FormatCodeAction(codeAction)}");

            // If this code action isn't on the allow list, it might have been handled by another provider, which means
            // it will already have been wrapped, so we have to check not to double-wrap it.
            if (_languageServerFeatureOptions.ShowAllCSharpCodeActions &&
                CanDeserializeTo<RazorCodeActionResolutionParams>(codeAction.Data))
            {
                // This code action has already been wrapped by something else, so skip it here, or it could
                // be marked as experimental when its not, and more importantly would be duplicated in the list.
                LogCodeActionTrace($"CSharpProvider.SkipAlreadyWrapped: Action={FormatCodeAction(codeAction)}");
                continue;
            }

            if (_languageServerFeatureOptions.ShowAllCSharpCodeActions || isOnAllowList)
            {
                var wrappedAction = codeAction.WrapResolvableCodeAction(context, isOnAllowList: isOnAllowList);
                LogCodeActionTrace($"CSharpProvider.Keep: WrappedAction={FormatCodeAction(wrappedAction)}");
                results.Add(wrappedAction);
            }
            else
            {
                LogCodeActionTrace($"CSharpProvider.DropNotAllowed: Action={FormatCodeAction(codeAction)}");
            }
        }

        var result = results.ToImmutable();
        LogCodeActionTrace($"CSharpProvider.Exit: Count={result.Length}, Results=[{FormatCodeActions(result)}]");
        return Task.FromResult(result);

        static bool CanDeserializeTo<T>(object? data)
        {
            // We don't care about errors here, and there is no TryDeserialize method, so we can just brute force this.
            // Since this only happens if the feature flag is on, which is internal only and intended only for users of
            // this repo, any perf hit here isn't going to affect real users.
            try
            {
                if (data is JsonElement element &&
                    element.Deserialize<RazorCodeActionResolutionParams>() is not null)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }
    }

    private void LogCodeActionTrace(string message)
    {
        var fullMessage = $"RazorCodeActionTrace {message}";
        _logger.LogDebug(fullMessage);
        Trace.WriteLine(fullMessage);
    }

    private static string FormatCodeActions(ImmutableArray<RazorVSInternalCodeAction> codeActions)
        => codeActions.IsEmpty
            ? "<empty>"
            : string.Join("; ", codeActions.Select(static (action, index) => $"{index}: {FormatCodeAction(action)}"));

    private static string FormatCodeAction(RazorVSInternalCodeAction action)
        => $"Name='{action.Name ?? "<null>"}', Title='{action.Title}', Kind='{action.Kind?.ToString() ?? "<null>"}', Group='{action.Group ?? "<null>"}', Children={action.Children?.Length ?? 0}, Command='{action.Command?.CommandIdentifier ?? "<null>"}', Data={FormatData(action.Data)}";

    private static string FormatData(object? data)
    {
        if (data is null)
        {
            return "<null>";
        }

        if (data is not JsonElement jsonData)
        {
            return data.GetType().FullName ?? data.GetType().Name;
        }

        var customTags = jsonData.TryGetProperty("CustomTags", out var customTagsValue) && customTagsValue.ValueKind == JsonValueKind.Array
            ? $"CustomTags=[{string.Join(", ", customTagsValue.EnumerateArray().Select(static tag => tag.GetString()))}]"
            : "CustomTags=<missing>";

        var fixAllFlavors = jsonData.TryGetProperty("FixAllFlavors", out var fixAllFlavorsValue) && fixAllFlavorsValue.ValueKind == JsonValueKind.Array
            ? $"FixAllFlavors={fixAllFlavorsValue.GetArrayLength()}"
            : "FixAllFlavors=<missing>";

        return $"JsonElement{{ValueKind={jsonData.ValueKind}, {customTags}, {fixAllFlavors}}}";
    }
}
