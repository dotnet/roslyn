using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MoveDeclarationNearReference;

namespace Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InlineTemporary)]
    class CSharpMoveDeclarationNearReferenceCodeRefactoringProvider : AbstractMoveDeclarationNearReferenceCodeRefactoringProvider<LocalDeclarationStatementSyntax>
    {
    }
}
