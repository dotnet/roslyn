// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioRuleSetManager
    {
        private sealed class RuleSetFile : IRuleSetFile
        {
            private readonly string _filePath;
            private readonly List<FileChangeTracker> _trackers;
            private readonly VisualStudioRuleSetManager _ruleSetManager;

            private ReportDiagnostic _generalDiagnosticOption;
            private ImmutableDictionary<string, ReportDiagnostic> _specificDiagnosticOptions;
            private bool _subscribed = false;
            private bool _optionsRead = false;

            private Exception _exception;

            public RuleSetFile(string filePath, IVsFileChangeEx fileChangeService, VisualStudioRuleSetManager ruleSetManager)
            {
                _filePath = filePath;
                _ruleSetManager = ruleSetManager;

                ImmutableArray<string> includes;

                try
                {
                    includes = RuleSet.GetEffectiveIncludesFromFile(filePath);
                }
                catch (Exception e)
                {
                    // We couldn't read the rule set for whatever reason. Capture the exception
                    // so we can surface the error later, and subscribe to file change notifications
                    // so that we'll automatically reload the file if the user can fix the issue.
                    _optionsRead = true;
                    _specificDiagnosticOptions = ImmutableDictionary<string, ReportDiagnostic>.Empty;
                    _exception = e;

                    includes = ImmutableArray.Create(filePath);
                }

                _trackers = new List<FileChangeTracker>(capacity: includes.Length);

                foreach (var include in includes)
                {
                    var tracker = new FileChangeTracker(fileChangeService, include);
                    tracker.UpdatedOnDisk += IncludeUpdated;
                    tracker.StartFileChangeListeningAsync();

                    _trackers.Add(tracker);
                }
            }

            public event EventHandler UpdatedOnDisk;

            public string FilePath
            {
                get { return _filePath; }
            }

            public Exception GetException()
            {
                EnsureSubscriptions();
                EnsureDiagnosticOptionsRead();

                return _exception;
            }

            public ReportDiagnostic GetGeneralDiagnosticOption()
            {
                EnsureSubscriptions();
                EnsureDiagnosticOptionsRead();

                return _generalDiagnosticOption;
            }

            public ImmutableDictionary<string, ReportDiagnostic> GetSpecificDiagnosticOptions()
            {
                EnsureSubscriptions();
                EnsureDiagnosticOptionsRead();

                return _specificDiagnosticOptions;
            }

            private void EnsureSubscriptions()
            {
                if (!_subscribed)
                {
                    foreach (var tracker in _trackers)
                    {
                        tracker.EnsureSubscription();
                    }

                    _subscribed = true;
                }
            }

            private void EnsureDiagnosticOptionsRead()
            {
                if (!_optionsRead)
                {
                    _optionsRead = true;
                    var specificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>();

                    try
                    {
                        var effectiveRuleset = RuleSet.LoadEffectiveRuleSetFromFile(_filePath);
                        _generalDiagnosticOption = effectiveRuleset.GeneralDiagnosticOption;
                        foreach (var rule in effectiveRuleset.SpecificDiagnosticOptions)
                        {
                            specificDiagnosticOptions.Add(rule.Key, rule.Value);
                        }

                        _specificDiagnosticOptions = specificDiagnosticOptions.ToImmutableDictionary();
                    }
                    catch (Exception e)
                    {
                        _exception = e;
                    }
                }
            }

            public void UnsubscribeFromFileTrackers()
            {
                foreach (var tracker in _trackers)
                {
                    tracker.UpdatedOnDisk -= IncludeUpdated;
                    tracker.Dispose();
                }

                _trackers.Clear();
            }

            private void IncludeUpdated(object sender, EventArgs e)
            {
                // The file change service is going to notify us of updates on the foreground thread.
                // This is going to cause us to drop our existing subscriptions and create new ones.
                // However, the FileChangeTracker signs up for subscriptions in a Task on a background thread.
                // We can easily end up with the foreground thread waiting on the Task, which is blocked
                // waiting for the foreground thread to release its lock on the file change service.
                // To avoid this, just queue up a Task to do the work on the foreground thread later, after
                // the lock on the file change service has been released.
                _ruleSetManager._foregroundNotificationService.RegisterNotification(
                    () => IncludeUpdateCore(), _ruleSetManager._listener.BeginAsyncOperation("IncludeUpdated"));
            }

            private void IncludeUpdateCore()
            {
                _ruleSetManager.StopTrackingRuleSetFile(this);
                UnsubscribeFromFileTrackers();
                UpdatedOnDisk?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
