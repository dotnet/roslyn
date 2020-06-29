// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class Extensions
    {
        public const string ComplexReceiverTypeName = "";
        // Although they have same value, one constant here is used for the entire name
        // and the other is just a suffix. Defining separate constants for clarity.
        public const string ComplexArrayReceiverTypeName = "[]";
        public const string ArrayReceiverTypeNameSuffix = "[]";

        public static async Task<IEnumerable<SyntaxToken>> GetConstructorInitializerTokensAsync(this Document document, SemanticModel model, CancellationToken cancellationToken)
        {
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts == null)
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxToken>();
            }

            return FindReferenceCache.GetConstructorInitializerTokens(syntaxFacts, model, root, cancellationToken);
        }

        internal static bool TextMatch(this ISyntaxFactsService syntaxFacts, string text1, string text2)
            => syntaxFacts.StringComparer.Equals(text1, text2);
    }
}
