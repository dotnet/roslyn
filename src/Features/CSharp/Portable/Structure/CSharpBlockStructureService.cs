// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public CSharpBlockStructureServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpBlockStructureService(languageServices.WorkspaceServices.Workspace);
        }
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
