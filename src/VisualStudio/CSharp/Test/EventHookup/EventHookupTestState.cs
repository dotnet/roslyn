// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup
{
    internal sealed class EventHookupTestState : AbstractCommandHandlerTestState
    {
        // TODO: It seems that we can move EventHookupSessionManager to EditorFeatures (https://github.com/dotnet/roslyn/issues/46280)
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeaturesWpf.AddParts(
            typeof(EventHookupCommandHandler),
            typeof(EventHookupSessionManager));

        private readonly EventHookupCommandHandler _commandHandler;
        private readonly Mutex _testSessionHookupMutex;

        public EventHookupTestState(XElement workspaceElement, OptionsCollection options)
            : base(workspaceElement, s_composition)
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

        public static EventHookupTestState CreateTestState(string markup, OptionsCollection options = null)
            => new EventHookupTestState(GetWorkspaceXml(markup), options);

        public static XElement GetWorkspaceXml(string markup)
            => XElement.Parse(string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>{0}</Document>
    </Project>
</Workspace>", markup));

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
