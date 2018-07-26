// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Progression;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public static class VisualStudioTestExportProvider
    {
        public static readonly IExportProviderFactory Factory;

        static VisualStudioTestExportProvider()
        {
            Factory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(
                    ExportProviderCache.GetOrCreateAssemblyCatalog(typeof(CSharpCodeModelService).Assembly)
                        .WithPart(typeof(FakeVsServiceProvider))
                        .WithPart(typeof(FakePrimaryWorkspaceProvider))));
        }

        [Export(typeof(SVsServiceProvider))]
        [Shared]
        [PartNotDiscoverable]
        internal sealed class FakeVsServiceProvider : SVsServiceProvider, IServiceProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public FakeVsServiceProvider()
            {
            }

            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }
        }

        [Export(typeof(IProgressionPrimaryWorkspaceProvider))]
        [Shared]
        [PartNotDiscoverable]
        private sealed class FakePrimaryWorkspaceProvider : IProgressionPrimaryWorkspaceProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public FakePrimaryWorkspaceProvider()
            {
            }

            public Workspace PrimaryWorkspace => throw new NotImplementedException();
        }
    }
}
