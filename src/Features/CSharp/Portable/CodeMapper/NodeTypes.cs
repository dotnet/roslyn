// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

internal static class NodeTypes
{
    /// <summary>
    /// A list of short circuit types.
    /// These are excluded types that we know we don't want to compare against
    /// all other scoped or simple types.
    /// </summary>
    public static HashSet<Type> Exclude = new HashSet<Type>
    {
        typeof(CompilationUnitSyntax),
    };

    /// <summary>
    /// The list of supported Scoped nodes.
    /// </summary>
    public static IReadOnlyDictionary<Scope, Type[]> Scoped = new Dictionary<Scope, Type[]>
    {
        [Scope.Class] = new[]
        {
            typeof(ClassDeclarationSyntax),
            typeof(InterfaceDeclarationSyntax),
            typeof(EnumDeclarationSyntax),
            typeof(StructDeclarationSyntax),
            typeof(RecordDeclarationSyntax),
        },
        [Scope.Method] = new[]
        {
            typeof(MethodDeclarationSyntax),
            typeof(LocalFunctionStatementSyntax),
            typeof(ConstructorDeclarationSyntax),
        },
        [Scope.Statement] = new[]
        {
            typeof(WhileStatementSyntax),
            typeof(IfStatementSyntax),
            typeof(SwitchStatementSyntax),
            typeof(DoStatementSyntax),
            typeof(ForEachStatementSyntax),
            typeof(ForStatementSyntax),
        },
    };

    /// <summary>
    /// The simple node types.
    /// </summary>
    public static Type[] Simple = new[]
    {
        typeof(FieldDeclarationSyntax),
        typeof(EventFieldDeclarationSyntax),
        typeof(PropertyDeclarationSyntax),
        typeof(DelegateDeclarationSyntax),
        typeof(LocalDeclarationStatementSyntax),
        typeof(ExpressionStatementSyntax),
        typeof(ReturnStatementSyntax),
        typeof(YieldStatementSyntax),
        typeof(ThrowExpressionSyntax),
        typeof(AwaitExpressionSyntax),
    };
}
