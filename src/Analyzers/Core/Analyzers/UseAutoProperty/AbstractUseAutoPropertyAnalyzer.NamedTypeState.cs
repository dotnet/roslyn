// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseAutoProperty;

internal abstract partial class AbstractUseAutoPropertyAnalyzer<
    TSyntaxKind,
    TPropertyDeclaration,
    TConstructorDeclaration,
    TFieldDeclaration,
    TVariableDeclarator,
    TExpression,
    TIdentifierName>
{
    /// <summary>
    /// What we learn about the fields of a type while analyzing it and its nested types.  It is created by whichever of
    /// the type or its nested types starts being analyzed first, and disposed in the type's symbol end action.
    /// </summary>
    private sealed class NamedTypeState(ObjectPool<HashSet<string>> fieldNamesPool) : IDisposable
    {
        /// <summary>
        /// The names of the private fields of the type that could be converted.
        /// </summary>
        public readonly HashSet<string> FieldNames = fieldNamesPool.Allocate();

        /// <summary>
        /// Results of our analysis pass that we will use to determine which fields and properties to offer to fixup.
        /// </summary>
        public readonly ConcurrentStack<AnalysisResult> AnalysisResults = s_analysisResultPool.Allocate();

        /// <summary>
        /// Fields whose usage may disqualify them from being removed (depending on the usage location). For example,
        /// a field taken by ref normally can't be converted (as a property can't be taken by ref).  However, this
        /// doesn't apply within the property itself (as it can refer to `field` after the rewrite).
        /// </summary>
        public readonly ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> IneligibleFieldUsageIfOutsideProperty = s_fieldToUsageLocationPool.Allocate();

        /// <summary>
        /// Locations where this field is read or written.  If it is read or written outside of hte property being
        /// changed, and the property getter/setter is non-trivial, then we cannot use 'field' for it, as that would
        /// change the semantics in those locations.
        /// </summary>
        public readonly ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> FieldReads = s_fieldToUsageLocationPool.Allocate();

        /// <inheritdoc cref="FieldReads"/>
        public readonly ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> FieldWrites = s_fieldToUsageLocationPool.Allocate();

        public void Dispose()
        {
            fieldNamesPool.ClearAndFree(FieldNames);
            s_analysisResultPool.ClearAndFree(AnalysisResults);

            ClearAndFree(IneligibleFieldUsageIfOutsideProperty);
            ClearAndFree(FieldReads);
            ClearAndFree(FieldWrites);
        }
    }
}
