﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.OrderModifiers;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    internal class CSharpOrderModifiersHelper : AbstractOrderModifiersHelpers
    {
        public static readonly CSharpOrderModifiersHelper Instance = new();

        private CSharpOrderModifiersHelper()
        {
        }

        protected override int GetKeywordKind(string trimmed)
        {
            var kind = SyntaxFacts.GetKeywordKind(trimmed);
            return (int)(kind == SyntaxKind.None ? SyntaxFacts.GetContextualKeywordKind(trimmed) : kind);
        }

        protected override bool TryParse(string value, out Dictionary<int, int> parsed)
        {
            if (!base.TryParse(value, out parsed))
            {
                return false;
            }

            // 'partial' must always go at the end in C#.
            parsed[(int)SyntaxKind.PartialKeyword] = int.MaxValue;
            return true;
        }
    }
}
