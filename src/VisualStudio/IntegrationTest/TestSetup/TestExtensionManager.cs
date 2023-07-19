// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    /// <summary>This class causes a crash if an exception is encountered inside lightbulb extension points such as code fixes and code refactorings.</summary>
    /// <remarks>
    /// This class is exported as a workspace service with layer: <see cref="ServiceLayer.Host"/>. This ensures that TestExtensionManager
    /// is preferred over EditorLayerExtensionManager (which has layer: <see cref="ServiceLayer.Editor"/>) when running VS integration tests.
    /// </remarks>
    [Shared, ExportWorkspaceServiceFactory(typeof(IExtensionManager), ServiceLayer.Host)]
    internal class TestExtensionManager : IWorkspaceServiceFactory
    {
        private readonly TestExtensionErrorHandler _errorHandler;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestExtensionManager([Import] TestExtensionErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new ExtensionManager(_errorHandler);

        private class ExtensionManager : AbstractExtensionManager
        {
            private readonly TestExtensionErrorHandler _errorHandler;

            public ExtensionManager(TestExtensionErrorHandler errorHandler)
            {
                _errorHandler = errorHandler;
            }

            public override bool CanHandleException(object provider, Exception exception)
            {
                // This method will be called from within the 'when' clause in an exception filter. Therefore calling HandleException()
                // below will ensure that VS will crash with a more actionable dump as opposed to a dump that gets created after the
                // exception has already been caught.
                HandleException(provider, exception);
                return true;
            }

            public override void HandleException(object provider, Exception exception)
                => _errorHandler.HandleError(provider, exception);
        }
    }
}
