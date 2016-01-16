// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.CallHierarchy;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;
using Microsoft.CodeAnalysis.Editor.Implementation.Notification;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy
{
    public class CallHierarchyTestState
    {
        private readonly CallHierarchyCommandHandler _commandHandler;
        private readonly MockCallHierarchyPresenter _presenter;
        internal TestWorkspace Workspace;
        private readonly ITextBuffer _subjectBuffer;
        private readonly IWpfTextView _textView;

        private class MockCallHierarchyPresenter : ICallHierarchyPresenter
        {
            public CallHierarchyItem PresentedRoot;

            public void PresentRoot(CallHierarchyItem root)
            {
                this.PresentedRoot = root;
            }
        }

        private class MockSearchCallback : ICallHierarchySearchCallback
        {
            private readonly Action<CallHierarchyItem> _verifyMemberItem;
            private readonly TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>();
            private readonly Action<ICallHierarchyNameItem> _verifyNameItem;

            public MockSearchCallback(Action<CallHierarchyItem> verify)
            {
                _verifyMemberItem = verify;
            }

            public MockSearchCallback(Action<ICallHierarchyNameItem> verify)
            {
                _verifyNameItem = verify;
            }

            public void AddResult(ICallHierarchyNameItem item)
            {
                _verifyNameItem(item);
            }

            public void AddResult(ICallHierarchyMemberItem item)
            {
                _verifyMemberItem((CallHierarchyItem)item);
            }

            public void InvalidateResults()
            {
            }

            public void ReportProgress(int current, int maximum)
            {
            }

            public void SearchFailed(string message)
            {
                _completionSource.SetException(new Exception(message));
            }

            public void SearchSucceeded()
            {
                _completionSource.SetResult(null);
            }

            internal void WaitForCompletion()
            {
                _completionSource.Task.Wait();
            }
        }

        public static async Task<CallHierarchyTestState> CreateAsync(XElement markup, params Type[] additionalTypes)
        {
            var exportProvider = CreateExportProvider(additionalTypes);
            var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(markup, exportProvider: exportProvider);

            return new CallHierarchyTestState(workspace);
        }

        private CallHierarchyTestState(TestWorkspace workspace)
        {
            this.Workspace = workspace;
            var testDocument = Workspace.Documents.Single(d => d.CursorPosition.HasValue);

            _textView = testDocument.GetTextView();
            _subjectBuffer = testDocument.GetTextBuffer();

            var provider = Workspace.GetService<CallHierarchyProvider>();

            var notificationService = Workspace.Services.GetService<INotificationService>() as INotificationServiceCallback;
            var callback = new Action<string, string, NotificationSeverity>((message, title, severity) => NotificationMessage = message);
            notificationService.NotificationCallback = callback;

            _presenter = new MockCallHierarchyPresenter();
            _commandHandler = new CallHierarchyCommandHandler(new[] { _presenter }, provider, TestWaitIndicator.Default);
        }

        private static VisualStudio.Composition.ExportProvider CreateExportProvider(Type[] additionalTypes)
        {
            var catalog = TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic
                .WithPart(typeof(CallHierarchyProvider))
                .WithPart(typeof(SymbolMappingServiceFactory))
                .WithPart(typeof(EditorNotificationServiceFactory))
                .WithParts(additionalTypes);

            return MinimalTestExportProvider.CreateExportProvider(catalog);
        }

        public static async Task<CallHierarchyTestState> CreateAsync(string markup, params Type[] additionalTypes)
        {
            var exportProvider = CreateExportProvider(additionalTypes);
            var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(markup, exportProvider: exportProvider);
            return new CallHierarchyTestState(markup, workspace);
        }

        private CallHierarchyTestState(string markup, TestWorkspace workspace)
        {
            this.Workspace = workspace;
            var testDocument = Workspace.Documents.Single(d => d.CursorPosition.HasValue);

            _textView = testDocument.GetTextView();
            _subjectBuffer = testDocument.GetTextBuffer();

            var provider = Workspace.GetService<CallHierarchyProvider>();

            var notificationService = Workspace.Services.GetService<INotificationService>() as INotificationServiceCallback;
            var callback = new Action<string, string, NotificationSeverity>((message, title, severity) => NotificationMessage = message);
            notificationService.NotificationCallback = callback;

            _presenter = new MockCallHierarchyPresenter();
            _commandHandler = new CallHierarchyCommandHandler(new[] { _presenter }, provider, TestWaitIndicator.Default);
        }

        internal string NotificationMessage
        {
            get;
            private set;
        }

        internal CallHierarchyItem GetRoot()
        {
            var args = new ViewCallHierarchyCommandArgs(_textView, _subjectBuffer);
            _commandHandler.ExecuteCommand(args, () => { });
            return _presenter.PresentedRoot;
        }

        internal IImmutableSet<Document> GetDocuments(string[] documentNames)
        {
            var selectedDocuments = new List<Document>();
            this.Workspace.CurrentSolution.Projects.Do(p => p.Documents.Where(d => documentNames.Contains(d.Name)).Do(d => selectedDocuments.Add(d)));
            return ImmutableHashSet.CreateRange<Document>(selectedDocuments);
        }

        internal void SearchRoot(CallHierarchyItem root, string displayName, Action<CallHierarchyItem> verify, CallHierarchySearchScope scope, IImmutableSet<Document> documents = null)
        {
            var callback = new MockSearchCallback(verify);
            var category = root.SupportedSearchCategories.First(c => c.DisplayName == displayName).Name;
            if (documents != null)
            {
                root.StartSearchWithDocuments(category, scope, callback, documents);
            }
            else
            {
                root.StartSearch(category, scope, callback);
            }

            callback.WaitForCompletion();
        }

        internal void SearchRoot(CallHierarchyItem root, string displayName, Action<ICallHierarchyNameItem> verify, CallHierarchySearchScope scope, IImmutableSet<Document> documents = null)
        {
            var callback = new MockSearchCallback(verify);
            var category = root.SupportedSearchCategories.First(c => c.DisplayName == displayName).Name;
            if (documents != null)
            {
                root.StartSearchWithDocuments(category, scope, callback, documents);
            }
            else
            {
                root.StartSearch(category, scope, callback);
            }

            callback.WaitForCompletion();
        }

        internal string ConvertToName(ICallHierarchyMemberItem root)
        {
            var name = root.MemberName;

            if (!string.IsNullOrEmpty(root.ContainingTypeName))
            {
                name = root.ContainingTypeName + "." + name;
            }

            if (!string.IsNullOrEmpty(root.ContainingNamespaceName))
            {
                name = root.ContainingNamespaceName + "." + name;
            }

            return name;
        }

        internal string ConvertToName(ICallHierarchyNameItem root)
        {
            return root.Name;
        }

        internal void VerifyRoot(CallHierarchyItem root, string name = "", string[] expectedCategories = null)
        {
            Assert.Equal(name, ConvertToName(root));

            if (expectedCategories != null)
            {
                var categories = root.SupportedSearchCategories.Select(s => s.DisplayName);
                foreach (var category in expectedCategories)
                {
                    Assert.Contains(category, categories);
                }
            }
        }

        internal void VerifyResultName(CallHierarchyItem root, string searchCategory, string[] expectedCallers, CallHierarchySearchScope scope = CallHierarchySearchScope.EntireSolution, IImmutableSet<Document> documents = null)
        {
            this.SearchRoot(root, searchCategory, (ICallHierarchyNameItem c) =>
                {
                    Assert.True(expectedCallers.Any());
                    Assert.True(expectedCallers.Contains(ConvertToName(c)));
                },
                scope,
                documents);
        }

        internal void VerifyResult(CallHierarchyItem root, string searchCategory, string[] expectedCallers, CallHierarchySearchScope scope = CallHierarchySearchScope.EntireSolution, IImmutableSet<Document> documents = null)
        {
            this.SearchRoot(root, searchCategory, (CallHierarchyItem c) =>
                {
                    Assert.True(expectedCallers.Any());
                    Assert.True(expectedCallers.Contains(ConvertToName(c)));
                },
                scope,
                documents);
        }

        internal void Navigate(CallHierarchyItem root, string searchCategory, string callSite, CallHierarchySearchScope scope = CallHierarchySearchScope.EntireSolution, IImmutableSet<Document> documents = null)
        {
            CallHierarchyItem item = null;
            this.SearchRoot(root, searchCategory, (CallHierarchyItem c) => item = c,
                scope,
                documents);

            if (callSite == ConvertToName(item))
            {
                var detail = item.Details.FirstOrDefault();
                if (detail != null)
                {
                    detail.NavigateTo();
                }
                else
                {
                    item.NavigateTo();
                }
            }
        }
    }
}
