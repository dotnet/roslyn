// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    internal partial class ModifiersOrganizer
    {
        private readonly Dictionary<int, int> _preferredOrder;

        public ModifiersOrganizer(Dictionary<int, int> preferredOrder)
        {
            _preferredOrder = preferredOrder;
        }

        public static ModifiersOrganizer ForCodeStyle(OptionSet optionSet)
        {
            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder);
            if (!CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(CSharpCodeStyleOptions.PreferredModifierOrder.DefaultValue.Value, out preferredOrder);
            }

            return new ModifiersOrganizer(preferredOrder);
        }

        public SyntaxTokenList Organize(SyntaxTokenList modifiers)
        {
            if (modifiers.Count > 1 && !modifiers.SpansPreprocessorDirective())
            {
                var initialList = new List<SyntaxToken>(modifiers);
                var leadingTrivia = initialList.First().LeadingTrivia;
                initialList[0] = initialList[0].WithLeadingTrivia(SpecializedCollections.EmptyEnumerable<SyntaxTrivia>());

                var finalList = initialList.OrderBy(this).ToList();
                if (!initialList.SequenceEqual(finalList))
                {
                    finalList[0] = finalList[0].WithLeadingTrivia(leadingTrivia);

                    return finalList.ToSyntaxTokenList();
                }
            }

            return modifiers;
        }
    }
}
