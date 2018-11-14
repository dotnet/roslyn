// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    [ExportWorkspaceService(typeof(IPullMemberUpOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioPullMemberUpService : IPullMemberUpOptionsService
    {
        private readonly IGlyphService _glyphService;

        private PullMemberUpViewModel ViewModel { get; set; }

        private AnalysisResult MembersInfo { get; set; }

        [ImportingConstructor]
        public VisualStudioPullMemberUpService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public PullMemberDialogResult GetPullTargetAndMembers(
            SemanticModel semanticModel,
            ISymbol selectedNodeSymbol)
        {
            ViewModel = new PullMemberUpViewModel(semanticModel, selectedNodeSymbol, _glyphService);
            var dialog = new PullMemberUpDialog(ViewModel);
            if (dialog.ShowModal().GetValueOrDefault())
            {
                var analysisResult = ViewModel.CreateAnaysisResult();
                return new PullMemberDialogResult(analysisResult);
            }
            else
            {
                return PullMemberDialogResult.CanceledResult;
            }
        }
    }
}
