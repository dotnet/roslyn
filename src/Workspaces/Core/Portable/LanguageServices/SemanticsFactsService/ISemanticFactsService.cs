// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ISemanticFactsService : ILanguageService
    {
        /// <summary>
        /// True if this language supports implementing an interface by signature only. If false,
        /// implementations must specific explicitly which symbol they're implementing.
        /// </summary>
        bool SupportsImplicitInterfaceImplementation { get; }

        bool SupportsParameterizedProperties { get; }

        /// <summary>
        /// True if anonymous functions in this language have signatures that include named
        /// parameters that can be referenced later on when the function is invoked.  Or, if the
        /// anonymous function is simply a signature that will be assigned to a delegate, and the
        /// delegate's parameter names are used when invoking.  
        /// 
        /// For example, in VB one can do this: 
        /// 
        /// dim v = Sub(x as Integer) Blah()
        /// v(x:=4)
        /// 
        /// However, in C# that would need to be:
        /// 
        /// Action&lt;int&gt; v = (int x) => Blah();
        /// v(obj:=4)
        /// 
        /// Note that in VB one can access 'x' outside of the declaration of the anonymous type.
        /// While in C# 'x' can only be accessed within the anonymous type.
        /// </summary>
        bool ExposesAnonymousFunctionParameterNames { get; }

        bool IsExpressionContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsStatementContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsTypeContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsNamespaceContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsNamespaceDeclarationNameContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsTypeDeclarationContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsMemberDeclarationContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsPreProcessorDirectiveContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsGlobalStatementContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsLabelContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        bool IsAttributeNameContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);

        bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol expressionTypeOpt, CancellationToken cancellationToken);

        /// <summary>
        /// True if a write is performed to the given expression.  Note: reads may also be performed
        /// to the expression as well.  For example, "++a".  In this expression 'a' is both read from
        /// and written to.
        /// </summary>
        bool IsWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);

        /// <summary>
        /// True if a write is performed to the given expression.  Note: unlike IsWrittenTo, this
        /// will not return true if reads are performed on the expression as well.  For example,
        /// "++a" will return 'false'.  However, 'a' in "out a" or "a = 1" will return true.
        /// </summary>
        bool IsOnlyWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        bool IsInOutContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        bool IsInRefContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        bool IsInInContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);

        bool CanReplaceWithRValue(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken);

        string GenerateNameForExpression(SemanticModel semanticModel, SyntaxNode expression, bool capitalize, CancellationToken cancellationToken);

        ISymbol GetDeclaredSymbol(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken);

        bool LastEnumValueHasInitializer(INamedTypeSymbol namedTypeSymbol);

        /// <summary>
        /// return speculative semantic model for supported node. otherwise, it will return null
        /// </summary>
        bool TryGetSpeculativeSemanticModel(SemanticModel oldSemanticModel, SyntaxNode oldNode, SyntaxNode newNode, out SemanticModel speculativeModel);

        /// <summary>
        /// get all alias names defined in the semantic model
        /// </summary>
        ImmutableHashSet<string> GetAliasNameSet(SemanticModel model, CancellationToken cancellationToken);

        ForEachSymbols GetForEachSymbols(SemanticModel semanticModel, SyntaxNode forEachStatement);

        IMethodSymbol GetGetAwaiterMethod(SemanticModel semanticModel, SyntaxNode node);

        ImmutableArray<IMethodSymbol> GetDeconstructionAssignmentMethods(SemanticModel semanticModel, SyntaxNode node);

        ImmutableArray<IMethodSymbol> GetDeconstructionForEachMethods(SemanticModel semanticModel, SyntaxNode node);

        bool IsPartial(ITypeSymbol typeSymbol, CancellationToken cancellationToken);

        IEnumerable<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, SyntaxNode memberDeclaration, CancellationToken cancellationToken);

        IParameterSymbol FindParameterForArgument(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken);

        ImmutableArray<ISymbol> GetBestOrAllSymbols(SemanticModel semanticModel, SyntaxNode node, SyntaxToken token, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel, SyntaxNode location,
            SyntaxNode containerOpt, string baseName, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel, SyntaxNode location,
            SyntaxNode containerOpt, string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueName(SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt, string baseName,
            Func<ISymbol, bool> filter, IEnumerable<string> usedNames, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueLocalName(
            SemanticModel semanticModel, SyntaxNode location,
            SyntaxNode containerOpt, string baseName, CancellationToken cancellationToken);

        bool IsInsideNameOfExpression(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
    }
}
