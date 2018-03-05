// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class EmptyCollection : AbstractCodeElementCollection
    {
        private static readonly Snapshot s_snapshot = new CodeElementSnapshot(ImmutableArray.Create<EnvDTE.CodeElement>());

        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            object parent)
        {
            var collection = new EmptyCollection(state, parent);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private EmptyCollection(
            CodeModelState state,
            object parent)
            : base(state, parent)
        {
        }

        internal override Snapshot CreateSnapshot()
        {
            return s_snapshot;
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            element = null;
            return false;
        }

        public override int Count
        {
            get { return 0; }
        }
    }
}
