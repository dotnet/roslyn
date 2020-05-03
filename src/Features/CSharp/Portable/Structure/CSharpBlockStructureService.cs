// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    [ExportLanguageServiceFactory(typeof(BlockStructureService), LanguageNames.CSharp), Shared]
    internal class CSharpBlockStructureServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpBlockStructureServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new CSharpBlockStructureService(languageServices.WorkspaceServices.Workspace);
    }

    internal class CSharpBlockStructureService : BlockStructureServiceWithProviders
    {
        public CSharpBlockStructureService(Workspace workspace) : base(workspace)
        {
        }

        protected override ImmutableArray<BlockStructureProvider> GetBuiltInProviders()
        {
            return ImmutableArray.Create<BlockStructureProvider>(
                new CSharpBlockStructureProvider());
        }

        public override string Language => LanguageNames.CSharp;
    }
}
