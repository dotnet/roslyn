// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.MoveStaticMembers
{
    internal readonly struct MoveStaticMembersOptions
    {
        public bool IsCancelled { get; }

        public string FileName { get; }

        public string TypeName { get; }

        public string NamespaceDisplay { get; }

        public ImmutableArray<ISymbol> SelectedMembers { get; }

        public static MoveStaticMembersOptions Cancelled = new(
            string.Empty,
            string.Empty,
            ImmutableArray<ISymbol>.Empty,
            isCancelled: true);

        public MoveStaticMembersOptions(
            string fileName,
            string fullTypeName,
            ImmutableArray<ISymbol> selectedMembers,
            bool isCancelled = false)
        {
            IsCancelled = isCancelled;
            FileName = fileName;
            var namespacesAndType = fullTypeName.Split(separator: '.');
            TypeName = namespacesAndType.Last();
            NamespaceDisplay = string.Join(separator: ".", namespacesAndType.Take(namespacesAndType.Length - 1));
            SelectedMembers = selectedMembers;
        }
    }
}
