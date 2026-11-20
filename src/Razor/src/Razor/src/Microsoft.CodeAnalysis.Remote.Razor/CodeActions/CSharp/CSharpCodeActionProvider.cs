// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(ICSharpCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class CSharpCodeActionProvider(IClientSettingsManager clientSettingsManager) : ICSharpCodeActionProvider
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

    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;

    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        CancellationToken cancellationToken)
    {
        // Used to identify if this is VSCode which doesn't support
        // code action resolve.
        if (!context.SupportsCodeActionResolve)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var root = context.CodeDocument.GetRequiredSyntaxRoot();
        var node = root.FindInnermostNode(context.StartAbsoluteIndex);
        var isInImplicitExpression = node?.AncestorsAndSelf().Any(n => n is CSharpImplicitExpressionSyntax) ?? false;

        var allowList = isInImplicitExpression
            ? SupportedImplicitExpressionCodeActionNames
            : SupportedDefaultCodeActionNames;
        var showAllCSharpCodeActions = _clientSettingsManager.GetClientSettings().AdvancedSettings.ShowAllCSharpCodeActions;

        using var results = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var codeAction in codeActions)
        {
            var isOnAllowList = codeAction.Name is not null && allowList.Contains(codeAction.Name);

            // If this code action isn't on the allow list, it might have been handled by another provider, which means
            // it will already have been wrapped, so we have to check not to double-wrap it.
            if (showAllCSharpCodeActions &&
                IsRazorCodeActionResolutionData(codeAction.Data))
            {
                // This code action has already been wrapped by something else, so skip it here, or it could
                // be marked as untested when it isn't, and more importantly would be duplicated in the list.
                continue;
            }

            if (showAllCSharpCodeActions || isOnAllowList)
            {
                results.Add(codeAction.WrapResolvableCodeAction(context, isOnAllowList: isOnAllowList));
            }
        }

        return Task.FromResult(results.ToImmutable());

        static bool IsRazorCodeActionResolutionData(object? data)
        {
            if (data is not JsonElement { ValueKind: JsonValueKind.Object } element)
            {
                return false;
            }

            // There is no TryDeserialize API. Malformed delegated data just means that Razor hasn't wrapped it.
            try
            {
                return element.Deserialize<RazorCodeActionResolutionParams>() is not null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
