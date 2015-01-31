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

        private readonly ComHandle<EnvDTE.FileCodeModel, FileCodeModel> _fileCodeModelHandle;

        private PartialTypeCollection(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            AbstractCodeType parent)
            : base(state, parent)
        {
            _fileCodeModelHandle = new ComHandle<EnvDTE.FileCodeModel, FileCodeModel>(fileCodeModel);
        }

        private FileCodeModel FileCodeModel
        {
            get { return _fileCodeModelHandle.Object; }
        }

        private AbstractCodeType ParentType
        {
            get { return (AbstractCodeType)this.Parent; }
        }

        private ImmutableArray<EnvDTE.CodeElement> GetParts()
        {
            var partsBuilder = ImmutableArray.CreateBuilder<EnvDTE.CodeElement>();
            var symbol = ParentType.LookupSymbol();

            foreach (var location in symbol.Locations.Where(l => l.IsInSource))
            {
                var tree = location.SourceTree;
                var document = this.Workspace.CurrentSolution.GetDocument(tree);

                var fileCodeModelObject = this.Workspace.GetFileCodeModel(document.Id);
                var fileCodeModel = ComAggregate.GetManagedObject<FileCodeModel>(fileCodeModelObject);

                var element = fileCodeModel.CodeElementFromPosition(location.SourceSpan.Start, ParentType.Kind);
                if (element != null)
                {
                    partsBuilder.Add(element);
                }
            }

            return partsBuilder.ToImmutable();
        }

        internal override Snapshot CreateSnapshot()
        {
            var parts = GetParts();
            return new CodeElementSnapshot(parts);
        }

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

        public override int Count
        {
            get { return GetParts().Length; }
        }
    }
}
