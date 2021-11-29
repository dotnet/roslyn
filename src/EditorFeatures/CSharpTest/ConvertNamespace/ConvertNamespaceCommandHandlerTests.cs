// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNamespace
{
    public class ConvertNamespaceCommandHandlerTests : AbstractCompleteStatementTests
    {
        private class ConvertNamespaceTestState : AbstractCommandHandlerTestState
        {
            public ConvertNamespaceTestState(
                string text,
                string? workspaceKind = null,
                bool makeSeparateBufferForCursor = false,
                ImmutableArray<string> roles = default)
                : base(XElement.Parse(text), EditorTestCompositions.Editor, workspaceKind, makeSeparateBufferForCursor, roles)
            {
            }
        }

        [Fact]
        public void TestConvert1()
        {
            using var testState = new ConvertNamespaceTestState(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace N$$
{
    class C
    {
    }
}
        </Document>
    </Project>
</Workspace>");

            testState.SendTypeChars(';');

        }
    }
}
