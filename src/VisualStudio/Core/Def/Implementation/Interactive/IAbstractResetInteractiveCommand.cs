// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
