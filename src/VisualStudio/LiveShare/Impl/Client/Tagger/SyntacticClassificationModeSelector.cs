// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attempt to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    internal sealed class SyntacticClassificationModeSelector
    {
        private readonly AbstractLspClientServiceFactory _lspClientServiceFactory;
        private readonly ITextBuffer _buffer;

        private int _modeCache = -1;

        public SyntacticClassificationModeSelector(AbstractLspClientServiceFactory lspClientServiceFactory, ITextBuffer buffer)
        {
            _lspClientServiceFactory = lspClientServiceFactory;
            _buffer = buffer;
        }

        public static SyntacticClassificationModeSelector GetModeSelector(AbstractLspClientServiceFactory lspClientServiceFactory, ITextBuffer buffer)
        {
            return new SyntacticClassificationModeSelector(lspClientServiceFactory, buffer);
        }

        public SyntacticClassificationMode GetMode()
        {
            if (_modeCache != -1)
            {
                return (SyntacticClassificationMode)_modeCache;
            }

            if (!Workspace.TryGetWorkspace(_buffer.AsTextContainer(), out var workspace))
            {
                return SyntacticClassificationMode.TextMate;
            }

            var experimentService = workspace.Services.GetService<IExperimentationService>();
            if (experimentService == null)
            {
                return SyntacticClassificationMode.TextMate;
            }

            if (RoslynSyntaxClassificationService.ShouldRunExperiment(_lspClientServiceFactory, experimentService, WellKnownExperimentNames.SyntacticExp_LiveShareTagger_TextMate))
            {
                _modeCache = (int)SyntacticClassificationMode.TextMate;
            }
            else if (RoslynSyntaxClassificationService.ShouldRunExperiment(_lspClientServiceFactory, experimentService, WellKnownExperimentNames.SyntacticExp_LiveShareTagger_Remote))
            {
                _modeCache = (int)SyntacticClassificationMode.SyntaxLsp;
            }
            else
            {
                // tagger is disabled.
                _modeCache = (int)SyntacticClassificationMode.None;
            }

            return (SyntacticClassificationMode)_modeCache;
        }
    }
}
