// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task ClassesWithNoContainerNamespace()
        {
            var code = 
@"[||]class Class1 { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task ClassesWithContainerNamespace()
        {
            var code =
@"namespace N1
{
    [||]class Class1 { }
        class Class2 { }
}";

            var codeAfterMove =
@"namespace N1
{
        class Class2 { }
}";

            var expectedDocumentName = "Class1.cs";

            var destinationDocumentText =
@"namespace N1
{
    class Class1 { }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }
    }
}