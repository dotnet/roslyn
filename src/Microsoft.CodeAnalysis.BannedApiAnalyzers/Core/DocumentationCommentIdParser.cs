// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    internal static class DocumentationCommentIdParser
    {
        public static (string ParentName, string SymbolName)? ParseDeclaredSymbolId(string id)
        {
            if (id == null)
                return null;

            if (id.Length < 2)
                return null;

            int index = 0;
            return ParseDeclaredId(id, ref index);
        }

        private static (string ParentName, string SymbolName)? ParseDeclaredId(string id, ref int index)
        {
            var kindChar = PeekNextChar(id, index);

            switch (kindChar)
            {
                case 'E': // Events
                case 'F': // Fields
                case 'M': // Methods
                case 'P': // Properties
                case 'T': // Types
                    break;
                case 'N': // Namespaces
                default:
                    // Documentation comment id must start with E, F, M, N, P or T. Note: we don't support banning full
                    // namespaces, so we bail in that case as well.
                    return null;
            }

            index++;
            if (PeekNextChar(id, index) == ':')
                index++;

            string parentName = "";

            // process dotted names
            while (true)
            {
                var symbolName = ParseName(id, ref index);

                // has type parameters?
                if (PeekNextChar(id, index) == '`')
                {
                    index++;

                    // method type parameters?
                    if (PeekNextChar(id, index) == '`')
                        index++;

                    ReadNextInteger(id, ref index);
                }

                if (PeekNextChar(id, index) == '.')
                {
                    index++;
                    parentName = symbolName;
                    continue;
                }
                else
                {
                    return (parentName, symbolName);
                }
            }
        }

        private static char PeekNextChar(string id, int index)
        {
            return index >= id.Length ? '\0' : id[index];
        }

        private static readonly char[] s_nameDelimiters = { ':', '.', '(', ')', '{', '}', '[', ']', ',', '\'', '@', '*', '`', '~' };

        private static string ParseName(string id, ref int index)
        {
            string name;

            int delimiterOffset = id.IndexOfAny(s_nameDelimiters, index);
            if (delimiterOffset >= 0)
            {
                name = id[index..delimiterOffset];
                index = delimiterOffset;
            }
            else
            {
                name = id[index..];
                index = id.Length;
            }

            return DecodeName(name);
        }

        // undoes dot encodings within names...
        private static string DecodeName(string name)
            => name.IndexOf('#') >= 0 ? name.Replace('#', '.') : name;

        private static int ReadNextInteger(string id, ref int index)
        {
            int n = 0;

            while (index < id.Length && char.IsDigit(id[index]))
            {
                n = n * 10 + (id[index] - '0');
                index++;
            }

            return n;
        }
    }
}
