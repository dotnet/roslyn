// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.VisualStudio.Services.Interactive
{
    /// <summary>
    /// A class that implements the execution of ResetInteractive.
    /// Implementation is defined separately from command declaration in order
    /// to avoid the need to load the dll.
    /// </summary>
    internal abstract class AbstractResetInteractiveCommand
    {
        protected abstract string LanguageName { get; }
        protected abstract string ProjectKind { get; }

        protected abstract string CreateReference(string referenceName);
        protected abstract string CreateImport(string namespaceName);

        internal abstract void ExecuteResetInteractive();
    }
}
