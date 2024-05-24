// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Intents;

[IntentProvider(WellKnownIntents.DeleteParameter, LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DeleteParameterIntentProvider(IGlobalOptionService globalOptionService) : IIntentProvider
{
    private readonly IGlobalOptionService _globalOptionService = globalOptionService;

    public async Task<ImmutableArray<IntentProcessorResult>> ComputeIntentAsync(
        Document priorDocument,
        TextSpan priorSelection,
        Document currentDocument,
        IntentDataProvider intentDataProvider,
        CancellationToken cancellationToken)
    {
        var changeSignatureService = priorDocument.GetRequiredLanguageService<AbstractChangeSignatureService>();
        var contextResult = await changeSignatureService.GetChangeSignatureContextAsync(
            priorDocument, priorSelection.Start, restrictToDeclarations: false, _globalOptionService.CreateProvider(), cancellationToken).ConfigureAwait(false);

        if (contextResult is not ChangeSignatureAnalysisSucceededContext context)
        {
            return [];
        }

        var parameterIndexToDelete = context.ParameterConfiguration.SelectedIndex;
        var parameters = context.ParameterConfiguration.ToListOfParameters();
        var isExtensionMethod = context.ParameterConfiguration.ThisParameter != null;

        if (isExtensionMethod && parameterIndexToDelete == 0)
        {
            // We can't delete the 'this' parameter of an extension method.
            return [];
        }

        var newParameters = parameters.RemoveAt(parameterIndexToDelete);

        var signatureChange = new SignatureChange(context.ParameterConfiguration, ParameterConfiguration.Create(newParameters, isExtensionMethod, selectedIndex: 0));
        var changeSignatureOptionResult = new ChangeSignatureOptionsResult(signatureChange, previewChanges: false);

        var changeSignatureResult = await changeSignatureService.ChangeSignatureWithContextAsync(context, changeSignatureOptionResult, cancellationToken).ConfigureAwait(false);
        if (!changeSignatureResult.Succeeded)
        {
            return [];
        }

        var changedDocuments = changeSignatureResult.UpdatedSolution.GetChangedDocuments(priorDocument.Project.Solution).ToImmutableArray();
        return [new IntentProcessorResult(changeSignatureResult.UpdatedSolution, changedDocuments, EditorFeaturesResources.Change_Signature, WellKnownIntents.DeleteParameter)];
    }
}
