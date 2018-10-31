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
        private sealed class RuleSetFile : IRuleSetFile, IDisposable
        {
            private readonly VisualStudioRuleSetManager _ruleSetManager;
            private readonly object _gate = new object();

            private FileChangeWatcher.IContext _fileChangeContext;

            private ReportDiagnostic _generalDiagnosticOption;
            private ImmutableDictionary<string, ReportDiagnostic> _specificDiagnosticOptions;
            private bool _subscribed = false;
            private bool _optionsRead = false;
            private bool _removedFromRuleSetManager = false;

            private Exception _exception;

            public RuleSetFile(string filePath, VisualStudioRuleSetManager ruleSetManager)
            {
                FilePath = filePath;
                _ruleSetManager = ruleSetManager;
            }

            public void InitializeFileTracking(FileChangeWatcher fileChangeWatcher)
            {
                lock (_gate)
                {
                    if (_fileChangeContext == null)
                    {
                        ImmutableArray<string> includes;

                        try
                        {
                            includes = RuleSet.GetEffectiveIncludesFromFile(FilePath);
                        }
                        catch (Exception e)
                        {
                            // We couldn't read the rule set for whatever reason. Capture the exception
                            // so we can surface the error later, and subscribe to file change notifications
                            // so that we'll automatically reload the file if the user can fix the issue.
                            _optionsRead = true;
                            _specificDiagnosticOptions = ImmutableDictionary<string, ReportDiagnostic>.Empty;
                            _exception = e;

                            includes = ImmutableArray.Create(FilePath);
                        }

                        _fileChangeContext = fileChangeWatcher.CreateContext();
                        _fileChangeContext.FileChanged += IncludeUpdated;

                        foreach (var include in includes)
                        {
                            _fileChangeContext.EnqueueWatchingFile(include);
                        }
                    }
                }
            }

            public event EventHandler UpdatedOnDisk;

            public string FilePath { get; }

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
                lock (_gate)
                {
                    if (!_subscribed)
                    {
                        // TODO: ensure subscriptions now
                        _subscribed = true;
                    }
                }
            }

            private void EnsureDiagnosticOptionsRead()
            {
                lock (_gate)
                {
                    if (!_optionsRead)
                    {
                        _optionsRead = true;
                        var specificDiagnosticOptions = new Dictionary<string, ReportDiagnostic>();

                        try
                        {
                            var effectiveRuleset = RuleSet.LoadEffectiveRuleSetFromFile(FilePath);
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
            }

            public void Dispose()
            {
                RemoveFromRuleSetManagerAndDisconnectFileTrackers();
            }

            private void RemoveFromRuleSetManagerAndDisconnectFileTrackers()
            {
                lock (_gate)
                {
                    _fileChangeContext.Dispose();

                    if (_removedFromRuleSetManager)
                    {
                        return;
                    }

                    _removedFromRuleSetManager = true;
                }

                // Call outside of lock to avoid general surprises; we skip this with the return above inside the lock.
                _ruleSetManager.StopTrackingRuleSetFile(this);
            }

            private void IncludeUpdated(object sender, string fileChanged)
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
                // It's critical that RemoveFromRuleSetManagerAndDisconnectFileTrackers() is called first prior to raising the event
                // -- this way any callers who call the RuleSetManager asking for the new file are guaranteed to get the new snapshot first.
                // idempotent.
                RemoveFromRuleSetManagerAndDisconnectFileTrackers();
                UpdatedOnDisk?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
