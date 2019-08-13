// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{

    internal static class CompletionItemExtensions
    {

        // A list of the different property names that the insertion text of a completion item may be stored under.
        private static readonly string[] InsertionTextPropertyNames = new string[] { "InsertionText", "InsertText" };

        internal static string GetInsertionText(this CompletionItem item)
        {
            foreach (var insertionTextPropertyName in InsertionTextPropertyNames)
            {
                if (item.Properties.ContainsKey(insertionTextPropertyName))
                {
                    return item.Properties[insertionTextPropertyName];
                }
            }
            return item.DisplayText;
        }
    }

}
