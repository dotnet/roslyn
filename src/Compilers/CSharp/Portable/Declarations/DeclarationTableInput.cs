// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class DeclarationTableInput
    {
        public static readonly DeclarationTableInput Empty = new(DeclarationTable.Empty, ImmutableList<(bool isAdd, Lazy<RootSingleNamespaceDeclaration> lazyRoot)>.Empty);

        private readonly DeclarationTable _initialTable;
        private readonly ImmutableList<(bool isAdd, Lazy<RootSingleNamespaceDeclaration> lazyRoot)> _instructions;
        private DeclarationTable? _finalTable;

        private DeclarationTableInput(DeclarationTable initialTable, ImmutableList<(bool isAdd, Lazy<RootSingleNamespaceDeclaration> lazyRoot)> instructions)
        {
            _initialTable = initialTable;
            _instructions = instructions;
        }

        internal DeclarationTableInput AddRootDeclaration(Lazy<RootSingleNamespaceDeclaration> lazyRoot)
        {
            var initialTable = _finalTable;
            var baseInstructions = ImmutableList<(bool isAdd, Lazy<RootSingleNamespaceDeclaration> lazyRoot)>.Empty;
            if (initialTable is null)
            {
                initialTable = _initialTable;
                baseInstructions = _instructions;
            }

            var instructions = baseInstructions.Add((isAdd: true, lazyRoot));
            return new DeclarationTableInput(initialTable, instructions);
        }

        internal DeclarationTableInput RemoveRootDeclaration(Lazy<RootSingleNamespaceDeclaration> lazyRoot)
        {
            var initialTable = _finalTable;
            var baseInstructions = ImmutableList<(bool isAdd, Lazy<RootSingleNamespaceDeclaration> lazyRoot)>.Empty;
            if (initialTable is null)
            {
                initialTable = _initialTable;
                baseInstructions = _instructions;
            }

            var instructions = baseInstructions.Add((isAdd: false, lazyRoot));
            return new DeclarationTableInput(initialTable, instructions);
        }

        internal DeclarationTableInput Reduce()
        {
            if (this is { _instructions.IsEmpty: false, _finalTable: { } finalTable })
            {
                return new DeclarationTableInput(finalTable, ImmutableList<(bool isAdd, Lazy<RootSingleNamespaceDeclaration> lazyRoot)>.Empty);
            }

            return this;
        }

        internal DeclarationTable GetDeclarationTable()
        {
            if (_finalTable is null)
            {
                var table = _initialTable;
                foreach (var (isAdd, lazyRoot) in _instructions)
                {
                    if (isAdd)
                        table = table.AddRootDeclaration(lazyRoot);
                    else
                        table = table.RemoveRootDeclaration(lazyRoot);
                }

                Interlocked.CompareExchange(ref _finalTable, table, null);
            }

            return _finalTable;
        }
    }
}
