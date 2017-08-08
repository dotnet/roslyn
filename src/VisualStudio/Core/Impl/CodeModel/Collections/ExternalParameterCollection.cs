// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class ExternalParameterCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            AbstractExternalCodeMember parent,
            ProjectId projectId)
        {
            var collection = new ExternalParameterCollection(state, parent, projectId);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private readonly ProjectId _projectId;

        private ExternalParameterCollection(
            CodeModelState state,
            AbstractExternalCodeMember parent,
            ProjectId projectId)
            : base(state, parent)
        {
            _projectId = projectId;
        }

        private AbstractExternalCodeMember ParentElement
        {
            get { return (AbstractExternalCodeMember)this.Parent; }
        }

        private ImmutableArray<IParameterSymbol> GetParameters()
        {
            var symbol = this.ParentElement.LookupSymbol();
            return symbol.GetParameters();
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var parameters = GetParameters();

            if (index < parameters.Length)
            {
                element = (EnvDTE.CodeElement)ExternalCodeParameter.Create(this.State, _projectId, parameters[index], this.ParentElement);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var parameters = GetParameters();
            var index = parameters.IndexOf(p => p.Name == name);

            if (index >= 0 && index < parameters.Length)
            {
                element = (EnvDTE.CodeElement)ExternalCodeParameter.Create(this.State, _projectId, parameters[index], this.ParentElement);
                return true;
            }

            element = null;
            return false;
        }

        public override int Count
        {
            get
            {
                var parameters = GetParameters();
                return parameters.Length;
            }
        }
    }
}
