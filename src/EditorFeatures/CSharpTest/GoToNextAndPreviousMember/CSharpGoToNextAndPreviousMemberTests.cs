using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    public class CSharpGoToNextAndPreviousMemberTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        public void EmptyFile()
        {
            var code = @"$$";
            Assert.Null(GetTargetPosition(code, next: true));
        }

        private static int? GetTargetPosition(string code, bool next)
        {
            int? targetPosition;
            using (var workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                LanguageNames.CSharp,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                CSharpParseOptions.Default,
                code))
            {
                var hostDocument = workspace.DocumentWithCursor;

                targetPosition = GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                    workspace.CurrentSolution.GetDocument(hostDocument.Id),
                    hostDocument.CursorPosition.Value,
                    next,
                    CancellationToken.None);
            }

            return targetPosition;
        }
    }
}
