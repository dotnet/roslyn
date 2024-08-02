// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;

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
    /// <param name="Property">The property we will make into an auto-property.</param>
    /// <param name="Field">The field we are removing.</param>
    /// <param name="PropertyDeclaration">The single declaration that <paramref name="Property"/> has.</param>
    /// <param name="FieldDeclaration">The single containing declaration that <paramref name="Field"/> has.</param>
    /// <param name="VariableDeclarator">The single containing declarator that <paramref name="Field"/> has.</param>
    /// <param name="Notification">The option value/severity at this particular analysis location.</param>
    /// <param name="IsTrivialGetAccessor">If the get-accessor is of a trivial form like <c>get => fieldName;</c>.  Such
    /// an accessor is a simple 'read through to the field' accessor.  As such, reads of the field can be replaced with
    /// calls to this accessor as it will have the same semantics.</param>
    /// <param name="IsTrivialSetAccessor">Same as <paramref name="IsTrivialSetAccessor"/>. Such an accessor is a simple
    /// 'write through to the field' accessor.  As such, writes of the field can be replaced with calls to this accessor
    /// as it will have the same semantics.</param>
    internal sealed record AnalysisResult(
        IPropertySymbol Property,
        IFieldSymbol Field,
        TPropertyDeclaration PropertyDeclaration,
        TFieldDeclaration FieldDeclaration,
        TVariableDeclarator VariableDeclarator,
        NotificationOption2 Notification,
        bool IsTrivialGetAccessor,
        bool IsTrivialSetAccessor);
}
