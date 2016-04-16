// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
{
    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.RenameTracking)]
    internal sealed class RenameTrackingWaiter : AsynchronousOperationListener
    {
    }
}
