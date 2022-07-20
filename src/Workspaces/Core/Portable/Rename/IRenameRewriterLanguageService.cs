// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal interface IRenameRewriterLanguageService : ILanguageService
    {
        /// <summary>
        /// This method annotates the given syntax tree with all the locations that need to be checked for conflict
        /// after the rename operation.  It also renames all the reference locations and expands any conflict locations.
        /// </summary>
        /// <param name="parameters">The options describing this rename operation</param>
        /// <returns>The root of the annotated tree.</returns>
        SyntaxNode AnnotateAndRename(RenameRewriterParameters parameters);

        /// <summary>
        /// Based on the kind of the symbol and the new name, this function determines possible conflicting names that
        /// should be tracked for semantic changes during rename.
        /// </summary>
        /// <param name="symbol">The symbol that gets renamed.</param>
        /// <param name="newName">The new name for the symbol.</param>
        /// <param name="possibleNameConflicts">List where possible conflicting names will be added to.</param>
        void TryAddPossibleNameConflicts(
            ISymbol symbol,
            string newName,
            ICollection<string> possibleNameConflicts);

        /// <summary>
        /// Identifies the conflicts caused by the new declaration created during rename.
        /// </summary>
        /// <param name="replacementText">The replacementText as given from the user.</param>
        /// <param name="renamedSymbol">The new symbol (after rename).</param>
        /// <param name="renameSymbol">The original symbol that got renamed.</param>
        /// <param name="referencedSymbols">All referenced symbols that are part of this rename session.</param>
        /// <param name="baseSolution">The original solution when rename started.</param>
        /// <param name="newSolution">The resulting solution after rename.</param>
        /// <param name="reverseMappedLocations">A mapping from new to old locations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>All locations where conflicts were caused because the new declaration.</returns>
        Task<ImmutableArray<Location>> ComputeDeclarationConflictsAsync(
            string replacementText,
            ISymbol renamedSymbol,
            ISymbol renameSymbol,
            IEnumerable<ISymbol> referencedSymbols,
            Solution baseSolution,
            Solution newSolution,
            IDictionary<Location, Location> reverseMappedLocations,
            CancellationToken cancellationToken);

        /// <summary>
        /// Identifies the conflicts caused by implicitly referencing the renamed symbol.
        /// </summary>
        /// <param name="renameSymbol">The original symbol that got renamed.</param>
        /// <param name="renamedSymbol">The new symbol (after rename).</param>
        /// <param name="implicitReferenceLocations">All implicit reference locations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of implicit conflicts.</returns>
        Task<ImmutableArray<Location>> ComputeImplicitReferenceConflictsAsync(
            ISymbol renameSymbol,
            ISymbol renamedSymbol,
            IEnumerable<ReferenceLocation> implicitReferenceLocations,
            CancellationToken cancellationToken);

        /// <summary>
        /// Identifies the conflicts caused by implicitly referencing the renamed symbol.
        /// </summary>
        /// <param name="renamedSymbol">The new symbol (after rename).</param>
        /// <param name="semanticModel">The SemanticModel of the document in the new solution containing the renamedSymbol</param>
        /// <param name="originalDeclarationLocation">The location of the renamedSymbol in the old solution</param>
        /// <param name="newDeclarationLocationStartingPosition">The starting position of the renamedSymbol in the new solution</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of implicit conflicts.</returns>
        ImmutableArray<Location> ComputePossibleImplicitUsageConflicts(
            ISymbol renamedSymbol,
            SemanticModel semanticModel,
            Location originalDeclarationLocation,
            int newDeclarationLocationStartingPosition,
            CancellationToken cancellationToken);

        /// <summary>
        /// Identifies potential Conflicts into the inner scope locals. This may give false positives.
        /// </summary>
        /// <param name="token">The Token that may introduce errors else where</param>
        /// <param name="newReferencedSymbols">The symbols that this token binds to after the rename
        /// has been applied</param>
        /// <returns>Returns if there is a potential conflict</returns>
        bool LocalVariableConflict(
            SyntaxToken token,
            IEnumerable<ISymbol> newReferencedSymbols);

        /// <summary>
        /// Used to find if the replacement Identifier is valid
        /// </summary>
        /// <param name="replacementText"></param>
        /// <param name="syntaxFactsService"></param>
        /// <returns></returns>
        bool IsIdentifierValid(
            string replacementText,
            ISyntaxFactsService syntaxFactsService);

        /// <summary>
        /// Gets the top most enclosing statement as target to call MakeExplicit on.
        /// It's either the enclosing statement, or if this statement is inside of a lambda expression, the enclosing
        /// statement of this lambda.
        /// </summary>
        /// <param name="token">The token to get the complexification target for.</param>
        /// <returns></returns>
        SyntaxNode? GetExpansionTargetForLocation(SyntaxToken token);

        /// <summary>
        /// Indicate if the token is renameable in structure comment.
        /// </summary>
        bool IsRenamableTokenInComment(SyntaxToken token);
    }

    internal abstract class AbstractRenameRewriterLanguageService : IRenameRewriterLanguageService
    {
        public abstract SyntaxNode AnnotateAndRename(RenameRewriterParameters parameters);
        public abstract Task<ImmutableArray<Location>> ComputeDeclarationConflictsAsync(string replacementText, ISymbol renamedSymbol, ISymbol renameSymbol, IEnumerable<ISymbol> referencedSymbols, Solution baseSolution, Solution newSolution, IDictionary<Location, Location> reverseMappedLocations, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<Location>> ComputeImplicitReferenceConflictsAsync(ISymbol renameSymbol, ISymbol renamedSymbol, IEnumerable<ReferenceLocation> implicitReferenceLocations, CancellationToken cancellationToken);
        public abstract ImmutableArray<Location> ComputePossibleImplicitUsageConflicts(ISymbol renamedSymbol, SemanticModel semanticModel, Location originalDeclarationLocation, int newDeclarationLocationStartingPosition, CancellationToken cancellationToken);
        public abstract SyntaxNode? GetExpansionTargetForLocation(SyntaxToken token);
        public abstract bool IsIdentifierValid(string replacementText, ISyntaxFactsService syntaxFactsService);
        public abstract bool LocalVariableConflict(SyntaxToken token, IEnumerable<ISymbol> newReferencedSymbols);
        public abstract void TryAddPossibleNameConflicts(ISymbol symbol, string newName, ICollection<string> possibleNameConflicts);
        public abstract bool IsRenamableTokenInComment(SyntaxToken token);

        protected static void AddConflictingParametersOfProperties(
            IEnumerable<ISymbol> properties, string newPropertyName, ArrayBuilder<Location> conflicts)
        {
            // check if the new property name conflicts with any parameter of the properties.
            // Note: referencedSymbols come from the original solution, so there is no need to reverse map the locations of the parameters
            foreach (var symbol in properties)
            {
                var prop = (IPropertySymbol)symbol;

                var conflictingParameter = prop.Parameters.FirstOrDefault(
                    param => string.Compare(param.Name, newPropertyName, StringComparison.OrdinalIgnoreCase) == 0);

                if (conflictingParameter != null)
                {
                    conflicts.AddRange(conflictingParameter.Locations);
                }
            }
        }

        protected static Dictionary<SymbolKey, RenameSymbolContext> GroupRenameContextBySymbolKey(
            ImmutableArray<RenameSymbolContext> symbolContexts, IEqualityComparer<SymbolKey> comparer)
        {
            var renameContexts = new Dictionary<SymbolKey, RenameSymbolContext>(comparer);
            foreach (var context in symbolContexts)
            {
                renameContexts[context.RenamedSymbol.GetSymbolKey()] = context;
            }

            return renameContexts;
        }

        protected static Dictionary<TextSpan, LocationRenameContext> GroupTextRenameContextsByTextSpan(
            ImmutableArray<LocationRenameContext> textSpanRenameContexts)
        {
            var textSpanToRenameContext = new Dictionary<TextSpan, LocationRenameContext>();
            foreach (var context in textSpanRenameContexts)
            {
                var textSpan = context.RenameLocation.Location.SourceSpan;
                if (!textSpanToRenameContext.TryGetValue(textSpan, out var existingRenameContext))
                {
                    textSpanToRenameContext[textSpan] = context;
                }
                else if (!existingRenameContext.Equals(context))
                {
                    // A textSpan is being renamed with different rename location info.
                    throw new ArgumentException(
                        $"{textSpan} of {context.RenameLocation.DocumentId} is being renamed to two differnt locations info: {existingRenameContext}, {context}. ");
                }
            }

            return textSpanToRenameContext;
        }

        protected static Dictionary<TextSpan, HashSet<LocationRenameContext>> GroupStringAndCommentsTextSpanRenameContexts(
            ImmutableArray<LocationRenameContext> renameSymbolContexts)
        {
            var textSpanToRenameContexts = new Dictionary<TextSpan, HashSet<LocationRenameContext>>();
            foreach (var context in renameSymbolContexts)
            {
                var containingSpan = context.RenameLocation.ContainingLocationForStringOrComment;
                if (textSpanToRenameContexts.TryGetValue(containingSpan, out var existingContexts))
                {
                    existingContexts.Add(context);
                }
                else
                {
                    textSpanToRenameContexts[containingSpan] = new HashSet<LocationRenameContext>() { context };
                }
            }

            return textSpanToRenameContexts;
        }

        protected static ImmutableHashSet<RenameSymbolContext> FilterRenameSymbolContexts(
            IEnumerable<RenameSymbolContext> renameContexts, Func<RenameSymbolContext, bool> predicate)
        {
            using var _ = PooledHashSet<RenameSymbolContext>.GetInstance(out var builder);

            foreach (var renameSymbolContext in renameContexts)
            {
                if (predicate(renameSymbolContext))
                    builder.Add(renameSymbolContext);
            }

            return builder.ToImmutableHashSet();
        }

        /// <summary>
        /// Given a set of renameLocations, create a sorted dictionary, maps the renameLocation to its replacementText and matchedText.
        /// The map is later used when rename inside a comment/string.
        /// e.g.
        /// class Hello
        /// {
        ///     void World()
        ///     {
        ///         var hello = "Hello World".
        ///     }
        /// }
        /// If rename class 'Hello' to 'Hello2', void 'World' to 'World2',
        /// <param name="locationRenameContexts"/> would contain the subspan info within string "Hello World".
        /// The output dictionary woud looks like
        /// {
        ///     "0-4" : ("Hello", "Hello2"),
        ///     "6-10" : ("World", "World2")
        /// }
        /// </summary>
        internal static ImmutableSortedDictionary<TextSpan, (string replacementText, string matchText)> CreateSubSpanToReplacementTextInfoDictionary(
            HashSet<LocationRenameContext> locationRenameContexts)
        {
            var subSpanToReplacementTextBuilder = ImmutableSortedDictionary.CreateBuilder<TextSpan, (string replacementText, string matchText)>();
            foreach (var context in locationRenameContexts)
            {
                var renameLocation = context.RenameLocation;
                var location = renameLocation.Location;
                var replacementText = context.ReplacementText;
                var originalText = context.OriginalText;
                if (location.IsInSource && renameLocation.IsRenameInStringOrComment)
                {
                    var sourceSpan = location.SourceSpan;

                    // SourceSpan should be a part of the containing location.
                    RoslynDebug.Assert(sourceSpan.Start >= renameLocation.ContainingLocationForStringOrComment.Start);
                    RoslynDebug.Assert(sourceSpan.End <= renameLocation.ContainingLocationForStringOrComment.End);

                    // Calculate the relative postion within the containg location.
                    var subSpan = new TextSpan(
                        sourceSpan.Start - renameLocation.ContainingLocationForStringOrComment.Start,
                        sourceSpan.Length);

                    if (!subSpanToReplacementTextBuilder.TryGetValue(subSpan, out var replacementTextAndOriginalText))
                    {
                        subSpanToReplacementTextBuilder[subSpan] = (replacementText, originalText);
                    }
                    else if (!replacementTextAndOriginalText.Equals((replacementText, originalText)))
                    {
                        // Two symbol tries to rename a same subspan,
                        // Exmaple:
                        //      // Comment Hello
                        // class Hello
                        // {
                        //    
                        // }
                        // class World
                        // {
                        //    void Hello() { }
                        // }
                        // If try to rename both 'class Hello' to 'Bar' and 'void Hello()' to 'Goo'. So both of them will try to rename
                        // 'Comment Hello'.
                        throw new ArgumentException(
                            $"{sourceSpan} of {context.RenameLocation.DocumentId} is being renamed to two differnt locations text: {replacementTextAndOriginalText.replacementText}, {replacementText}. ");
                    }
                }
            }

            return subSpanToReplacementTextBuilder.ToImmutable();
        }

    }
}
