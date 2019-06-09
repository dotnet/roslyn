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

        private readonly ReferenceCountedDisposableCache<string, RuleSetFile> _ruleSetFileMap = new ReferenceCountedDisposableCache<string, RuleSetFile>();

        public VisualStudioRuleSetManager(
            FileChangeWatcher fileChangeWatcher, IForegroundNotificationService foregroundNotificationService, IAsynchronousOperationListener listener)
        {
            _fileChangeWatcher = fileChangeWatcher;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listener;
        }

        public IReferenceCountedDisposable<ICacheEntry<string, IRuleSetFile>> GetOrCreateRuleSet(string ruleSetFileFullPath)
        {
            var cacheEntry = _ruleSetFileMap.GetOrCreate(ruleSetFileFullPath, _ => new RuleSetFile(ruleSetFileFullPath, this));

            // Call InitializeFileTracking outside the lock inside ReferenceCountedDisposableCache, so we don't have requests
            // for other files blocking behind the initialization of this one. RuleSetFile itself will ensure InitializeFileTracking is locked as appropriate.
            cacheEntry.Target.Value.InitializeFileTracking(_fileChangeWatcher);

            return cacheEntry;
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
            _ruleSetFileMap.Evict(ruleSetFile.FilePath);
        }
    }
}
