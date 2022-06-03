// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class SourceGeneratorItem : BaseItem
    {
        public ProjectId ProjectId { get; }

        public string GeneratorAssemblyName { get; }

        // Since the type name is also used for the display text, we can just reuse that. We'll still have an explicit
        // property so the assembly name/type name pair that is used in other places is also used here.
        public string GeneratorTypeName => base.Text;
        public AnalyzerReference AnalyzerReference { get; }

        public SourceGeneratorItem(ProjectId projectId, ISourceGenerator generator, AnalyzerReference analyzerReference)
            : base(name: SourceGeneratedDocumentIdentity.GetGeneratorTypeName(generator))
        {
            ProjectId = projectId;
            GeneratorAssemblyName = SourceGeneratedDocumentIdentity.GetGeneratorAssemblyName(generator);
            AnalyzerReference = analyzerReference;
        }

        // TODO: do we need an icon for our use?
        public override ImageMoniker IconMoniker => KnownMonikers.Process;

        public override object GetBrowseObject()
        {
            return new BrowseObject(this);
        }
    }
}
