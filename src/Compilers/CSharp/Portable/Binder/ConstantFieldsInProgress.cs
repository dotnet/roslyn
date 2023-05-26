// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This is used while computing the values of constant fields.  Since they can depend on each
    /// other, we need to keep track of which ones we are currently computing in order to avoid (and
    /// report) cycles.
    /// </summary>
    internal sealed class ConstantFieldsInProgress
    {
        private readonly SourceFieldSymbol _fieldOpt;
        private readonly HashSet<SourceFieldSymbolWithSyntaxReference> _dependencies;

        private SourceFieldSymbolWithSyntaxReference? _recentDependency;

        internal static readonly ConstantFieldsInProgress Empty = new ConstantFieldsInProgress(null, null);

        internal ConstantFieldsInProgress(
            SourceFieldSymbol fieldOpt,
            HashSet<SourceFieldSymbolWithSyntaxReference> dependencies)
        {
            _fieldOpt = fieldOpt;
            _dependencies = dependencies;
        }

        public bool IsEmpty
        {
            get { return (object)_fieldOpt == null; }
        }

        internal void AddDependency(SourceFieldSymbolWithSyntaxReference field)
        {
            _dependencies.Add(field);
            _recentDependency = field;
        }

        internal void RemoveDepencency(SourceFieldSymbolWithSyntaxReference field)
        {
            _dependencies?.Remove(field);
        }

        private void RemoveRecentDependency()
        {
            if (_recentDependency is not null)
            {
                _dependencies.Remove(_recentDependency);
                _recentDependency = null;
            }
        }

        private bool EqualsRecentDependency(SourceFieldSymbolWithSyntaxReference field)
        {
            return _recentDependency == field;
        }

        internal void RemoveIfEqualsRecentDependency(SourceFieldSymbolWithSyntaxReference field)
        {
            if (EqualsRecentDependency(field))
            {
                RemoveRecentDependency();
            }
        }
    }
}
