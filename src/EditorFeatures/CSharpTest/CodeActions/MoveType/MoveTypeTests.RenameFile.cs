// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task SingleClassInFileWithNoContainerNamespace()
        {
            var code =
@"[||]class Class1";

            var expectedDocumentName = "Class1.cs";

            await TestRenameFileToMatchTypeAsync(code, expectedDocumentName);
        }
    }
}
