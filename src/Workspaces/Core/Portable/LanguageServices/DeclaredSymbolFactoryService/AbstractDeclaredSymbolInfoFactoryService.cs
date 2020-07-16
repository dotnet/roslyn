// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractDeclaredSymbolInfoFactoryService : IDeclaredSymbolInfoFactoryService
    {
        private const string GenericTypeNameManglingString = "`";
        private static readonly string[] s_aritySuffixesOneToNine = { "`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9" };

        private static readonly ObjectPool<List<Dictionary<string, string>>> s_aliasMapListPool
            = SharedPools.Default<List<Dictionary<string, string>>>();

        // Note: these names are stored case insensitively.  That way the alias mapping works 
        // properly for VB.  It will mean that our inheritance maps may store more links in them
        // for C#.  However, that's ok.  It will be rare in practice, and all it means is that
        // we'll end up examining slightly more types (likely 0) when doing operations like 
        // Find all references.
        private static readonly ObjectPool<Dictionary<string, string>> s_aliasMapPool
            = SharedPools.StringIgnoreCaseDictionary<string>();

        protected static List<Dictionary<string, string>> AllocateAliasMapList()
            => s_aliasMapListPool.Allocate();

        // We do not differentiate arrays of different kinds for simplicity.
        // e.g. int[], int[][], int[,], etc. are all represented as int[] in the index.
        protected static string CreateReceiverTypeString(string typeName, bool isArray)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return isArray
                    ? FindSymbols.Extensions.ComplexArrayReceiverTypeName
                    : FindSymbols.Extensions.ComplexReceiverTypeName;
            }
            else
            {
                return isArray
                    ? typeName + FindSymbols.Extensions.ArrayReceiverTypeNameSuffix
                    : typeName;
            }
        }

        protected static string CreateValueTupleTypeString(int elementCount)
        {
            const string ValueTupleName = "ValueTuple";
            if (elementCount == 0)
            {
                return ValueTupleName;
            }
            // A ValueTuple can have up to 8 type parameters.
            return ValueTupleName + GetMetadataAritySuffix(elementCount > 8 ? 8 : elementCount);
        }

        protected static void FreeAliasMapList(List<Dictionary<string, string>> list)
        {
            if (list != null)
            {
                foreach (var aliasMap in list)
                {
                    FreeAliasMap(aliasMap);
                }

                s_aliasMapListPool.ClearAndFree(list);
            }
        }

        protected static void FreeAliasMap(Dictionary<string, string> aliasMap)
        {
            if (aliasMap != null)
            {
                s_aliasMapPool.ClearAndFree(aliasMap);
            }
        }

        protected static Dictionary<string, string> AllocateAliasMap()
            => s_aliasMapPool.Allocate();

        protected static void AppendTokens(SyntaxNode node, StringBuilder builder)
        {
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsToken)
                {
                    builder.Append(child.AsToken().Text);
                }
                else
                {
                    AppendTokens(child.AsNode(), builder);
                }
            }
        }

        protected static void Intern(StringTable stringTable, ArrayBuilder<string> builder)
        {
            for (int i = 0, n = builder.Count; i < n; i++)
            {
                builder[i] = stringTable.Add(builder[i]);
            }
        }

        public static string GetMetadataAritySuffix(int arity)
        {
            Debug.Assert(arity > 0);
            return (arity <= s_aritySuffixesOneToNine.Length)
                ? s_aritySuffixesOneToNine[arity - 1]
                : string.Concat(GenericTypeNameManglingString, arity.ToString(CultureInfo.InvariantCulture));
        }

        public abstract bool TryGetDeclaredSymbolInfo(StringTable stringTable, SyntaxNode node, string rootNamespace, out DeclaredSymbolInfo declaredSymbolInfo);

        /// <summary>
        /// Get the name of the target type of specified extension method declaration. 
        /// The node provided must be an extension method declaration,  i.e. calling `TryGetDeclaredSymbolInfo()` 
        /// on `node` should return a `DeclaredSymbolInfo` of kind `ExtensionMethod`. 
        /// If the return value is null, then it means this is a "complex" method (as described at <see cref="SyntaxTreeIndex.ExtensionMethodInfo"/>).
        /// </summary>
        public abstract string GetReceiverTypeName(SyntaxNode node);

        public abstract bool TryGetAliasesFromUsingDirective(SyntaxNode node, out ImmutableArray<(string aliasName, string name)> aliases);

        public abstract string GetRootNamespace(CompilationOptions compilationOptions);
    }
}
