// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class PartialTypeCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            AbstractCodeType parent)
        {
            var collection = new PartialTypeCollection(state, fileCodeModel, parent);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private PartialTypeCollection(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            AbstractCodeType parent)
            : base(state, parent)
        {
        }

        private ImmutableArray<EnvDTE.CodeElement> _parts;

        private AbstractCodeType ParentType => (AbstractCodeType)this.Parent;

        private ImmutableArray<EnvDTE.CodeElement> GetParts()
        {
            // Retrieving the parts is potentially very expensive because it can force multiple FileCodeModels to be instantiated.
            // Here, we cache the result to avoid having to perform these calculations each time GetParts() is called.
            // This *could* be an issue because it means that a PartialTypeCollection will not necessarily reflect the
            // current state of the user's code. However, because a new PartialTypeCollection is created every time the Parts
            // property is accessed on CodeClass, CodeStruct or CodeInterface, consumers would hit this behavior rarely.
            if (_parts == null)
            {
                var partsBuilder = ImmutableArray.CreateBuilder<EnvDTE.CodeElement>();

                var solution = this.Workspace.CurrentSolution;
                var symbol = ParentType.LookupSymbol();

                foreach (var location in symbol.Locations.Where(l => l.IsInSource))
                {
                    var document = solution.GetDocument(location.SourceTree);
                    if (document != null)
                    {
                        var fileCodeModelObject = this.Workspace.GetFileCodeModel(document.Id);
                        if (fileCodeModelObject != null)
                        {
                            var fileCodeModel = ComAggregate.GetManagedObject<FileCodeModel>(fileCodeModelObject);

                            var element = fileCodeModel.CodeElementFromPosition(location.SourceSpan.Start, ParentType.Kind);
                            if (element != null)
                            {
                                partsBuilder.Add(element);
                            }
                        }
                    }
                }

                _parts = partsBuilder.ToImmutable();
            }

            return _parts;
        }

        internal override Snapshot CreateSnapshot() => new CodeElementSnapshot(GetParts());

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var parts = GetParts();
            if (index < parts.Length)
            {
                element = parts[index];
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            foreach (var part in GetParts())
            {
                if (part.Name == name)
                {
                    element = part;
                    return true;
                }
            }

            element = null;
            return false;
        }

        public override int Count => GetParts().Length;
    }
}
