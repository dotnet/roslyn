// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    public abstract class AbstractCodeElementCollection : AbstractCodeModelObject, ICodeElements
    {
        private readonly ParentHandle<object> _parentHandle;

        internal AbstractCodeElementCollection(
            CodeModelState state,
            object parent)
            : base(state)
        {
            Debug.Assert(parent != null);

            _parentHandle = new ParentHandle<object>(parent);
        }

        internal virtual Snapshot CreateSnapshot()
        {
            return new CodeElementSnapshot(this);
        }

        protected abstract bool TryGetItemByName(string name, out EnvDTE.CodeElement element);
        protected abstract bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element);

        protected bool MatchesNameOrIndex(string name, int index, string specifiedName, int specifiedIndex)
        {
            return (specifiedName != null && specifiedName == name)
                || (specifiedIndex != -1 && specifiedIndex == index);
        }

        public abstract int Count { get; }

        public int Item(object index, out EnvDTE.CodeElement element)
        {
            var elementIndex = -1;

            if (index is string elementName)
            {
                if (TryGetItemByName(elementName, out element))
                {
                    return VSConstants.S_OK;
                }
            }
            else if (index is int i)
            {
                elementIndex = i - 1;
                if (elementIndex >= 0 && TryGetItemByIndex(elementIndex, out element))
                {
                    return VSConstants.S_OK;
                }
            }

            element = null;
            return VSConstants.E_INVALIDARG;
        }

        public object Parent
        {
            get
            {
                return _parentHandle.Value;
            }
        }

        public bool CreateUniqueID(string prefix, ref string newName)
        {
            throw new NotImplementedException();
        }

        public void Reserved1(object element)
        {
            throw new NotImplementedException();
        }

        public virtual System.Collections.IEnumerator GetEnumerator()
        {
            return Enumerator.Create(CreateSnapshot());
        }
    }
}
