﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class SourceGeneratorItem : BaseItem
    {
        public ProjectId ProjectId { get; }
        public ISourceGenerator Generator { get; }
        public AnalyzerReference AnalyzerReference { get; }

        public SourceGeneratorItem(ProjectId projectId, ISourceGenerator generator, AnalyzerReference analyzerReference)
            : base(name: generator.GetType().FullName)
        {
            ProjectId = projectId;
            Generator = generator;
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
