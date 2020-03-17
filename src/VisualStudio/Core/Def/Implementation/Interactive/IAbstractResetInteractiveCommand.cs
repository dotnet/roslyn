// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.VisualStudio.Services.Interactive
{
    /// <summary>
    /// An interface that implements the execution of ResetInteractive.
    /// Implementation is defined separately from command declaration in order
    /// to avoid the need to load the dll.
    /// </summary>
    internal interface IResetInteractiveCommand
    {
        void ExecuteResetInteractive();
    }
}
