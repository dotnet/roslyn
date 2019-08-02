// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup
{
    internal sealed class EventHookupTestState : AbstractCommandHandlerTestState
    {
        private readonly EventHookupCommandHandler _commandHandler;
        private Mutex _testSessionHookupMutex;

        public EventHookupTestState(XElement workspaceElement, IDictionary<OptionKey, object> options)
            : base(workspaceElement, excludedTypes: null, GetExtraParts())
        {
            _commandHandler = new EventHookupCommandHandler(
                Workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                Workspace.GetService<IInlineRenameService>(),
                Workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>(),
                Workspace.ExportProvider.GetExportedValue<EventHookupSessionManager>());

            _testSessionHookupMutex = new Mutex(false);
            _commandHandler.TESTSessionHookupMutex = _testSessionHookupMutex;
            Workspace.ApplyOptions(options);
        }

        private static ComposableCatalog GetExtraParts()
        {
            return ExportProviderCache.CreateTypeCatalog(new[] { typeof(EventHookupCommandHandler), typeof(EventHookupSessionManager) });
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
            Assert.NotNull(_commandHandler.EventHookupSessionManager.TEST_MostRecentToolTipContent);
            Assert.Single(_commandHandler.EventHookupSessionManager.TEST_MostRecentToolTipContent);

            var textElement = _commandHandler.EventHookupSessionManager.TEST_MostRecentToolTipContent.First();
            Assert.Equal(3, textElement.Runs.Count());
            Assert.Equal(expectedText, textElement.Runs.First().Text);
        }

        internal void AssertNotShowing()
        {
            Assert.Null(_commandHandler.EventHookupSessionManager.TEST_MostRecentToolTipContent);
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
