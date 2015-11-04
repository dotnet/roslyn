// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;
using Microsoft.CodeAnalysis.Editor.Implementation.Commands;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup
{
    internal sealed class EventHookupTestState : AbstractCommandHandlerTestState
    {
        private readonly EventHookupCommandHandler _commandHandler;
        private Mutex _testSessionHookupMutex;

        public EventHookupTestState(XElement workspaceElement) : base(workspaceElement, null, false)
        {
            CommandHandlerService t = (CommandHandlerService)Workspace.GetService<ICommandHandlerServiceFactory>().GetService(Workspace.Documents.Single().TextBuffer);
            var field = t.GetType().GetField("_commandHandlers", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            var handlers = (IEnumerable<Lazy<ICommandHandler, OrderableContentTypeMetadata>>)field.GetValue(t);
            _commandHandler = handlers.Single(h => h.Value is EventHookupCommandHandler).Value as EventHookupCommandHandler;

            _testSessionHookupMutex = new Mutex(false);
            _commandHandler.TESTSessionHookupMutex = _testSessionHookupMutex;
        }

        public static EventHookupTestState CreateTestState(string markup)
        {
            var workspaceXml = string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>{0}</Document>
    </Project>
</Workspace>", markup);

            return new EventHookupTestState(XElement.Parse(workspaceXml));
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
