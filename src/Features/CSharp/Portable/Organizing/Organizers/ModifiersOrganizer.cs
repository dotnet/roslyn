// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    internal static partial class ModifiersOrganizer
    {
        public static SyntaxTokenList Organize(SyntaxTokenList modifiers)
        {
            if (modifiers.Count > 1 && !modifiers.SpansPreprocessorDirective())
            {
                var initialList = new List<SyntaxToken>(modifiers);
                var leadingTrivia = initialList.First().LeadingTrivia;
                initialList[0] = initialList[0].WithLeadingTrivia(SpecializedCollections.EmptyEnumerable<SyntaxTrivia>());

                var finalList = initialList.OrderBy(new Comparer()).ToList();
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
