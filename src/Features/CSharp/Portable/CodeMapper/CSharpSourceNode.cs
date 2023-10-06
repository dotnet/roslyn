// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

/// <summary>
/// Represents a syntax node in C# source code.
/// </summary>
internal partial class CSharpSourceNode
{
    private readonly Lazy<string> _identifierName;

    /// <summary>
    /// The name of the node's identifier.
    /// </summary>
    public string IdentifierName => _identifierName.Value;

    public string ToFullString() => Node.ToFullString();

    /// <summary>
    /// The syntax node wrapped by this class.
    /// </summary>
    public readonly SyntaxNode Node;

    /// <summary>
    /// Gets the scope of the node.
    /// </summary>
    public Scope Scope { get; private set; }

    /// <summary>
    /// Returns a value indicating whether this node is valid replace type of node.
    /// Replace nodes currently only support Class and Method.
    /// </summary>
    public bool IsReplaceScope => Scope == Scope.Method || Scope == Scope.Class;

    /// <summary>
    /// Creates a new instance of the SourceNode class.
    /// </summary>
    /// <param name="node">The syntax node to represent.</param>
    private CSharpSourceNode(SyntaxNode node, Scope scope)
    {
        Node = node;
        Scope = scope;

        _identifierName = new Lazy<string>(() => GetIdentifierName(Node));
    }

    /// <summary>
    /// Checks whether this node exists on a given target node.
    /// </summary>
    /// <param name="node">The target node to check.</param>
    /// <param name="matchingNode">If this node exists on the target node, this is the matching node.</param>
    /// <returns>True if this node exists on the target node, false otherwise.</returns>
    public bool ExistsOnTarget(SyntaxNode node, out SyntaxNode? matchingNode)
    {
        matchingNode = null;

        if (Scope is Scope.None)
        {
            matchingNode = node.DescendantNodesAndSelf()
                .Where(n => IsSimpleNode(n) && GetIdentifierName(n) == IdentifierName)
                .FirstOrDefault();
        }
        else if (IsReplaceScope)
        {
            matchingNode = node.DescendantNodesAndSelf()
                                .Where(n => IsScopedNode(n, out var scope) && scope == Scope && GetIdentifierName(n) == IdentifierName)
                                .FirstOrDefault();
        }

        return matchingNode is not null;
    }

    /// <summary>
    /// Returns a string representation of the node.
    /// </summary>
    /// <returns>A string representation of the node.</returns>
    public override string ToString()
    {
        return Node.ToFullString();
    }

    /// <summary>
    /// Gets the identifier name of a syntax node.
    /// </summary>
    /// <param name="node">The syntax node to get the identifier name for.</param>
    /// <returns>The identifier name of the syntax node.</returns>
    private static string GetIdentifierName(SyntaxNode node)
    {
        return node switch
        {
            (MethodDeclarationSyntax method) => method.Identifier.ToString(),
            (ClassDeclarationSyntax cl) => cl.Identifier.ToString(),
            (InterfaceDeclarationSyntax ifc) => ifc.Identifier.ToString(),
            (PropertyDeclarationSyntax prop) => prop.Identifier.ToString(),
            (EnumDeclarationSyntax enu) => enu.Identifier.ToString(),
            (StructDeclarationSyntax str) => str.Identifier.ToString(),
            (RecordDeclarationSyntax rcr) => rcr.Identifier.ToString(),
            (ConstructorDeclarationSyntax constructor) => BuildConstructorIdentifier(constructor),
            (LocalFunctionStatementSyntax localFunction) => BuildLocalFunctionIdentifier(localFunction),
            (FieldDeclarationSyntax fieldDeclaration) => BuildFieldDeclarationIdentifier(fieldDeclaration),
            _ => node.ToString(),   // Fallback to returning the full string of the node as identifier.
        };
    }

    private static string BuildFieldDeclarationIdentifier(FieldDeclarationSyntax fieldDeclaration)
    {
        var firstDeclarator = fieldDeclaration.Declaration.Variables.FirstOrDefault();
        if (firstDeclarator is not null)
        {
            return firstDeclarator.Identifier.ToString();
        }
        
        // Fallback to returning the full string of the node as identifier.
        return fieldDeclaration.ToString();
    }

    private static string BuildLocalFunctionIdentifier(LocalFunctionStatementSyntax localFunction)
    {
        var identifier = localFunction.Identifier.ToString();

        // when identifier is empty, that means this
        // local function declaration represents a Constructor
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return BuildConstructorIdentifier(localFunction);
        }

        return identifier;
    }

    /// <summary>
    /// Returns a composed version of the constructor's identifier.
    /// The string is composed by the original identifier of the constructor
    /// and the types of its parameters.
    /// Example:
    /// MyClass(string, string, int)
    /// </summary>
    /// <param name="ctor">The constructor declaration syntax</param>
    /// <returns>An identifier string composed by the constructor's identifier and argument types.</returns>
    private static string BuildConstructorIdentifier(SyntaxNode ctor)
    {
        ParameterListSyntax parameters;
        var identifier = string.Empty;
        if (ctor is ConstructorDeclarationSyntax cds)
        {
            identifier = cds.Identifier.ToString();
            parameters = cds.ParameterList;
        }
        else if (ctor is LocalFunctionStatementSyntax lfs)
        {
            identifier = lfs.ReturnType.ToString();
            parameters = lfs.ParameterList;
        }
        else
        {
            return ctor.ToString();
        }

        var parameterTypes = parameters.Parameters
            .Select(p => p.Type?.ToString())
            .Where(type => type is not null);
        if (parameterTypes.Any())
        {
            return $"{identifier}({string.Join(", ", parameterTypes)})";
        }
        else
        {
            return $"{identifier}()";
        }
    }
}
