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
    /// <param name="PropertyDeclaration"></param>
    /// <param name="FieldDeclaration"></param>
    /// <param name="VariableDeclarator"></param>
    /// <param name="Notification"></param>
    /// <param name="IsTrivialGetAccessor">If the get-accessor is of a trivial form like <c>get =>
    /// fieldName;</c></param>
    /// <param name="IsTrivialSetAccessor">Same as <paramref name="IsTrivialSetAccessor"/>.</param>
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
