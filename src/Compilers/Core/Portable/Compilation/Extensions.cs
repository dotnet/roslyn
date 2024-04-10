// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    public static class ModelExtensions
    {
        /// <summary>
        /// Gets symbol information about a syntax node.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="node">The syntax node to get semantic information for.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the
        /// process of obtaining the semantic info.</param>
        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return semanticModel.GetSymbolInfo(node, cancellationToken);
        }

        /// <summary>
        /// Binds the node in the context of the specified location and get semantic information
        /// such as type, symbols and diagnostics. This method is used to get semantic information
        /// about an expression that did not actually appear in the source code.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="position">A character position used to identify a declaration scope and
        /// accessibility. This character position must be within the FullSpan of the Root syntax
        /// node in this SemanticModel.
        /// </param>
        /// <param name="expression">A syntax node that represents a parsed expression. This syntax
        /// node need not and typically does not appear in the source code referred to  SemanticModel
        /// instance.</param>
        /// <param name="bindingOption">Indicates whether to binding the expression as a full expressions,
        /// or as a type or namespace. If SpeculativeBindingOption.BindAsTypeOrNamespace is supplied, then
        /// expression should derive from TypeSyntax.</param>
        /// <returns>The semantic information for the topmost node of the expression.</returns>
        /// <remarks>The passed in expression is interpreted as a stand-alone expression, as if it
        /// appeared by itself somewhere within the scope that encloses "position".</remarks>
        public static SymbolInfo GetSpeculativeSymbolInfo(this SemanticModel semanticModel, int position, SyntaxNode expression, SpeculativeBindingOption bindingOption)
        {
            return semanticModel.GetSpeculativeSymbolInfo(position, expression, bindingOption);
        }

        /// <summary>
        /// Gets type information about a syntax node.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="node">The syntax node to get semantic information for.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the
        /// process of obtaining the semantic info.</param>
        public static TypeInfo GetTypeInfo(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return semanticModel.GetTypeInfo(node, cancellationToken);
        }

        /// <summary>
        /// If "nameSyntax" resolves to an alias name, return the IAliasSymbol corresponding
        /// to A. Otherwise return null.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="nameSyntax">Name to get alias info for.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the
        /// process of obtaining the alias information.</param>
        public static IAliasSymbol? GetAliasInfo(this SemanticModel semanticModel, SyntaxNode nameSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            return semanticModel.GetAliasInfo(nameSyntax, cancellationToken);
        }

        /// <summary>
        /// Binds the name in the context of the specified location and sees if it resolves to an
        /// alias name. If it does, return the AliasSymbol corresponding to it. Otherwise, return null.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="position">A character position used to identify a declaration scope and
        /// accessibility. This character position must be within the FullSpan of the Root syntax
        /// node in this SemanticModel.
        /// </param>
        /// <param name="nameSyntax">A syntax node that represents a name. This syntax
        /// node need not and typically does not appear in the source code referred to by the
        /// SemanticModel instance.</param>
        /// <param name="bindingOption">Indicates whether to binding the name as a full expression,
        /// or as a type or namespace. If SpeculativeBindingOption.BindAsTypeOrNamespace is supplied, then
        /// expression should derive from TypeSyntax.</param>
        /// <remarks>The passed in name is interpreted as a stand-alone name, as if it
        /// appeared by itself somewhere within the scope that encloses "position".</remarks>
        public static IAliasSymbol? GetSpeculativeAliasInfo(this SemanticModel semanticModel, int position, SyntaxNode nameSyntax, SpeculativeBindingOption bindingOption)
        {
            return semanticModel.GetSpeculativeAliasInfo(position, nameSyntax, bindingOption);
        }

        /// <summary>
        /// Binds the node in the context of the specified location and get semantic information
        /// such as type, symbols and diagnostics. This method is used to get semantic information
        /// about an expression that did not actually appear in the source code.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="position">A character position used to identify a declaration scope and
        /// accessibility. This character position must be within the FullSpan of the Root syntax
        /// node in this SemanticModel.
        /// </param>
        /// <param name="expression">A syntax node that represents a parsed expression. This syntax
        /// node need not and typically does not appear in the source code referred to  SemanticModel
        /// instance.</param>
        /// <param name="bindingOption">Indicates whether to binding the expression as a full expressions,
        /// or as a type or namespace. If SpeculativeBindingOption.BindAsTypeOrNamespace is supplied, then
        /// expression should derive from TypeSyntax.</param>
        /// <returns>The semantic information for the topmost node of the expression.</returns>
        /// <remarks>The passed in expression is interpreted as a stand-alone expression, as if it
        /// appeared by itself somewhere within the scope that encloses "position".</remarks>
        public static TypeInfo GetSpeculativeTypeInfo(this SemanticModel semanticModel, int position, SyntaxNode expression, SpeculativeBindingOption bindingOption)
        {
            return semanticModel.GetSpeculativeTypeInfo(position, expression, bindingOption);
        }

        /// <summary>
        /// Gets the symbol associated with a declaration syntax node.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="declaration">A syntax node that is a declaration. This can be any type
        /// derived from MemberDeclarationSyntax, TypeDeclarationSyntax, EnumDeclarationSyntax,
        /// NamespaceDeclarationSyntax, ParameterSyntax, TypeParameterSyntax, or the alias part of a
        /// UsingDirectiveSyntax</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol declared by the node or null if the node is not a declaration.</returns>
        public static ISymbol? GetDeclaredSymbol(this SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken = default(CancellationToken))
        {
            return semanticModel.GetDeclaredSymbolForNode(declaration, cancellationToken);
        }

        /// <summary>
        /// Gets a list of method or indexed property symbols for a syntax node.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="node">The syntax node to get semantic information for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static ImmutableArray<ISymbol> GetMemberGroup(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return semanticModel.GetMemberGroup(node, cancellationToken);
        }

        /// <summary>
        /// Analyze control-flow within a part of a method body. 
        /// </summary>
        public static ControlFlowAnalysis AnalyzeControlFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement)
        {
            return semanticModel.AnalyzeControlFlow(firstStatement, lastStatement);
        }

        /// <summary>
        /// Analyze control-flow within a part of a method body. 
        /// </summary>
        public static ControlFlowAnalysis AnalyzeControlFlow(this SemanticModel semanticModel, SyntaxNode statement)
        {
            return semanticModel.AnalyzeControlFlow(statement);
        }

        /// <summary>
        /// Analyze data-flow within a part of a method body. 
        /// </summary>
        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement)
        {
            return semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
        }

        /// <summary>
        /// Analyze data-flow within a part of a method body.
        /// note (for C#): ConstructorInitializerSyntax and PrimaryConstructorBaseTypeSyntax are treated by this API as regular statements
        /// </summary>
        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, SyntaxNode statementOrExpression)
        {
            return semanticModel.AnalyzeDataFlow(statementOrExpression);
        }
    }
}
