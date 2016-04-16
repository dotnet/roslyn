// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
