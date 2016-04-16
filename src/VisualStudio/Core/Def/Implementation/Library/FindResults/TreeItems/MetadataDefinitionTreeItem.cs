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
        private readonly bool _canGoToDefinition;
        private readonly Workspace _workspace;
        private readonly ProjectId _referencingProjectId;

        public MetadataDefinitionTreeItem(Workspace workspace, ISymbol definition, ProjectId referencingProjectId, ushort glyphIndex)
            : base(glyphIndex)
        {
            _workspace = workspace;
            _referencingProjectId = referencingProjectId;
            _symbolKey = definition.GetSymbolKey();
            _assemblyName = definition.ContainingAssembly?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            _symbolDefinition = definition.ToDisplayString(definitionDisplayFormat);
            _canGoToDefinition = definition.Kind != SymbolKind.Namespace;

            this.DisplayText = $"{GetAssemblyNameString()}{_symbolDefinition}";
        }

        public override bool CanGoToDefinition()
        {
            return _canGoToDefinition;
        }

        public override int GoToSource()
        {
            var symbol = ResolveSymbolInCurrentSolution();
            var referencingProject = _workspace.CurrentSolution.GetProject(_referencingProjectId);
            if (symbol != null && referencingProject != null)
            {
                var navigationService = _workspace.Services.GetService<ISymbolNavigationService>();
                return navigationService.TryNavigateToSymbol(symbol, referencingProject, cancellationToken: CancellationToken.None)
                    ? VSConstants.S_OK
                    : VSConstants.E_FAIL;
            }

            return VSConstants.E_FAIL;
        }

        private ISymbol ResolveSymbolInCurrentSolution()
        {
            return _symbolKey.Resolve(_workspace.CurrentSolution.GetProject(_referencingProjectId).GetCompilationAsync(CancellationToken.None).Result).Symbol;
        }

        internal override void SetReferenceCount(int referenceCount)
        {
            var referenceCountDisplay = referenceCount == 1
                ? ServicesVSResources.ReferenceCountSingular
                : string.Format(ServicesVSResources.ReferenceCountPlural, referenceCount);

            this.DisplayText = $"{GetAssemblyNameString()}{_symbolDefinition} ({referenceCountDisplay})";
        }

        private string GetAssemblyNameString()
        {
            return (_assemblyName != null && _canGoToDefinition) ? $"[{_assemblyName}] " : string.Empty;
        }
    }
}
