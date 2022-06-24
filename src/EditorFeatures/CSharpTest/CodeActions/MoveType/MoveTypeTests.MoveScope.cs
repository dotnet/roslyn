// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_SingleItem()
        {
            var code =
@"namespace N1
{
    class [||]Class1
    {
    }
}";

            var expected =
@"namespace N1
{
    class Class1
    {
    }
}";

            return TestNamespaceMove(code, expected, expectOperation: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_SingleItemNamespaceComment()
        {
            var code =
@"// Comment on the namespace
namespace N1
{
    class [||]Class1
    {
    }
}";

            var expected =
@"// Comment on the namespace
namespace N1
{
    class Class1
    {
    }
}";

            return TestNamespaceMove(code, expected, expectOperation: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtTop()
        {
            var code =
@"namespace N1
{
    class [||]Class1
    {
    }

    class Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    class Class1
    {
    }
}

namespace N1
{
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtTopNamespaceComment()
        {
            var code =
@"// Comment on the namespace
namespace N1
{
    class [||]Class1
    {
    }

    class Class2
    {
    }
}";

            var expected =
@"// Comment on the namespace
namespace N1
{
    class Class1
    {
    }
}

namespace N1
{
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtTopWithComments()
        {
            var code =
@"namespace N1
{
    // Class1 Comment
    class [||]Class1
    {
    }

    // Class2 Comment
    class Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    // Class1 Comment
    class Class1
    {
    }
}

namespace N1
{
    // Class2 Comment
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtTopWithXmlComments()
        {
            var code =
@"namespace N1
{
    /// <summary>
    /// Class1 summary
    /// </summary>
    class [||]Class1
    {
    }

    /// <summary>
    /// Class2 summary
    /// </summary>
    class Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    /// <summary>
    /// Class1 summary
    /// </summary>
    class Class1
    {
    }
}

namespace N1
{
    /// <summary>
    /// Class2 summary
    /// </summary>
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtBottom()
        {
            var code =
@"namespace N1
{
    class Class1
    {
    }

    class [||]Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    class Class1
    {
    }
}

namespace N1
{
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtBottomNamespaceComments()
        {
            var code =
@"// Comment on the namespace
namespace N1
{
    class Class1
    {
    }

    class [||]Class2
    {
    }
}";

            var expected =
@"// Comment on the namespace
namespace N1
{
    class Class1
    {
    }
}

namespace N1
{
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtBottomWithComments()
        {
            var code =
@"namespace N1
{
    // Class1 comment
    class Class1
    {
    }

    // Class2 comment
    class [||]Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    // Class1 comment
    class Class1
    {
    }
}

namespace N1
{
    // Class2 comment
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemAtBottomWithXmlComments()
        {
            var code =
@"namespace N1
{
    /// <summary>
    /// Class1 summary
    /// </summary>
    class Class1
    {
    }

    /// <summary>
    /// Class2 summary
    /// </summary>
    class [||]Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    /// <summary>
    /// Class1 summary
    /// </summary>
    class Class1
    {
    }
}

namespace N1
{
    /// <summary>
    /// Class2 summary
    /// </summary>
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemInMiddle()
        {
            var code =
@"namespace N1
{
    class Class1
    {
    }

    class Class2
    {
    }

    class [||]Class3
    {
    }

    class Class4
    {
    }

    class Class5
    {
    }
}";

            var expected =
@"namespace N1
{
    class Class1
    {
    }

    class Class2
    {
    }
}

namespace N1
{
    class Class3
    {
    }
}

namespace N1
{
    class Class4
    {
    }

    class Class5
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemInMiddleNamespaceComment()
        {
            var code =
@"// Comment on the namespace
namespace N1
{
    class Class1
    {
    }

    class Class2
    {
    }

    class [||]Class3
    {
    }

    class Class4
    {
    }

    class Class5
    {
    }
}";

            var expected =
@"// Comment on the namespace
namespace N1
{
    class Class1
    {
    }

    class Class2
    {
    }
}

namespace N1
{
    class Class3
    {
    }
}

namespace N1
{
    class Class4
    {
    }

    class Class5
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemInMiddleWithComments()
        {
            var code =
@"namespace N1
{
    // Class1 comment
    class Class1
    {
    }

    // Class2 comment
    class Class2
    {
    }

    // Class3 comment
    class [||]Class3
    {
    }

    // Class4 comment
    class Class4
    {
    }

    // Class5 comment
    class Class5
    {
    }
}";

            var expected =
@"namespace N1
{
    // Class1 comment
    class Class1
    {
    }

    // Class2 comment
    class Class2
    {
    }
}

namespace N1
{
    // Class3 comment
    class Class3
    {
    }
}

namespace N1
{
    // Class4 comment
    class Class4
    {
    }

    // Class5 comment
    class Class5
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemInMiddleWithXmlComments()
        {
            var code =
@"namespace N1
{
    /// <summary>
    /// Class1 summary
    /// </summary>
    class Class1
    {
    }

    /// <summary>
    /// Class2 summary
    /// </summary>
    class Class2
    {
    }

    /// <summary>
    /// Class3 summary
    /// </summary>
    class [||]Class3
    {
    }

    /// <summary>
    /// Class4 summary
    /// </summary>
    class Class4
    {
    }

    /// <summary>
    /// Class5 summary
    /// </summary>
    class Class5
    {
    }
}";

            var expected =
@"namespace N1
{
    /// <summary>
    /// Class1 summary
    /// </summary>
    class Class1
    {
    }

    /// <summary>
    /// Class2 summary
    /// </summary>
    class Class2
    {
    }
}

namespace N1
{
    /// <summary>
    /// Class3 summary
    /// </summary>
    class Class3
    {
    }
}

namespace N1
{
    /// <summary>
    /// Class4 summary
    /// </summary>
    class Class4
    {
    }

    /// <summary>
    /// Class5 summary
    /// </summary>
    class Class5
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemInMiddleWithInterface()
        {
            var code =
@"namespace N1
{
    // Class1 comment
    class Class1
    {
    }

    // IClass3 comment
    interface IClass3
    {
        void DoStuff();
    }

    // Class3 comment
    class [||]Class3 : IClass3
    {
        public void DoStuff() { }
    }

    // Class4 comment
    class Class4
    {
    }

    // Class5 comment
    class Class5
    {
    }
}";

            var expected =
@"namespace N1
{
    // Class1 comment
    class Class1
    {
    }

    // IClass3 comment
    interface IClass3
    {
        void DoStuff();
    }
}

namespace N1
{
    // Class3 comment
    class Class3 : IClass3
    {
        public void DoStuff() { }
    }
}

namespace N1
{
    // Class4 comment
    class Class4
    {
    }

    // Class5 comment
    class Class5
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_TwoItemsInDifferentNamespace()
        {
            var code =
@"namespace N1
{
    class [||]Class1
    {
    }
}

namespace N2
{
    class Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    class Class1
    {
    }
}

namespace N2
{
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected, expectOperation: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_ItemsInDifferentNamespace()
        {
            var code =
@"namespace N1
{
    interface IClass1
    {
    }

    class [||]Class1 : IClass1
    {
    }
}

namespace N2
{
    class Class2
    {
    }
}";

            var expected =
@"namespace N1
{
    interface IClass1
    {
    }
}

namespace N1
{
    class Class1 : IClass1
    {
    }
}

namespace N2
{
    class Class2
    {
    }
}";

            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_NestedNamespaces()
        {
            var code =
@"namespace N1
{
    namespace N2
    {
        class [||]C1
        {
        }

        class C2
        {
        }
    }

    class C3
    {
    }
}";
            var expected =
@"namespace N1
{
    namespace N1.N2
    {
        class C1
        {
        }
    }

    namespace N2
    {
        class C2
        {
        }
    }

    class C3
    {
    }
}";
            return TestNamespaceMove(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public Task MoveType_NamespaceScope_NestedNamespaces2()
        {
            var code =
@"namespace N1
{
    namespace N2
    {
        class C1
        {
        }

        class C2
        {
        }
    }

    class [||]C3
    {
    }

    namespace N3
    {
        class C4
        {
        }
    }
}";
            var expected =
@"namespace N1
{
    namespace N2
    {
        class C1
        {
        }

        class C2
        {
        }
    }
}

namespace N1
{
    class C3
    {
    }
}

namespace N1
{
    namespace N3
    {
        class C4
        {
        }
    }
}";
            return TestNamespaceMove(code, expected);
        }

        private async Task TestNamespaceMove(string originalCode, string expectedCode, bool expectOperation = true)
        {
            using var workspace = CreateWorkspaceFromOptions(originalCode);
            var documentToModifyId = workspace.Documents[0].Id;
            var textSpan = workspace.Documents[0].SelectedSpans[0];
            var documentToModify = workspace.CurrentSolution.GetDocument(documentToModifyId);

            var moveTypeService = documentToModify.GetLanguageService<IMoveTypeService>();
            Assert.NotNull(moveTypeService);

            var modifiedSolution = await moveTypeService.GetModifiedSolutionAsync(documentToModify, textSpan, MoveTypeOperationKind.MoveTypeNamespaceScope, CodeActionOptions.DefaultProvider, CancellationToken.None).ConfigureAwait(false);

            if (expectOperation)
            {
                Assert.NotEqual(documentToModify.Project.Solution, modifiedSolution);
            }
            else
            {
                Assert.Equal(documentToModify.Project.Solution, modifiedSolution);
            }

            var modifiedDocument = modifiedSolution.GetDocument(documentToModifyId);
            var formattedDocument = await Formatter.FormatAsync(modifiedDocument, CSharpSyntaxFormattingOptions.Default, CancellationToken.None).ConfigureAwait(false);

            var formattedText = await formattedDocument.GetTextAsync().ConfigureAwait(false);
            Assert.Equal(expectedCode, formattedText.ToString());
        }
    }
}
