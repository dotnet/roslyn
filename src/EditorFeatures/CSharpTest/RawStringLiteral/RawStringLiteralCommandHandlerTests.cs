// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RawStringLiteral
{
    [UseExportProvider]
    public class RawStringLiteralCommandHandlerTests
    {
        internal sealed class RawStringLiteralTestState : AbstractCommandHandlerTestState
        {
            private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeaturesWpf.AddParts(
                typeof(RawStringLiteralCommandHandler));

            private readonly RawStringLiteralCommandHandler _commandHandler;

            public RawStringLiteralTestState(XElement workspaceElement)
                : base(workspaceElement, s_composition)
            {
                _commandHandler = (RawStringLiteralCommandHandler)GetExportedValues<ICommandHandler>().
                    Single(c => c is RawStringLiteralCommandHandler);
            }

            public static RawStringLiteralTestState CreateTestState(string markup)
                => new(GetWorkspaceXml(markup));

            public static XElement GetWorkspaceXml(string markup)
                => XElement.Parse(string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>{0}</Document>
    </Project>
</Workspace>", markup));

            internal void AssertCodeIs(string expectedCode)
            {
                MarkupTestFile.GetPosition(expectedCode, out var massaged, out int caretPosition);
                Assert.Equal(massaged, TextView.TextSnapshot.GetText());
                Assert.Equal(caretPosition, TextView.Caret.Position.BufferPosition.Position);
            }

            public void SendTypeChar(char ch)
                => SendTypeChar(ch, _commandHandler.ExecuteCommand, () => EditorOperations.InsertText(ch.ToString()));
        }

        [WpfFact]
        public void TestAtEndOfFile()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"$$");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = """"""$$""""""");
        }
    }
}
