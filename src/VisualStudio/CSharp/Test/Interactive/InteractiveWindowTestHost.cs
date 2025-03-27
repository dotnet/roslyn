// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive;

public sealed class InteractiveWindowTestHost : IDisposable
{
    internal readonly IInteractiveWindow Window;
    internal readonly TestInteractiveEvaluator Evaluator;

    internal InteractiveWindowTestHost(IInteractiveWindowFactoryService interactiveWindowFactory)
    {
        Evaluator = new TestInteractiveEvaluator();
        Window = interactiveWindowFactory.CreateWindow(Evaluator);
        Window.InitializeAsync().Wait();
    }

    public void Dispose()
    {
        if (Window != null)
        {
            // close interactive host process:
            Window.Evaluator?.Dispose();

            // dispose buffer:
            Window.Dispose();
        }
    }
}
