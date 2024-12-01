// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.IntegrationTest.Setup;

/// <summary>This class causes a crash if an exception is encountered inside lightbulb extension points such as code fixes and code refactorings.</summary>
/// <remarks>
/// This class is exported as a workspace service with layer: <see cref="ServiceLayer.Host"/>. This ensures that TestExtensionManager
/// is preferred over EditorLayerExtensionManager (which has layer: <see cref="ServiceLayer.Editor"/>) when running VS integration tests.
/// </remarks>
[Shared, ExportWorkspaceService(typeof(IExtensionManager), ServiceLayer.Host)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TestExtensionManager(
    [Import] TestExtensionErrorHandler errorHandler) : AbstractExtensionManager
{
    protected override void HandleNonCancellationException(object provider, Exception exception)
    {
        Debug.Assert(exception is not OperationCanceledException);
        errorHandler.HandleError(provider, exception);
    }
}
