// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Editor.InteractiveWindow;

namespace Roslyn.VisualStudio.InteractiveWindow
{
    public interface IVsInteractiveWindowFactory
    {
        IVsInteractiveWindow Create(Guid providerId, int instanceId, string title, IInteractiveEvaluator evaluator);
    }
}
