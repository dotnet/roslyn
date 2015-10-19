// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        internal enum State
        {
            /// <summary>
            /// Initial state.  <see cref="IInteractiveWindow.InitializeAsync"/> hasn't been called.
            /// Transition to <see cref="Initializing"/> when <see cref="IInteractiveWindow.InitializeAsync"/> is called.
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            Starting,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindow.InitializeAsync"/>.
            /// Transition to <see cref="WaitingForInput"/> when finished (in <see cref="UIThreadOnly.ProcessPendingSubmissions"/>).
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            Initializing,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindowOperations.ResetAsync"/>.
            /// Transition to <see cref="WaitingForInput"/> when finished (in <see cref="UIThreadOnly.ProcessPendingSubmissions"/>).
            /// Transition to <see cref="ResettingAndReadingStandardInput"/> when <see cref="IInteractiveWindow.ReadStandardInput"/> is called
            /// </summary>
            Resetting,
            /// <summary>
            /// Prompt has been displayed - waiting for the user to make the next submission.
            /// Transition to <see cref="ExecutingInput"/> when <see cref="IInteractiveWindowOperations.ExecuteInput"/> is called.
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// Transition to <see cref="WaitingForInputAndReadingStandardInput"/> when <see cref="IInteractiveWindow.ReadStandardInput"/> is called
            /// </summary>
            WaitingForInput,
            /// <summary>
            /// Executing the user's submission.
            /// Transition to <see cref="WaitingForInput"/> when finished (in <see cref="UIThreadOnly.ProcessPendingSubmissions"/>).
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// Transition to <see cref="ExecutingInputAndReadingStandardInput"/> when <see cref="IInteractiveWindow.ReadStandardInput"/> is called
            /// </summary>
            ExecutingInput,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindow.ReadStandardInput"/> (within <see cref="IInteractiveWindowOperations.ResetAsync"/>).
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ClearView"/>,
            /// <see cref="IInteractiveWindowOperations.TrySubmitStandardInput"/>, or
            /// <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            ResettingAndReadingStandardInput,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindow.ReadStandardInput"/> (while prompt has been displayed).
            /// Transition to <see cref="WaitingForInput"/> when <see cref="IInteractiveWindowOperations.ClearView"/> or <see cref="IInteractiveWindowOperations.TrySubmitStandardInput"/> is called.
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            WaitingForInputAndReadingStandardInput,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindow.ReadStandardInput"/> (while executing the user's submission).
            /// Transition to <see cref="ExecutingInput"/> when <see cref="IInteractiveWindowOperations.ClearView"/> or <see cref="IInteractiveWindowOperations.TrySubmitStandardInput"/> is called.
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            ExecutingInputAndReadingStandardInput,
        }
    }
}
