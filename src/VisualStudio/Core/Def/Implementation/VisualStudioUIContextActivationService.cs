// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[Export(typeof(IUIContextActivationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioUIContextActivationService() : IUIContextActivationService
{
    public void ExecuteWhenActivated(Guid uiContext, Action action)
    {
        var context = UIContext.FromUIContextGuid(uiContext);
        context.WhenActivated(action);
    }
}
