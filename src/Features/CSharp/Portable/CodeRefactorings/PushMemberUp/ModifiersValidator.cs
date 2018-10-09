
namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class ClassModifiersValidator
    {
        internal bool IsStaticModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return !targetSymbol.IsStatic || selectedMember.IsStatic;
        }
      
        internal bool IsAbstractModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return targetSymbol.IsAbstract || !selectedMember.IsAbstract;
        }

        internal bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return IsStaticModifiersMatch(targetSymbol, selectedMember) &&
                   IsAbstractModifiersMatch(targetSymbol, selectedMember);
        }
    }

    internal class InterfaceModifiersValidator
    {
        internal bool IsAccessiblityModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return selectedMember.DeclaredAccessibility == Accessibility.Public;
        }

        internal bool IsStaticModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return !selectedMember.IsStatic;
        }

        internal bool IsAbstractModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return !selectedMember.IsAbstract;
        }

        internal bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return IsStaticModifiersMatch(targetSymbol, selectedMember) &&
                   IsAbstractModifiersMatch(targetSymbol, selectedMember) &&
                   IsAbstractModifiersMatch(targetSymbol, selectedMember);
        }
    }
}
