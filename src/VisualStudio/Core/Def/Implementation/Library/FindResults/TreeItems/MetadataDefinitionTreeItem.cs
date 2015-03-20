// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class MetadataDefinitionTreeItem : AbstractTreeItem
    {
        private readonly string _assemblyName;
        private readonly string _symbolDefinition;
        private readonly SymbolKey _symbolKey;
        private readonly Workspace _workspace;
        private readonly ProjectId _referencingProjectId;

        public MetadataDefinitionTreeItem(Workspace workspace, ISymbol definition, ProjectId referencingProjectId, ushort glyphIndex)
            : base(glyphIndex)
        {
            _workspace = workspace;
            _referencingProjectId = referencingProjectId;
            _symbolKey = definition.GetSymbolKey();
            _assemblyName = definition.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            _symbolDefinition = definition.ToDisplayString(definitionDisplayFormat);
            this.DisplayText = $"[{_assemblyName}] {_symbolDefinition}";
        }

        public override int GoToSource()
        {
            var resolution = _symbolKey.Resolve(_workspace.CurrentSolution.GetCompilationAsync(_referencingProjectId, CancellationToken.None).Result);
            var referencingProject = _workspace.CurrentSolution.GetProject(_referencingProjectId);
            if (resolution.Symbol != null && referencingProject != null)
            {
                var navigationService = _workspace.Services.GetService<ISymbolNavigationService>();
                return navigationService.TryNavigateToSymbol(resolution.Symbol, referencingProject)
                    ? VSConstants.S_OK
                    : VSConstants.E_FAIL;
            }

            return VSConstants.E_FAIL;
        }

        internal override void SetReferenceCount(int referenceCount)
        {
            var referenceCountDisplay = referenceCount == 1
                ? ServicesVSResources.ReferenceCountSingular
                : string.Format(ServicesVSResources.ReferenceCountPlural, referenceCount);

            this.DisplayText = $"[{_assemblyName}] {_symbolDefinition} ({referenceCountDisplay})";
        }
    }
}
