// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    [ExportLanguageService(typeof(IRenameRewriterLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpRenameConflictLanguageService : AbstractRenameRewriterLanguageService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRenameConflictLanguageService()
        {
        }
        #region "Annotation"

        public override SyntaxNode AnnotateAndRename(RenameRewriterParameters parameters)
        {
            var renameAnnotationRewriter = new MultipleSymbolsRenameRewriter(parameters);
            return renameAnnotationRewriter.Visit(parameters.SyntaxRoot)!;
        }

        #endregion

        #region "Declaration Conflicts"

        public override bool LocalVariableConflict(
            SyntaxToken token,
            IEnumerable<ISymbol> newReferencedSymbols)
        {
            if (token.Parent.IsKind(SyntaxKind.IdentifierName, out ExpressionSyntax? expression) &&
                token.Parent.IsParentKind(SyntaxKind.InvocationExpression) &&
                token.GetPreviousToken().Kind() != SyntaxKind.DotToken &&
                token.GetNextToken().Kind() != SyntaxKind.DotToken)
            {
                var enclosingMemberDeclaration = expression.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                if (enclosingMemberDeclaration != null)
                {
                    var locals = enclosingMemberDeclaration.GetLocalDeclarationMap()[token.ValueText];
                    if (locals.Length > 0)
                    {
                        // This unqualified invocation name matches the name of an existing local
                        // or parameter. Report a conflict if the matching local/parameter is not
                        // a delegate type.

                        var relevantLocals = newReferencedSymbols
                            .Where(s => s.MatchesKind(SymbolKind.Local, SymbolKind.Parameter) && s.Name == token.ValueText);

                        if (relevantLocals.Count() != 1)
                        {
                            return true;
                        }

                        var matchingLocal = relevantLocals.Single();
                        var invocationTargetsLocalOfDelegateType =
                            (matchingLocal.IsKind(SymbolKind.Local) && ((ILocalSymbol)matchingLocal).Type.IsDelegateType()) ||
                            (matchingLocal.IsKind(SymbolKind.Parameter) && ((IParameterSymbol)matchingLocal).Type.IsDelegateType());

                        return !invocationTargetsLocalOfDelegateType;
                    }
                }
            }

            return false;
        }

        public override async Task<ImmutableArray<Location>> ComputeDeclarationConflictsAsync(
            string replacementText,
            ISymbol renamedSymbol,
            ISymbol renameSymbol,
            IEnumerable<ISymbol> referencedSymbols,
            Solution baseSolution,
            Solution newSolution,
            IDictionary<Location, Location> reverseMappedLocations,
            CancellationToken cancellationToken)
        {
            try
            {
                using var _ = ArrayBuilder<Location>.GetInstance(out var conflicts);

                // If we're renaming a named type, we can conflict with members w/ our same name.  Note:
                // this doesn't apply to enums.
                if (renamedSymbol is INamedTypeSymbol { TypeKind: not TypeKind.Enum } namedType)
                    AddSymbolSourceSpans(conflicts, namedType.GetMembers(renamedSymbol.Name), reverseMappedLocations);

                // If we're contained in a named type (we may be a named type ourself!) then we have a
                // conflict.  NOTE(cyrusn): This does not apply to enums. 
                if (renamedSymbol.ContainingSymbol is INamedTypeSymbol { TypeKind: not TypeKind.Enum } containingNamedType &&
                    containingNamedType.Name == renamedSymbol.Name)
                {
                    AddSymbolSourceSpans(conflicts, SpecializedCollections.SingletonEnumerable(containingNamedType), reverseMappedLocations);
                }

                if (renamedSymbol.Kind is SymbolKind.Parameter or
                    SymbolKind.Local or
                    SymbolKind.RangeVariable)
                {
                    var token = renamedSymbol.Locations.Single().FindToken(cancellationToken);
                    var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                    var visitor = new LocalConflictVisitor(token);

                    visitor.Visit(memberDeclaration);
                    conflicts.AddRange(visitor.ConflictingTokens.Select(t => reverseMappedLocations[t.GetLocation()]));

                    // If this is a parameter symbol for a partial method definition, be sure we visited 
                    // the implementation part's body.
                    if (renamedSymbol is IParameterSymbol renamedParameterSymbol &&
                        renamedSymbol.ContainingSymbol is IMethodSymbol methodSymbol &&
                        methodSymbol.PartialImplementationPart != null)
                    {
                        var matchingParameterSymbol = methodSymbol.PartialImplementationPart.Parameters[renamedParameterSymbol.Ordinal];

                        token = matchingParameterSymbol.Locations.Single().FindToken(cancellationToken);
                        memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                        visitor = new LocalConflictVisitor(token);
                        visitor.Visit(memberDeclaration);
                        conflicts.AddRange(visitor.ConflictingTokens.Select(t => reverseMappedLocations[t.GetLocation()]));
                    }
                }
                else if (renamedSymbol.Kind == SymbolKind.Label)
                {
                    var token = renamedSymbol.Locations.Single().FindToken(cancellationToken);
                    var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                    var visitor = new LabelConflictVisitor(token);

                    visitor.Visit(memberDeclaration);
                    conflicts.AddRange(visitor.ConflictingTokens.Select(t => reverseMappedLocations[t.GetLocation()]));
                }
                else if (renamedSymbol.Kind == SymbolKind.Method)
                {
                    conflicts.AddRange(DeclarationConflictHelpers.GetMembersWithConflictingSignatures((IMethodSymbol)renamedSymbol, trimOptionalParameters: false).Select(t => reverseMappedLocations[t]));

                    // we allow renaming overrides of VB property accessors with parameters in C#.
                    // VB has a special rule that properties are not allowed to have the same name as any of the parameters. 
                    // Because this declaration in C# affects the property declaration in VB, we need to check this VB rule here in C#.
                    var properties = new List<ISymbol>();
                    foreach (var referencedSymbol in referencedSymbols)
                    {
                        var property = await RenameLocations.ReferenceProcessing.TryGetPropertyFromAccessorOrAnOverrideAsync(
                            referencedSymbol, baseSolution, cancellationToken).ConfigureAwait(false);
                        if (property != null)
                            properties.Add(property);
                    }

                    AddConflictingParametersOfProperties(properties.Distinct(), replacementText, conflicts);
                }
                else if (renamedSymbol.Kind == SymbolKind.Alias)
                {
                    // in C# there can only be one using with the same alias name in the same block (top of file of namespace). 
                    // It's ok to redefine the alias in different blocks.
                    var location = renamedSymbol.Locations.Single();
                    var tree = location.SourceTree;
                    Contract.ThrowIfNull(tree);

                    var token = await tree.GetTouchingTokenAsync(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
                    var currentUsing = (UsingDirectiveSyntax)token.Parent!.Parent!.Parent!;

                    var namespaceDecl = token.Parent.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
                    SyntaxList<UsingDirectiveSyntax> usings;
                    if (namespaceDecl != null)
                    {
                        usings = namespaceDecl.Usings;
                    }
                    else
                    {
                        var compilationUnit = (CompilationUnitSyntax)await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                        usings = compilationUnit.Usings;
                    }

                    foreach (var usingDirective in usings)
                    {
                        if (usingDirective.Alias != null && usingDirective != currentUsing)
                        {
                            if (usingDirective.Alias.Name.Identifier.ValueText == currentUsing.Alias!.Name.Identifier.ValueText)
                                conflicts.Add(reverseMappedLocations[usingDirective.Alias.Name.GetLocation()]);
                        }
                    }
                }
                else if (renamedSymbol.Kind == SymbolKind.TypeParameter)
                {
                    foreach (var location in renamedSymbol.Locations)
                    {
                        var token = await location.SourceTree!.GetTouchingTokenAsync(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
                        var currentTypeParameter = token.Parent!;

                        foreach (var typeParameter in ((TypeParameterListSyntax)currentTypeParameter.Parent!).Parameters)
                        {
                            if (typeParameter != currentTypeParameter && token.ValueText == typeParameter.Identifier.ValueText)
                                conflicts.Add(reverseMappedLocations[typeParameter.Identifier.GetLocation()]);
                        }
                    }
                }

                // if the renamed symbol is a type member, it's name should not conflict with a type parameter
                if (renamedSymbol.ContainingType != null && renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name).Contains(renamedSymbol))
                {
                    var conflictingLocations = renamedSymbol.ContainingType.TypeParameters
                        .Where(t => t.Name == renamedSymbol.Name)
                        .SelectMany(t => t.Locations);

                    foreach (var location in conflictingLocations)
                    {
                        var typeParameterToken = location.FindToken(cancellationToken);
                        conflicts.Add(reverseMappedLocations[typeParameterToken.GetLocation()]);
                    }
                }

                return conflicts.ToImmutable();
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task<ISymbol?> GetVBPropertyFromAccessorOrAnOverrideAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            try
            {
                if (symbol.IsPropertyAccessor())
                {
                    var property = ((IMethodSymbol)symbol).AssociatedSymbol!;

                    return property.Language == LanguageNames.VisualBasic ? property : null;
                }

                if (symbol.IsOverride && symbol.GetOverriddenMember() != null)
                {
                    var originalSourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol.GetOverriddenMember(), solution, cancellationToken).ConfigureAwait(false);
                    if (originalSourceSymbol != null)
                    {
                        return await GetVBPropertyFromAccessorOrAnOverrideAsync(originalSourceSymbol, solution, cancellationToken).ConfigureAwait(false);
                    }
                }

                return null;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static void AddSymbolSourceSpans(
            ArrayBuilder<Location> conflicts, IEnumerable<ISymbol> symbols,
            IDictionary<Location, Location> reverseMappedLocations)
        {
            foreach (var symbol in symbols)
            {
                foreach (var location in symbol.Locations)
                {
                    // reverseMappedLocations may not contain the location if the location's token
                    // does not contain the text of it's name (e.g. the getter of "int X { get; }"
                    // does not contain the text "get_X" so conflicting renames to "get_X" will not
                    // have added the getter to reverseMappedLocations).
                    if (location.IsInSource && reverseMappedLocations.ContainsKey(location))
                    {
                        conflicts.Add(reverseMappedLocations[location]);
                    }
                }
            }
        }

        public override async Task<ImmutableArray<Location>> ComputeImplicitReferenceConflictsAsync(
            ISymbol renameSymbol, ISymbol renamedSymbol, IEnumerable<ReferenceLocation> implicitReferenceLocations, CancellationToken cancellationToken)
        {
            // Handle renaming of symbols used for foreach
            var implicitReferencesMightConflict = renameSymbol.Kind == SymbolKind.Property &&
                                                string.Compare(renameSymbol.Name, "Current", StringComparison.OrdinalIgnoreCase) == 0;

            implicitReferencesMightConflict =
                implicitReferencesMightConflict ||
                    (renameSymbol.Kind == SymbolKind.Method &&
                        (string.Compare(renameSymbol.Name, WellKnownMemberNames.MoveNextMethodName, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(renameSymbol.Name, WellKnownMemberNames.GetEnumeratorMethodName, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(renameSymbol.Name, WellKnownMemberNames.GetAwaiter, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(renameSymbol.Name, WellKnownMemberNames.DeconstructMethodName, StringComparison.OrdinalIgnoreCase) == 0));

            // TODO: handle Dispose for using statement and Add methods for collection initializers.

            if (implicitReferencesMightConflict)
            {
                if (renamedSymbol.Name != renameSymbol.Name)
                {
                    foreach (var implicitReferenceLocation in implicitReferenceLocations)
                    {
                        var token = await implicitReferenceLocation.Location.SourceTree!.GetTouchingTokenAsync(
                            implicitReferenceLocation.Location.SourceSpan.Start, cancellationToken, findInsideTrivia: false).ConfigureAwait(false);

                        switch (token.Kind())
                        {
                            case SyntaxKind.ForEachKeyword:
                                return ImmutableArray.Create(((CommonForEachStatementSyntax)token.Parent!).Expression.GetLocation());
                            case SyntaxKind.AwaitKeyword:
                                return ImmutableArray.Create(token.GetLocation());
                        }

                        if (token.Parent.IsInDeconstructionLeft(out var deconstructionLeft))
                        {
                            return ImmutableArray.Create(deconstructionLeft.GetLocation());
                        }
                    }
                }
            }

            return ImmutableArray<Location>.Empty;
        }

        public override ImmutableArray<Location> ComputePossibleImplicitUsageConflicts(
            ISymbol renamedSymbol,
            SemanticModel semanticModel,
            Location originalDeclarationLocation,
            int newDeclarationLocationStartingPosition,
            CancellationToken cancellationToken)
        {
            // TODO: support other implicitly used methods like dispose

            if ((renamedSymbol.Name == "MoveNext" || renamedSymbol.Name == "GetEnumerator" || renamedSymbol.Name == "Current") && renamedSymbol.GetAllTypeArguments().Length == 0)
            {
                // TODO: partial methods currently only show the location where the rename happens as a conflict.
                //       Consider showing both locations as a conflict.
                var baseType = renamedSymbol.ContainingType?.GetBaseTypes().FirstOrDefault();
                if (baseType != null)
                {
                    var implicitSymbols = semanticModel.LookupSymbols(
                        newDeclarationLocationStartingPosition,
                        baseType,
                        renamedSymbol.Name)
                            .Where(sym => !sym.Equals(renamedSymbol));

                    foreach (var symbol in implicitSymbols)
                    {
                        if (symbol.GetAllTypeArguments().Length != 0)
                        {
                            continue;
                        }

                        if (symbol.Kind == SymbolKind.Method)
                        {
                            var method = (IMethodSymbol)symbol;

                            if (symbol.Name == "MoveNext")
                            {
                                if (!method.ReturnsVoid && !method.Parameters.Any() && method.ReturnType.SpecialType == SpecialType.System_Boolean)
                                {
                                    return ImmutableArray.Create(originalDeclarationLocation);
                                }
                            }
                            else if (symbol.Name == "GetEnumerator")
                            {
                                // we are a bit pessimistic here. 
                                // To be sure we would need to check if the returned type is having a MoveNext and Current as required by foreach
                                if (!method.ReturnsVoid &&
                                    !method.Parameters.Any())
                                {
                                    return ImmutableArray.Create(originalDeclarationLocation);
                                }
                            }
                        }
                        else if (symbol.Kind == SymbolKind.Property && symbol.Name == "Current")
                        {
                            var property = (IPropertySymbol)symbol;

                            if (!property.Parameters.Any() && !property.IsWriteOnly)
                            {
                                return ImmutableArray.Create(originalDeclarationLocation);
                            }
                        }
                    }
                }
            }

            return ImmutableArray<Location>.Empty;
        }

        #endregion

        public override void TryAddPossibleNameConflicts(ISymbol symbol, string replacementText, ICollection<string> possibleNameConflicts)
        {
            if (replacementText.EndsWith("Attribute", StringComparison.Ordinal) && replacementText.Length > 9)
            {
                var conflict = replacementText.Substring(0, replacementText.Length - 9);
                if (!possibleNameConflicts.Contains(conflict))
                {
                    possibleNameConflicts.Add(conflict);
                }
            }

            if (symbol.Kind == SymbolKind.Property)
            {
                foreach (var conflict in new string[] { "_" + replacementText, "get_" + replacementText, "set_" + replacementText })
                {
                    if (!possibleNameConflicts.Contains(conflict))
                    {
                        possibleNameConflicts.Add(conflict);
                    }
                }
            }

            // in C# we also need to add the valueText because it can be different from the text in source
            // e.g. it can contain escaped unicode characters. Otherwise conflicts would be detected for
            // v\u0061r and var or similar.
            var valueText = replacementText;
            var kind = SyntaxFacts.GetKeywordKind(replacementText);
            if (kind != SyntaxKind.None)
            {
                valueText = SyntaxFacts.GetText(kind);
            }
            else
            {
                var name = SyntaxFactory.ParseName(replacementText);
                if (name.Kind() == SyntaxKind.IdentifierName)
                {
                    valueText = ((IdentifierNameSyntax)name).Identifier.ValueText;
                }
            }

            // this also covers the case of an escaped replacementText
            if (valueText != replacementText)
            {
                possibleNameConflicts.Add(valueText);
            }
        }

        /// <summary>
        /// Gets the top most enclosing statement or CrefSyntax as target to call MakeExplicit on.
        /// It's either the enclosing statement, or if this statement is inside of a lambda expression, the enclosing
        /// statement of this lambda.
        /// </summary>
        /// <param name="token">The token to get the complexification target for.</param>
        /// <returns></returns>
        public override SyntaxNode? GetExpansionTargetForLocation(SyntaxToken token)
            => GetExpansionTarget(token);

        private static SyntaxNode? GetExpansionTarget(SyntaxToken token)
        {
            // get the directly enclosing statement
            var enclosingStatement = token.GetAncestors(n => n is StatementSyntax).FirstOrDefault();

            // System.Func<int, int> myFunc = arg => X;
            var possibleLambdaExpression = enclosingStatement == null
                ? token.GetAncestors(n => n is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax).FirstOrDefault()
                : null;
            if (possibleLambdaExpression != null)
            {
                var lambdaExpression = ((LambdaExpressionSyntax)possibleLambdaExpression);
                if (lambdaExpression.Body is ExpressionSyntax)
                {
                    return lambdaExpression.Body;
                }
            }

            // int M() => X;
            var possibleArrowExpressionClause = enclosingStatement == null
                ? token.GetAncestors<ArrowExpressionClauseSyntax>().FirstOrDefault()
                : null;
            if (possibleArrowExpressionClause != null)
            {
                return possibleArrowExpressionClause.Expression;
            }

            var enclosingNameMemberCrefOrnull = token.GetAncestors(n => n is NameMemberCrefSyntax).LastOrDefault();
            if (enclosingNameMemberCrefOrnull != null)
            {
                if (token.Parent is TypeSyntax && token.Parent.Parent is TypeSyntax)
                {
                    enclosingNameMemberCrefOrnull = null;
                }
            }

            var enclosingXmlNameAttr = token.GetAncestors(n => n is XmlNameAttributeSyntax).FirstOrDefault();
            if (enclosingXmlNameAttr != null)
            {
                return null;
            }

            var enclosingInitializer = token.GetAncestors<EqualsValueClauseSyntax>().FirstOrDefault();
            if (enclosingStatement == null && enclosingInitializer != null && enclosingInitializer.Parent is VariableDeclaratorSyntax)
            {
                return enclosingInitializer.Value;
            }

            var attributeSyntax = token.GetAncestor<AttributeSyntax>();
            if (attributeSyntax != null)
            {
                return attributeSyntax;
            }

            // there seems to be no statement above this one. Let's see if we can at least get an SimpleNameSyntax
            return enclosingStatement ?? enclosingNameMemberCrefOrnull ?? token.GetAncestors(n => n is SimpleNameSyntax).FirstOrDefault();
        }

        #region "Helper Methods"

        public override bool IsIdentifierValid(string replacementText, ISyntaxFactsService syntaxFactsService)
        {
            // Identifiers we never consider valid to rename to.
            switch (replacementText)
            {
                case "var":
                case "dynamic":
                case "unmanaged":
                case "notnull":
                    return false;
            }

            var escapedIdentifier = replacementText.StartsWith("@", StringComparison.Ordinal)
                ? replacementText : "@" + replacementText;

            // Make sure we got an identifier. 
            if (!syntaxFactsService.IsValidIdentifier(escapedIdentifier))
            {
                // We still don't have an identifier, so let's fail
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the semantic model for the given node.
        /// If the node belongs to the syntax tree of the original semantic model, then returns originalSemanticModel.
        /// Otherwise, returns a speculative model.
        /// The assumption for the later case is that span start position of the given node in it's syntax tree is same as
        /// the span start of the original node in the original syntax tree.
        /// </summary>
        public static SemanticModel? GetSemanticModelForNode(SyntaxNode node, SemanticModel originalSemanticModel)
        {
            if (node.SyntaxTree == originalSemanticModel.SyntaxTree)
            {
                // This is possible if the previous rename phase didn't rewrite any nodes in this tree.
                return originalSemanticModel;
            }

            var nodeToSpeculate = node.GetAncestorsOrThis(n => SpeculationAnalyzer.CanSpeculateOnNode(n)).LastOrDefault();
            if (nodeToSpeculate == null)
            {
                if (node.IsKind(SyntaxKind.NameMemberCref, out NameMemberCrefSyntax? nameMember))
                {
                    nodeToSpeculate = nameMember.Name;
                }
                else if (node.IsKind(SyntaxKind.QualifiedCref, out QualifiedCrefSyntax? qualifiedCref))
                {
                    nodeToSpeculate = qualifiedCref.Container;
                }
                else if (node.IsKind(SyntaxKind.TypeConstraint, out TypeConstraintSyntax? typeConstraint))
                {
                    nodeToSpeculate = typeConstraint.Type;
                }
                else if (node is BaseTypeSyntax baseType)
                {
                    nodeToSpeculate = baseType.Type;
                }
                else
                {
                    return null;
                }
            }

            var isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(node as ExpressionSyntax);
            var position = nodeToSpeculate.SpanStart;
            return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(nodeToSpeculate, originalSemanticModel, position, isInNamespaceOrTypeContext);
        }

        public override bool IsRenamableTokenInComment(SyntaxToken token)
            => token.IsKind(SyntaxKind.XmlTextLiteralToken) || token.IsKind(SyntaxKind.IdentifierToken) && token.Parent.IsKind(SyntaxKind.XmlName);

        #endregion
    }
}
