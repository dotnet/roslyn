
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal interface IValidator
    {
        List<string> WarningMessageList { get; }

        bool AreModifiersValid(INamedTypeSymbol typeSymbol, IEnumerable<ISymbol> selectedMembers);
    }

    internal class ClassModifiersValidator : IValidator
    {
        public List<string> WarningMessageList { get; }

        internal ClassModifiersValidator()
        {
            WarningMessageList = new List<string>();
        }

        internal bool IsStaticModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            if (targetSymbol.IsStatic && !selectedMember.IsStatic)
            {
                WarningMessageList.Add($"{targetSymbol.Name} is static class, {selectedMember.Name} will be changed to static.");
                return false;
            }
            return true;
        }
      
        internal bool IsAbstractModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            if (selectedMember.IsAbstract && !targetSymbol.IsAbstract)
            {
                WarningMessageList.Add($"{selectedMember} is abstract, {targetSymbol} will be changed to abstract.");
                return false;
            }
            return true;
        }

        public bool AreModifiersValid(INamedTypeSymbol targetSymbol, IEnumerable<ISymbol> selectedMembers)
        {
            var result = true;
            foreach (var member in selectedMembers)
            {
                result = result &&
                         IsStaticModifiersMatch(targetSymbol, member) &&
                         IsAbstractModifiersMatch(targetSymbol, member);
            }
            return result;
        }
    }

    internal class InterfaceModifiersValidator : IValidator
    {
        public List<string> WarningMessageList { get; }

        internal InterfaceModifiersValidator()
        {
            WarningMessageList = new List<string>();
        }

        internal bool IsAccessiblityModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            if (selectedMember.DeclaredAccessibility != Accessibility.Public)
            {
                WarningMessageList.Add($"Destination {targetSymbol.Name} is an interface, {selectedMember.Name} will be changed to public.");
                return false;
            }
            return true;
        }

        internal bool IsStaticModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            if (selectedMember.IsStatic)
            {
                WarningMessageList.Add($"Destination {targetSymbol.Name} is an interface, {selectedMember.Name} will be changed to non-static.");
                return false;
            }
            return true;
        }

        public bool AreModifiersValid(INamedTypeSymbol targetSymbol, IEnumerable<ISymbol> selectedMembers)
        {
            var result = true;
            foreach (var member in selectedMembers)
            {
                result = result &&
                         IsStaticModifiersMatch(targetSymbol, member) &&
                         IsAccessiblityModifiersMatch(targetSymbol, member);
            }
            return result;
        }
    }
}
