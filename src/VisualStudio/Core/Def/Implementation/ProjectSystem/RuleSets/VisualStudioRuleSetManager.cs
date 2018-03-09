// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioRuleSetManager : IWorkspaceService
    {
        private readonly FileChangeWatcher _fileChangeWatcher;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _listener;

        /// <summary>
        /// Gate that guards access to <see cref="_ruleSetFileMap"/>.
        /// </summary>
        private readonly object _gate = new object();

        /// <summary>
        /// The map of cached <see cref="RuleSetFile"/>. We use a WeakReference here because we want to strike this entry from the cache when all users are
        /// disposed, but that means we ourselves don't want to be contributing to that reference count. Since consumers of this are responsible for all
        /// disposing, it's safe for us to also remove entries from this map if we need to refresh -- once everybody who consumed it disposes everything,
        /// the underlying file watchers will be cleaned up.
        /// </summary>
        private readonly Dictionary<string, ReferenceCountedDisposable<RuleSetFile>.WeakReference> _ruleSetFileMap =
            new Dictionary<string, ReferenceCountedDisposable<RuleSetFile>.WeakReference>();

        public VisualStudioRuleSetManager(
            FileChangeWatcher fileChangeWatcher, IForegroundNotificationService foregroundNotificationService, IAsynchronousOperationListener listener)
        {
            _fileChangeWatcher = fileChangeWatcher;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listener;
        }

        public IReferenceCountedDisposable<IRuleSetFile> GetOrCreateRuleSet(string ruleSetFileFullPath)
        {
            ReferenceCountedDisposable<RuleSetFile> disposable = null;

            lock (_gate)
            {
                // If we already have one in the map to hand out, great
                if (_ruleSetFileMap.TryGetValue(ruleSetFileFullPath, out var weakReference))
                {
                    disposable = weakReference.TryAddReference();
                }

                if (disposable == null)
                {
                    // We didn't easily get a disposable, so one of two things is the case:
                    //
                    // 1. We have no entry in _ruleSetFileMap at all for this.
                    // 2. We had an entry, but it was disposed and is no longer valid.

                    // In either case, we'll create a new rule set file and add it to the map.
                    disposable = new ReferenceCountedDisposable<RuleSetFile>(new RuleSetFile(ruleSetFileFullPath, this));
                    _ruleSetFileMap[ruleSetFileFullPath] = new ReferenceCountedDisposable<RuleSetFile>.WeakReference(disposable);
                }
            }

            // Call InitializeFileTracking outside the lock, so we don't have requests for other files blocking behind the initialization of this one.
            // RuleSetFile itself will ensure InitializeFileTracking is locked as appropriate.
            disposable.Target.InitializeFileTracking(_fileChangeWatcher);

            return disposable;
        }

        private void StopTrackingRuleSetFile(RuleSetFile ruleSetFile)
        {
            // We can arrive here in one of two situations:
            // 
            // 1. The underlying RuleSetFile was disposed by all consumers, and we can try cleaning up our weak reference. This is purely an optimization
            //    to avoid the key/value pair being unnecessarily held.
            // 2. The RuleSetFile was modified, and we want to get rid of our cache now. Anybody still holding onto the values will dispose at their leaisure,
            //    but it won't really matter anyways since the Dispose() that cleans up file trackers is already done.
            //
            // In either case, we can just be lazy and remove the key/value pair. It's possible in the mean time that the rule set had already been removed
            // (perhaps by a file change), and we're removing a live instance. This is fine, as this doesn't affect correctness: we consider this to be a cache
            // and if two callers got different copies that's fine. We *could* fetch the item out of the dictionary, check if the item is strong and then compare,
            // but that seems overkill.
            lock (_gate)
            {
                _ruleSetFileMap.Remove(ruleSetFile.FilePath);
            }
        }
    }
}
