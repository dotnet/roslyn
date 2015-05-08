using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddMissingReference
{
    internal abstract class AbstractAddMissingReferenceCodeFixProvider<TIdentifierNameSyntax> : CodeFixProvider
        where TIdentifierNameSyntax  : SyntaxNode
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var uniqueIdentities = new HashSet<AssemblyIdentity>();
            foreach (var diagnostic in context.Diagnostics)
            {
                var nodes = FindNodes(root, diagnostic);
                var types = GetTypesForNodes(model, nodes, cancellationToken).Distinct();
                AssemblyIdentity identity = GetAssemblyIdentity(types);
                if (identity != null && !uniqueIdentities.Contains(identity) && !identity.Equals(model.Compilation.Assembly.Identity))
                {
                    uniqueIdentities.Add(identity);
                    context.RegisterCodeFix(
                        await AddMissingReferenceCodeAction.CreateAsync(context.Document.Project, identity, context.CancellationToken).ConfigureAwait(false),
                        diagnostic);
                }
            }
        }

        private static IEnumerable<SyntaxNode> FindNodes(SyntaxNode root, Diagnostic diagnostic)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            return node.DescendantNodesAndSelf().OfType<TIdentifierNameSyntax>();
        }

        private static IEnumerable<ITypeSymbol> GetTypesForNodes(SemanticModel model, IEnumerable<SyntaxNode> nodes, CancellationToken cancellationToken)
        {
            foreach (var node in nodes)
            {
                yield return model.GetTypeInfo(node, cancellationToken).Type;
                var symbol = model.GetSymbolInfo(node, cancellationToken).GetAnySymbol();
                if (symbol != null)
                {
                    if (symbol is IMethodSymbol)
                    {
                        foreach (var param in (symbol as IMethodSymbol).Parameters)
                        {
                            yield return param.Type;
                        }
                    }
                    if (symbol is IPropertySymbol)
                    {
                        foreach (var param in (symbol as IPropertySymbol).Parameters)
                        {
                            yield return param.Type;
                        }
                    }
                    yield return symbol?.GetContainingTypeOrThis();
                }
                var originalSymbol = symbol?.OriginalDefinition;
                if (originalSymbol != null)
                {
                    if (originalSymbol is IMethodSymbol)
                    {
                        foreach (var param in (originalSymbol as IMethodSymbol).Parameters)
                        {
                            yield return param.Type;
                        }
                    }
                    if (originalSymbol is IPropertySymbol)
                    {
                        foreach (var param in (originalSymbol as IPropertySymbol).Parameters)
                        {
                            yield return param.Type;
                        }
                    }
                    yield return originalSymbol?.GetContainingTypeOrThis();
                }
            }
        }

        private static AssemblyIdentity GetAssemblyIdentity(IEnumerable<ITypeSymbol> types)
        {
            foreach (var type in types)
            {
                var identity = type?.GetBaseTypesAndThis().OfType<IErrorTypeSymbol>().FirstOrDefault()?.ContainingAssembly?.Identity;
                if (identity != null)
                {
                    return identity;
                }

                identity = type?.AllInterfaces.OfType<IErrorTypeSymbol>().FirstOrDefault()?.ContainingAssembly?.Identity;
                if (identity != null)
                {
                    return identity;
                }
            }

            return null;
        }
    }
}
