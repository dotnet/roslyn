// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.FindReferences;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// An adapter between Roslyn's <see cref="IWaitContext"/> and editor's <see cref="IUIThreadOperationScope"/>, 
    /// which represent the same abstraction. The only place where it's needed so far is <see cref="FindReferencesCommandHandler"/>,
    /// that operates within <see cref="IUIThreadOperationContext"/>, but calls to the <see cref="IFindReferencesService"/>, which
    /// requires Roslyn's <see cref="IWaitContext"/>.
    /// Going forward this adapter can be deleted once Roslyn's <see cref="IWaitContext"/> is retired in favor of editor's 
    /// <see cref="IUIThreadOperationContext"/>.
    /// </summary>
    internal class WaitContextAdapter : IWaitContext
    {
        private readonly IUIThreadOperationScope _uiThreadOperationScope;

        public WaitContextAdapter(IUIThreadOperationScope uiThreadOperationScope)
        {
            Requires.NotNull(uiThreadOperationScope, nameof(uiThreadOperationScope));
            _uiThreadOperationScope = uiThreadOperationScope;
        }

        public CancellationToken CancellationToken => _uiThreadOperationScope.Context.UserCancellationToken;

        public bool AllowCancel
        {
            get => _uiThreadOperationScope.AllowCancellation;
            set => _uiThreadOperationScope.AllowCancellation = value;
        }

        public string Message
        {
            get => _uiThreadOperationScope.Description;
            set => _uiThreadOperationScope.Description = value;
        }

        public CodeAnalysis.Shared.Utilities.IProgressTracker ProgressTracker => new ProgressTrackerAdapter(_uiThreadOperationScope);
        
        public void Dispose()
        {
            _uiThreadOperationScope.Dispose();
        }
    }
}
