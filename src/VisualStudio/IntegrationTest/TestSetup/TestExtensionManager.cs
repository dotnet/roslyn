// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Roslyn.VisualStudio.Test.Setup
{
    /// <summary>
    /// This class causes a crash if an exception is encountered inside lightbulb extension points such as code fixes and code refactorings.
    /// </summary>
    /// <remarks>
    /// This class is exported as a workspace service with layer: <see cref="ServiceLayer.Host"/>. This ensures that TestExtensionManager
    /// is preferred over EditorLayerExtensionManager (which has layer: <see cref="ServiceLayer.Editor"/>) when running VS integration tests./>
    /// </remarks>
    [Shared, ExportWorkspaceServiceFactory(typeof(IExtensionManager), ServiceLayer.Host)]
    internal class TestExtensionManager : IWorkspaceServiceFactory
    {
        private readonly TestExtensionErrorHandler errorHandler;

        [ImportingConstructor]
        public TestExtensionManager([Import]TestExtensionErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new ExtensionManager(errorHandler);
        }

        private class ExtensionManager : AbstractExtensionManager
        {
            private readonly TestExtensionErrorHandler errorHandler;

            public ExtensionManager(TestExtensionErrorHandler errorHandler)
            {
                this.errorHandler = errorHandler;
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
            {
                errorHandler.HandleError(provider, exception);
            }
        }
    }
}