// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportLanguageServiceFactory(typeof(SignatureHelpService), LanguageNames.CSharp), Shared]
    internal class CSharpSignatureHelpServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpSignatureHelpService(languageServices.WorkspaceServices.Workspace);
        }
    }

    internal class CSharpSignatureHelpService : CommonSignatureHelpService
    {
        private readonly ImmutableArray<SignatureHelpProvider> _defaultProviders =
            ImmutableArray.Create<SignatureHelpProvider>(
                new AttributeSignatureHelpProvider(),
                new ConstructorInitializerSignatureHelpProvider(),
                new ElementAccessExpressionSignatureHelpProvider(),
                new GenericNamePartiallyWrittenSignatureHelpProvider(),
                new GenericNameSignatureHelpProvider(),
                new InvocationExpressionSignatureHelpProvider(),
                new ObjectCreationExpressionSignatureHelpProvider()
            );

        public CSharpSignatureHelpService(Workspace workspace)
            : base(workspace, LanguageNames.CSharp)
        {
        }

        protected override ImmutableArray<SignatureHelpProvider> GetBuiltInProviders()
        {
            return _defaultProviders;
        }
    }
}
