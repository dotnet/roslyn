using System.Linq;
using System.Threading;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.Extensions
{
    internal static class ISymbolDeclarationServiceExtensions
    {
        /// <summary>
        /// Find the most relevant declaration for a named type symbol given a specified context
        /// location.  For example, if the context location is contained within one of the
        /// declarations of the symbol, then it will be returned.  
        /// </summary>
        public static CommonSyntaxNode FindMostRelevantDeclaration(
            this ISymbolDeclarationService symbolDeclarationService,
            INamespaceOrTypeSymbol symbol,
            CommonLocation locationOpt,
            CancellationToken cancellationToken)
        {
            var declarations = symbolDeclarationService.GetDeclarations(symbol);

            if (locationOpt != null && locationOpt.IsInSource)
            {
                var token = locationOpt.FindToken(cancellationToken);

                // Prefer a declaration that the context node is contained within. 
                //
                // Note: This behavior is slightly suboptimal in some cases.  For example, when the
                // user has the pattern:
#if false
                C.cs
                partial class C
                {
                    // Stuff.
                }

                C.NestedType.cs
                partial class C
                {
                    class NestedType 
                    {
                        // Context location.  
                    }
                }
#endif
                // If we're at the specified context location, but we're trying to find the most
                // relevant part for C, then we want to pick the part in C.cs not the one in
                // C.NestedType.cs that contains the context location.  This is because this simply
                // container really isn't really used by the user to place code, but is instead just
                // used to separate out the nested type.  It would be nice to detect this and do the
                // right thing.
                var bestDeclaration = declarations.Where(token.GetAncestors<CommonSyntaxNode>().Contains).FirstOrDefault();
                if (bestDeclaration != null)
                {
                    return bestDeclaration;
                }

                // Then, prefer a declaration from the same file.
                bestDeclaration = declarations.Where(d => d.SyntaxTree == locationOpt.SourceTree).FirstOrDefault();
                if (bestDeclaration != null)
                {
                    return bestDeclaration;
                }
            }

            // Generate into any decl we can find.
            return declarations.FirstOrDefault();
        }
    }
}
