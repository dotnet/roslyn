// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class TreeRules
    {
        [TreeRule(Name = "RoundTrip", Group = "RoundTrip")]
        bool RoundTrip(SyntaxTree tree, string codeText, string filename, ref string errorText)
        {
            var retVal = true;
            if (tree.GetRoot().ToFullString() != codeText)
            {
                retVal = false;
                errorText = "FullText for tree parsed from '" + filename + "' does match actual text";
            }

            return retVal;
        }

        [TreeRule(Name = "TreeFullSpan", Group = "Span")]
        bool TreeFullSpan(SyntaxTree tree, string codeText, string filename, ref string errorText)
        {
            var retVal = true;
            if (tree.GetRoot().FullSpan.Length != codeText.Length)
            {
                retVal = false;
                errorText = "FullSpan width of tree (" + tree.GetRoot().FullSpan.Length + ") does not match length of the code (" + codeText.Length + ")";
            }

            return retVal;
        }

        [TreeRule(Name = "NullsAndCollections", Group = "RoundTrip")]
        bool NullsAndCollections(SyntaxTree tree, string codeText, string filename, ref string errorText)
        {
            var retVal = true;
            if (tree.GetDiagnostics() == null)
            {
                retVal = false;
                errorText = "Diagnostics collection for this tree is null";
            }
            else if ((
                from e in tree.GetDiagnostics()
                where e == null
                select e).Any())
            {
                retVal = false;
                errorText = "Diagnostics collection for this tree contains a null element";
            }
            else if (tree.GetRoot().DescendantTokens() == null)
            {
                retVal = false;
                errorText = "Tokens collection for this tree is null";
            }

            return retVal;
        }
    }
}