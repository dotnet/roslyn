// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// This is used while computing the values of constant fields.  Since they can depend on each
/// other, we need to keep track of which ones we are currently computing in order to avoid (and
/// report) cycles.
/// </summary>
internal sealed class ConstantFieldsInProgress
{
    private readonly SourceFieldSymbol _fieldOpt;
    private readonly HashSet<SourceFieldSymbolWithSyntaxReference> _dependencies;

    /// <summary>
    /// Stores the last dependency that was added to the set. This is used to check if
    /// the dependency that was added is not after all a dependency after successful rebinding
    /// in Color Color resolution, i.e. `public const Color Color = Color.Red;`
    /// </summary>
    private SourceFieldSymbolWithSyntaxReference _lastDependency;

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
        _lastDependency = field;
    }

    internal void RemoveIfLastDependency(SourceFieldSymbolWithSyntaxReference field)
    {
        if (_lastDependency is not null && _lastDependency == (object)field)
        {
            _dependencies.Remove(_lastDependency);
            _lastDependency = null;
        }
    }
}
