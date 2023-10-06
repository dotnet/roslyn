// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

internal partial class CSharpSourceNode
{
    /// <summary>
    /// A list of short circuit types.
    /// These are excluded types that we know we don't want to compare against
    /// all other scoped or simple types.
    /// </summary>
    private static bool IsExcluded(SyntaxKind kind) => kind is SyntaxKind.CompilationUnit;

    /// <summary>
    /// The list of supported Scoped nodes.
    /// </summary>
    private static bool IsScoped(SyntaxKind kind, out Scope scopeKind)
    {
        switch (kind)
        {
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.InterfaceDeclaration:
            case SyntaxKind.EnumDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordDeclaration:
                scopeKind = Scope.Class;
                return true;

            case SyntaxKind.MethodDeclaration:
            case SyntaxKind.LocalFunctionStatement:
            case SyntaxKind.ConstructorDeclaration:
                scopeKind = Scope.Method;
                return true;

            case SyntaxKind.WhileStatement:
            case SyntaxKind.IfStatement:
            case SyntaxKind.SwitchStatement:
            case SyntaxKind.DoStatement:
            case SyntaxKind.ForEachStatement:
            case SyntaxKind.ForStatement:
                scopeKind = Scope.Statement;
                return true;
        }

        scopeKind = Scope.None;
        return false;
    }

    /// <summary>
    /// The simple node types.
    /// </summary>
    private static bool IsSimple(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.FieldDeclaration => true,
            SyntaxKind.EventFieldDeclaration => true,
            SyntaxKind.PropertyDeclaration => true,
            SyntaxKind.DelegateDeclaration => true,
            SyntaxKind.LocalDeclarationStatement => true,
            SyntaxKind.ExpressionStatement => true,
            SyntaxKind.ReturnStatement => true,
            SyntaxKind.YieldBreakStatement => true,
            SyntaxKind.YieldReturnStatement => true,
            SyntaxKind.ThrowExpression => true,
            SyntaxKind.AwaitExpression => true,
            _ => false
        };
    }
    

    /// <summary>
    /// Determines whether the node is a scoped node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>True if node is a scoped node, false otherwise.</returns>
    public static bool IsScopedNode(SyntaxNode node, out Scope scope)
        => IsScoped(node.Kind(), out scope);

    /// <summary>
    /// Determines whether the node is a simple node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>True if node is a simple node, false otherwise.</returns>
    public static bool IsSimpleNode(SyntaxNode node)
        => IsSimple(node.Kind());
}
