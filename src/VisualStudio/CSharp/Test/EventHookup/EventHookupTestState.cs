// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup
{
    internal sealed class EventHookupTestState : AbstractCommandHandlerTestState
    {
        private readonly EventHookupCommandHandler _commandHandler;
        private Mutex _testSessionHookupMutex;

        public EventHookupTestState(XElement workspaceElement, IDictionary<OptionKey, object> options)
            : base(workspaceElement, GetExtraParts(), false)
        {
#pragma warning disable CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
            _commandHandler = new EventHookupCommandHandler(Workspace.GetService<IInlineRenameService>(), Workspace.GetService<IQuickInfoBroker>(),
                null, Workspace.ExportProvider.GetExportedValues<IAsynchronousOperationListener>().Select(l => new Lazy<IAsynchronousOperationListener, FeatureMetadata>(() => l, new FeatureMetadata("EventHookup"))));
#pragma warning restore CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094

            _testSessionHookupMutex = new Mutex(false);
            _commandHandler.TESTSessionHookupMutex = _testSessionHookupMutex;
            Workspace.ApplyOptions(options);
        }

        private static ComposableCatalog GetExtraParts()
        {
            return MinimalTestExportProvider.CreateTypeCatalog(new[] { typeof(EventHookupWaiter), typeof(EventHookupCommandHandler), typeof(EventHookupQuickInfoSourceProvider) });
        }

        public static EventHookupTestState CreateTestState(string markup, IDictionary<OptionKey, object> options = null)
        {
            var workspaceXml = string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>{0}</Document>
    </Project>
</Workspace>", markup);

            return new EventHookupTestState(XElement.Parse(workspaceXml), options);
        }

        internal void AssertShowing(string expectedText)
        {
            Assert.NotNull(_commandHandler.EventHookupSessionManager.QuickInfoSession);
            Assert.NotNull(_commandHandler.EventHookupSessionManager.TEST_MostRecentQuickInfoContent);

            var inlines = (_commandHandler.EventHookupSessionManager.TEST_MostRecentQuickInfoContent as System.Windows.Controls.TextBlock).Inlines;
            Assert.Equal(2, inlines.Count);
            Assert.Equal(expectedText, (inlines.First() as System.Windows.Documents.Run).Text);
        }

        internal void AssertNotShowing()
        {
            Assert.True(_commandHandler.EventHookupSessionManager.QuickInfoSession == null || _commandHandler.EventHookupSessionManager.QuickInfoSession.IsDismissed);
            Assert.Null(_commandHandler.EventHookupSessionManager.TEST_MostRecentQuickInfoContent);
        }

        internal void SetEventHookupCheckMutex()
        {
            _testSessionHookupMutex.WaitOne();
        }

        internal void ReleaseEventHookupCheckMutex()
        {
            _testSessionHookupMutex.ReleaseMutex();
        }

        internal void AssertCodeIs(string expectedCode)
        {
            Assert.Equal(expectedCode, TextView.TextSnapshot.GetText());
        }

        public void SendTypeChar(char ch)
        {
            SendTypeChar(ch, _commandHandler.ExecuteCommand, () => EditorOperations.InsertText(ch.ToString()));
        }

        internal void SendTab()
        {
            base.SendTab(_commandHandler.ExecuteCommand, () => EditorOperations.InsertText("    "));
        }
    }
}
