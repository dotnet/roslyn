// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

/// <summary>
/// Represents a scoped node.
/// A scope node is a syntax node that contains a scope or a body.
/// Like a method, a class, an enum, etc.
/// </summary>
internal class CSharpScopedNode : CSharpSourceNode
{
    /// <summary>
    /// Gets the scope of the node.
    /// </summary>
    public Scope Scope { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpScopedNode"/> class.
    /// </summary>
    /// <param name="node">The syntax node that represents the scoped node.</param>
    /// <param name="scope">The scope of the scoped node.</param>
    public CSharpScopedNode(SyntaxNode node, Scope scope)
        : base(node)
    {
        this.Scope = scope;
    }

    public override bool IsValid()
    {
        var bodySyntax = NodeHelper.GetBodySyntax(this.Node);
        if (bodySyntax is BlockSyntax blockSyntax)
        {
            // Mark as invalid when any of the brackets is missing.
            if (blockSyntax.CloseBraceToken.IsMissing || blockSyntax.OpenBraceToken.IsMissing)
            {
                return false;
            }
        }
        else if (bodySyntax is StructDeclarationSyntax str)
        {
            if (str.CloseBraceToken.IsMissing || str.OpenBraceToken.IsMissing)
            {
                return false;
            }
        }
        else if (bodySyntax is RecordDeclarationSyntax rec)
        {
            // Record may or may not have the brace tokens depending on how they are
            // initialized.
            // So we will only return false when we detect that they did have an open brace token
            // but then they didn't have a close brace token;
            if (!rec.OpenBraceToken.IsMissing && rec.CloseBraceToken.IsMissing)
            {
                return false;
            }

            // If record doesn't have neither close brace token or semi column
            if (rec.CloseBraceToken.IsMissing && rec.SemicolonToken.IsMissing)
            {
                return false;
            }
        }
        else if (bodySyntax is BaseTypeDeclarationSyntax typeSyntax)
        {
            if (typeSyntax.CloseBraceToken.IsMissing || typeSyntax.OpenBraceToken.IsMissing)
            {
                return false;
            }
        }

        return base.IsValid();
    }

    /// <summary>
    /// Returns a value indicating whether this node is valid replace type of node.
    /// Replace nodes currently only support Class and Method.
    /// </summary>
    public bool IsReplaceScope => this.Scope == Scope.Method || this.Scope == Scope.Class;
}

/// <summary>
/// The level of the scope where this node is located.
/// NOTE: The scope does not represent where this node will be inserted.
/// The scope represents the hierarchy this node has.
/// For example, a Class Scope node means that it needs to be placed next to
/// other classes or interfaces, and not inside them.
/// NOTE: The order in which these scopes are setup on this enum matter.
/// They should go from lower to higher in terms of what goes inside what.
/// For example, Class is the highest scope here, because the class will usually contain methods, and methods cannot contain classes.
/// Same with statements.
/// </summary>
public enum Scope
{
    /// <summary>
    /// Unknown scope.
    /// </summary>
    Unknown,

    /// <summary>
    /// Statement represents all the nodes that are scoped statements such as
    /// if statements, while statements, class declaration statements.
    /// </summary>
    Statement,

    /// <summary>
    /// Method represents all elements that will be at level of method, with scope.
    /// Like method, and constructor.
    /// </summary>
    Method,

    /// <summary>
    /// Class represents all the nodes that can be set at the level of class, like interface,
    /// enum, class, etc.
    /// </summary>
    Class,
}
