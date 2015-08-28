// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    // This doesn't need to be an EditorAdornmentWaiter, since we control our own 
    // adornment layer
    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.LineSeparators)]
    internal class LineSeparatorWaiter : AsynchronousOperationListener { }
}