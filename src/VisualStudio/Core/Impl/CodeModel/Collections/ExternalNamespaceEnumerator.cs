// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    public sealed class ExternalNamespaceEnumerator : IEnumerator, ICloneable
    {
        internal static IEnumerator Create(CodeModelState state, ProjectId projectId, SymbolKey namespaceSymbolId)
        {
            var newEnumerator = new ExternalNamespaceEnumerator(state, projectId, namespaceSymbolId);
            return (IEnumerator)ComAggregate.CreateAggregatedObject(newEnumerator);
        }

        private ExternalNamespaceEnumerator(CodeModelState state, ProjectId projectId, SymbolKey namespaceSymbolId)
        {
            _state = state;
            _projectId = projectId;
            _namespaceSymbolId = namespaceSymbolId;

            _childEnumerator = ChildrenOfNamespace(state, projectId, namespaceSymbolId).GetEnumerator();
        }

        private readonly CodeModelState _state;
        private readonly ProjectId _projectId;
        private readonly SymbolKey _namespaceSymbolId;

        private readonly IEnumerator<EnvDTE.CodeElement> _childEnumerator;

        public object Current
        {
            get
            {
                return _childEnumerator.Current;
            }
        }

        public object Clone()
        {
            return Create(_state, _projectId, _namespaceSymbolId);
        }

        public bool MoveNext()
        {
            return _childEnumerator.MoveNext();
        }

        public void Reset()
        {
            _childEnumerator.Reset();
        }

        internal static IEnumerable<EnvDTE.CodeElement> ChildrenOfNamespace(CodeModelState state, ProjectId projectId, SymbolKey namespaceSymbolId)
        {
            var project = state.Workspace.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                throw Exceptions.ThrowEFail();
            }

            if (!(namespaceSymbolId.Resolve(project.GetCompilationAsync().Result).Symbol is INamespaceSymbol namespaceSymbol))
            {
                throw Exceptions.ThrowEFail();
            }

            var containingAssembly = project.GetCompilationAsync().Result.Assembly;

            foreach (var child in namespaceSymbol.GetMembers())
            {
                if (child is INamespaceSymbol namespaceChild)
                {
                    yield return (EnvDTE.CodeElement)ExternalCodeNamespace.Create(state, projectId, namespaceChild);
                }
                else
                {
                    var namedType = (INamedTypeSymbol)child;

                    if (namedType.IsAccessibleWithin(containingAssembly))
                    {
                        if (namedType.Locations.Any(l => l.IsInMetadata || l.IsInSource))
                        {
                            yield return state.CodeModelService.CreateCodeType(state, projectId, namedType);
                        }
                    }
                }
            }
        }
    }
}
