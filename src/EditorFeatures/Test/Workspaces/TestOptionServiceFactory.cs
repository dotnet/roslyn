// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
#if false
    [ExportWorkspaceServiceFactory(typeof(IOptionService), TestWorkspace.WorkspaceName)]
    internal class TestOptionServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IEnumerable<Lazy<IOptionProvider>> optionProviders;

        [ImportingConstructor]
        public TestOptionServiceFactory([ImportMany] IEnumerable<Lazy<IOptionProvider>> optionProviders)
        {
            this.optionProviders = optionProviders;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // We don't want any persistence at all
            return new OptionService(optionProviders, Enumerable.Empty<Lazy<IOptionSerializer, OptionSerializerMetadata>>());
        }
    }
#endif
}
