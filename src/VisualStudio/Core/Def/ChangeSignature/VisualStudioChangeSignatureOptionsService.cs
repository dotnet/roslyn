// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;

[ExportWorkspaceService(typeof(IChangeSignatureOptionsService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioChangeSignatureOptionsService(
    IClassificationFormatMapService classificationFormatMapService,
    ClassificationTypeMap classificationTypeMap,
    IThreadingContext threadingContext) : IChangeSignatureOptionsService
{
    private readonly IClassificationFormatMap _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("tooltip");
    private readonly ClassificationTypeMap _classificationTypeMap = classificationTypeMap;
    private readonly IThreadingContext _threadingContext = threadingContext;

    public ChangeSignatureOptionsResult? GetChangeSignatureOptions(
        Document document,
        int positionForTypeBinding,
        ISymbol symbol,
        ParameterConfiguration parameters)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var viewModel = new ChangeSignatureDialogViewModel(
            parameters,
            symbol,
            document,
            positionForTypeBinding,
            _classificationFormatMap,
            _classificationTypeMap);

        ChangeSignatureLogger.LogChangeSignatureDialogLaunched();

        var dialog = new ChangeSignatureDialog(viewModel);
        var result = dialog.ShowModal();

        if (result.HasValue && result.Value)
        {
            ChangeSignatureLogger.LogChangeSignatureDialogCommitted();

            var signatureChange = new SignatureChange(parameters, viewModel.GetParameterConfiguration());
            signatureChange.LogTelemetry();

            return new ChangeSignatureOptionsResult(signatureChange, previewChanges: viewModel.PreviewChanges);
        }

        return null;
    }
}
