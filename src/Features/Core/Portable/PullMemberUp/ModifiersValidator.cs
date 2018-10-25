// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal interface IValidator
    {
        List<string> WarningMessageList { get; }

        bool AreModifiersValid(INamedTypeSymbol typeSymbol, IEnumerable<ISymbol> selectedMembers);
    }

    internal class ClassModifiersValidator : IValidator
    {
        public List<string> WarningMessageList { get; }

        private bool GenerateWarningMessage { get; }

        internal ClassModifiersValidator(bool generateWarningMessage=false)
        {
            GenerateWarningMessage = generateWarningMessage;
            if (GenerateWarningMessage)
            {
                WarningMessageList = new List<string>();
            }
        }

        internal bool IsAbstractModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            if (selectedMember.IsAbstract && !targetSymbol.IsAbstract)
            {
                if (GenerateWarningMessage)
                {
                    WarningMessageList.Add($"{selectedMember} is abstract, {targetSymbol} will be changed to abstract.");
                }
                return false;
            }
            return true;
        }

        public bool AreModifiersValid(INamedTypeSymbol targetSymbol, IEnumerable<ISymbol> selectedMembers)
        {
            var result = true;
            foreach (var member in selectedMembers)
            {
                result = IsAbstractModifiersMatch(targetSymbol, member) && result;
            }
            return result;
        }
    }

    internal class InterfaceModifiersValidator : IValidator
    {
        public List<string> WarningMessageList { get; }

        private bool GenerateWarningMessage { get; }

        internal InterfaceModifiersValidator(bool generateWarningMessage=false)
        {
            GenerateWarningMessage = generateWarningMessage;
            if (GenerateWarningMessage)
            {
                WarningMessageList = new List<string>();
            }
        }

        internal bool IsAccessiblityModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            if (selectedMember.DeclaredAccessibility != Accessibility.Public)
            {
                if (GenerateWarningMessage)
                {
                    WarningMessageList.Add($"Destination {targetSymbol.Name} is an interface, {selectedMember.Name} will be changed to public.");
                }
                return false;
            }
            return true;
        }

        internal bool IsStaticModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            if (selectedMember.IsStatic)
            {
                if (GenerateWarningMessage)
                {
                    WarningMessageList.Add($"Destination {targetSymbol.Name} is an interface, {selectedMember.Name} will be changed to non-static.");
                }
                return false;
            }
            return true;
        }

        public bool AreModifiersValid(INamedTypeSymbol targetSymbol, IEnumerable<ISymbol> selectedMembers)
        {
            var result = true;
            foreach (var member in selectedMembers)
            {
                // This is to make sure the all the checks will be run
                // e.g. user will get two error warning messages if pull a
                // private static method to an interface
                result = IsAccessiblityModifiersMatch(targetSymbol, member) && result;
                result = IsStaticModifiersMatch(targetSymbol, member) && result;
            }
            return result;
        }
    }
}
