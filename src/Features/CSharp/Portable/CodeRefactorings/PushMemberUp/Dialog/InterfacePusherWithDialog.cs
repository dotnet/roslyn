using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp.Dialog
{
    internal class InterfacePusherWithDialog
    {
        private INamedTypeSymbol TargetInterface { get; }

        internal InterfacePusherWithDialog(INamedTypeSymbol targetInterface)
        {
            TargetInterface = targetInterface;
        }

        internal async Task<Solution> ComputeChangedSolution(IEnumerable<ISymbol> members, SemanticModel semanticModel)
        {
            var syntaxGenerator = new InterfacePushUpSyntaxGenerator(semanticModel);
            var validator = new InterfaceModifiersValidator();
            var areModifiersValid = members.Select(member => validator.AreModifiersValid(TargetInterface, member));

            var membersDeclarations = (await Task.WhenAll(
                members.Select(member => member.DeclaringSyntaxReferences.First().GetSyntaxAsync()))).
                Zip(areModifiersValid,
                (memberDeclaration, needToFixModifier) =>
                {
                    if (needToFixModifier)
                    {
                        return syntaxGenerator.CreateMemberSyntaxWithFix(memberDeclaration as MemberDeclarationSyntax);
                    }
                    else
                    {
                        return syntaxGenerator.CreateMemberSyntax(memberDeclaration as MemberDeclarationSyntax);
                    }
                });
            return null;
        }
    }
}
