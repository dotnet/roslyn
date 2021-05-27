// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(AnalyzerItemSourceProvider))]
    [Order]
    [AppliesToProject("(CSharp | VB) & !CPS")]  // in the CPS case, the Analyzers items are created by the project system
    internal sealed class AnalyzerItemSourceProvider : AttachedCollectionSourceProvider<AnalyzersFolderItem>
    {
        [Import(typeof(AnalyzersCommandHandler))]
        private readonly IAnalyzersCommandHandler _commandHandler = null;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerItemSourceProvider()
        {
        }

        protected override IAttachedCollectionSource CreateCollectionSource(AnalyzersFolderItem analyzersFolder, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                return new AnalyzerItemSource(analyzersFolder, _commandHandler);
            }

            return null;
        }
    }
}
