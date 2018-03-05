// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioRuleSetManager : IWorkspaceService, IDisposable
    {
        private readonly IVsFileChangeEx _fileChangeService;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _listener;

        private readonly Dictionary<string, RuleSetFile> _ruleSetFileMap = new Dictionary<string, RuleSetFile>(StringComparer.OrdinalIgnoreCase);

        public VisualStudioRuleSetManager(
            IVsFileChangeEx fileChangeService, IForegroundNotificationService foregroundNotificationService, IAsynchronousOperationListener listener)
        {
            _fileChangeService = fileChangeService;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listener;
        }

        public IRuleSetFile GetOrCreateRuleSet(string ruleSetFileFullPath)
        {
            if (!_ruleSetFileMap.TryGetValue(ruleSetFileFullPath, out var ruleSetFile))
            {
                ruleSetFile = new RuleSetFile(ruleSetFileFullPath, _fileChangeService, this);
                _ruleSetFileMap.Add(ruleSetFileFullPath, ruleSetFile);
            }

            return ruleSetFile;
        }

        private void StopTrackingRuleSetFile(RuleSetFile ruleSetFile)
        {
            _ruleSetFileMap.Remove(ruleSetFile.FilePath);
        }

        public void ClearCachedRuleSetFiles()
        {
            foreach (var pair in _ruleSetFileMap)
            {
                pair.Value.UnsubscribeFromFileTrackers();
            }

            _ruleSetFileMap.Clear();
        }

        void IDisposable.Dispose()
        {
            ClearCachedRuleSetFiles();
        }
    }
}
