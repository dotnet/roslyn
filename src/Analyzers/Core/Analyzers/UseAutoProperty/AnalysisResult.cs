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
