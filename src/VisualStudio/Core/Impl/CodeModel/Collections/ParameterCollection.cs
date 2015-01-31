// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class ParameterCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            AbstractCodeMember parent)
        {
            var collection = new ParameterCollection(state, parent);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private ParameterCollection(
            CodeModelState state,
            AbstractCodeMember parent)
            : base(state, parent)
        {
        }

        private AbstractCodeMember ParentElement
        {
            get { return (AbstractCodeMember)Parent; }
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var parameters = this.ParentElement.GetParameters();

            if (index < parameters.Length)
            {
                var parameter = parameters[index];
                element = (EnvDTE.CodeElement)CodeParameter.Create(this.State, this.ParentElement, parameter.Name);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            foreach (var parameter in this.ParentElement.GetParameters())
            {
                var childName = parameter.Name;
                if (childName == name)
                {
                    element = (EnvDTE.CodeElement)CodeParameter.Create(this.State, this.ParentElement, parameter.Name);
                    return true;
                }
            }

            element = null;
            return false;
        }

        public override int Count
        {
            get
            {
                return ParentElement.GetParameters().Length;
            }
        }
    }
}
