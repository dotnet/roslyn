// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Roslyn.VisualStudio.Test.Utilities.Remoting
{
    /// <summary>Provides a means of accessing the <see cref="IInteractiveWindow"/> service in the Visual Studio host.</summary>
    /// <remarks>This object exists in the Visual Studio host and is marhsalled across the process boundary.</remarks>
    internal class InteractiveWindowWrapper : MarshalByRefObject
    {
        private IInteractiveWindow _interactiveWindow;

        public InteractiveWindowWrapper(IInteractiveWindow interactiveWindow)
        {
            _interactiveWindow = interactiveWindow;
        }

        public string CurrentSnapshotText
            => _interactiveWindow.TextView.TextBuffer.CurrentSnapshot.GetText();

        public bool IsInitializing
            => _interactiveWindow.IsInitializing;

        public void Submit(string text)
            => _interactiveWindow.SubmitAsync(new[] { text }).GetAwaiter().GetResult();
    }
}
