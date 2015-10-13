// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class ExternalMemberCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            object parent,
            ProjectId projectId,
            ITypeSymbol typeSymbol)
        {
            var collection = new ExternalMemberCollection(state, parent, projectId, typeSymbol);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private readonly ProjectId _projectId;
        private readonly SymbolKey _typeSymbolId;
        private ImmutableArray<EnvDTE.CodeElement> _children;

        private ExternalMemberCollection(CodeModelState state, object parent, ProjectId projectId, ITypeSymbol typeSymbol)
            : base(state, parent)
        {
            _projectId = projectId;
            _typeSymbolId = typeSymbol.GetSymbolKey();
        }

        private ImmutableArray<EnvDTE.CodeElement> GetChildren()
        {
            if (_children == null)
            {
                var project = this.State.Workspace.CurrentSolution.GetProject(_projectId);
                if (project == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                var typeSymbol = _typeSymbolId.Resolve(project.GetCompilationAsync().Result).Symbol as ITypeSymbol;
                if (typeSymbol == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                var childrenBuilder = ImmutableArray.CreateBuilder<EnvDTE.CodeElement>();

                foreach (var member in typeSymbol.GetMembers())
                {
                    if (this.CodeModelService.IsValidExternalSymbol(member))
                    {
                        childrenBuilder.Add(this.State.CodeModelService.CreateExternalCodeElement(this.State, _projectId, member));
                    }
                }

                foreach (var typeMember in typeSymbol.GetTypeMembers())
                {
                    childrenBuilder.Add(this.State.CodeModelService.CreateExternalCodeElement(this.State, _projectId, typeMember));
                }

                _children = childrenBuilder.ToImmutable();
            }

            return _children;
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var children = GetChildren();
            if (index < children.Length)
            {
                element = children[index];
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var children = GetChildren();
            var index = children.IndexOf(e => e.Name == name);

            if (index < children.Length)
            {
                element = children[index];
                return true;
            }

            element = null;
            return false;
        }

        public override int Count
        {
            get { return GetChildren().Length; }
        }
    }
}
