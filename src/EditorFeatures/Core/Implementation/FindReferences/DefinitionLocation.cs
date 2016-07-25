// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    /// <summary>
    /// Represent the location of a symbol definition that can be presented in 
    /// an editor and used to navigate to the symbol's origin.
    /// 
    /// Standard implmentations can be obtained through <see cref="CreateDocumentLocation"/>,
    /// <see cref="CreateNonNavigatingLocation"/> and <see cref="CreateSymbolLocation"/>.
    /// 
    /// Subclassing is also supported for scenarios that fall outside the bounds of
    /// these common cases.
    /// </summary>
    internal abstract class DefinitionLocation
    {
        /// <summary>
        /// Where the location originally came from (for example, the containing assembly or
        /// project name).  May be used in the presentation of a definition.
        /// </summary>
        public abstract ImmutableArray<TaggedText> OriginationParts { get; }

        protected DefinitionLocation()
        {
        }

        public abstract bool CanNavigateTo();
        public abstract bool TryNavigateTo();

        public static DefinitionLocation CreateDocumentLocation(DocumentLocation location)
        {
            return new DocumentDefinitionLocation(location);
        }

        public static DefinitionLocation CreateSymbolLocation(ISymbol symbol, Project referencingProject)
        {
            return new SymbolDefinitionLocation(symbol, referencingProject);
        }

        public static DefinitionLocation CreateNonNavigatingLocation(
            ImmutableArray<TaggedText> originationParts)
        {
            return new NonNavigatingDefinitionLocation(originationParts);
        }

        private sealed class DocumentDefinitionLocation : DefinitionLocation
        {
            private readonly DocumentLocation _location;

            public DocumentDefinitionLocation(DocumentLocation location)
            {
                _location = location;
            }

            public override ImmutableArray<TaggedText> OriginationParts =>
                ImmutableArray.Create(new TaggedText(TextTags.Text, _location.Document.Project.Name));

            public override bool CanNavigateTo()
            {
                return _location.CanNavigateTo();
            }

            public override bool TryNavigateTo()
            {
                return _location.TryNavigateTo();
            }
        }

        internal static ImmutableArray<TaggedText> GetOriginationParts(ISymbol symbol)
        {
            var assemblyName = symbol.ContainingAssembly?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return string.IsNullOrWhiteSpace(assemblyName)
                ? ImmutableArray<TaggedText>.Empty
                : ImmutableArray.Create(new TaggedText(TextTags.Assembly, assemblyName));
        }

        private sealed class SymbolDefinitionLocation : DefinitionLocation
        {
            private readonly Workspace _workspace;
            private readonly ProjectId _referencingProjectId;
            private readonly SymbolKey _symbolKey;
            private readonly ImmutableArray<TaggedText> _originationParts;

            public SymbolDefinitionLocation(ISymbol definition, Project project)
            {
                _workspace = project.Solution.Workspace;
                _referencingProjectId = project.Id;
                _symbolKey = definition.GetSymbolKey();
                _originationParts = GetOriginationParts(definition);
            }

            public override ImmutableArray<TaggedText> OriginationParts => _originationParts;

            public override bool CanNavigateTo()
            {
                return TryNavigateTo((symbol, project, service) => true);
            }

            public override bool TryNavigateTo()
            {
                return TryNavigateTo((symbol, project, service) =>
                    service.TryNavigateToSymbol(symbol, project));
            }

            private bool TryNavigateTo(Func<ISymbol, Project, ISymbolNavigationService, bool> action)
            {
                var symbol = ResolveSymbolInCurrentSolution();
                var referencingProject = _workspace.CurrentSolution.GetProject(_referencingProjectId);
                if (symbol == null || referencingProject == null)
                {
                    return false;
                }

                if (symbol.Kind == SymbolKind.Namespace)
                {
                    return false;
                }

                var navigationService = _workspace.Services.GetService<ISymbolNavigationService>();
                return action(symbol, referencingProject, navigationService);
            }

            private ISymbol ResolveSymbolInCurrentSolution()
            {
                var compilation = _workspace.CurrentSolution.GetProject(_referencingProjectId)
                                                            .GetCompilationAsync(CancellationToken.None)
                                                            .WaitAndGetResult(CancellationToken.None);
                return _symbolKey.Resolve(compilation).Symbol;
            }
        }

        private sealed class NonNavigatingDefinitionLocation : DefinitionLocation
        {
            private readonly ImmutableArray<TaggedText> _originationParts;

            public NonNavigatingDefinitionLocation(ImmutableArray<TaggedText> originationParts)
            {
                _originationParts = originationParts;
            }

            public override ImmutableArray<TaggedText> OriginationParts => _originationParts;

            public override bool CanNavigateTo()
            {
                return false;
            }

            public override bool TryNavigateTo()
            {
                return false;
            }
        }
    }

}
