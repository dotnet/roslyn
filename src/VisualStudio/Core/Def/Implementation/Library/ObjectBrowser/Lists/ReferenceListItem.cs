// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
{
    internal class ReferenceListItem : ObjectListItem
    {
        private readonly string _name;
        private readonly MetadataReference _reference;

        public ReferenceListItem(ProjectId projectId, string name, MetadataReference reference)
            : base(projectId, StandardGlyphGroup.GlyphAssembly)
        {
            _name = name;
            _reference = reference;
        }

        public override string DisplayText => _name;

        public override string FullNameText => _name;

        public override string SearchText => _name;

        public MetadataReference MetadataReference => _reference;

        public IAssemblySymbol GetAssembly(Compilation compilation)
        {
            return compilation.GetAssemblyOrModuleSymbol(_reference) as IAssemblySymbol;
        }
    }
}
