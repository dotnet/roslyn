// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal abstract partial class AbstractBreakpointResolver
    {
        // I believe this is a close approximation of the IVsDebugName string format produced 
        // by the native language service implementations:
        //
        //   C#: csharp\radmanaged\DebuggerInteraction\BreakpointNameResolver.cs
        //   VB: vb\Language\VsEditor\Debugging\VsLanguageDebugInfo.vb
        //
        // The one clear deviation from the native implementation is VB properties.  Resolving
        // the name of a property in VB used to return all the accessor methods (using their
        // metadata names) because setting a breakpoint directly on a property isn't supported
        // in VB.  In Roslyn, we'll keep things consistent and just return the property name.
        // This means that VB users won't be able to set breakpoints on property accessors using
        // Ctrl+B, but it would seem that a better solution to this problem would be to simply
        // enable setting breakpoints on all accessors by setting a breakpoint on the property
        // declaration (same as C# behavior).
        private static readonly SymbolDisplayFormat s_vsDebugNameFormat =
            new(
                globalNamespaceStyle:
                    SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                    SymbolDisplayParameterOptions.IncludeType,
                propertyStyle:
                    SymbolDisplayPropertyStyle.NameOnly,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected readonly string Text;
        private readonly string _language;
        private readonly Solution _solution;
        private readonly IEqualityComparer<string> _identifierComparer;

        protected AbstractBreakpointResolver(
            Solution solution,
            string text,
            string language,
            IEqualityComparer<string> identifierComparer)
        {
            _solution = solution;
            Text = text;
            _language = language;
            _identifierComparer = identifierComparer;
        }

        protected abstract void ParseText(out IList<NameAndArity> nameParts, out int? parameterCount);
        protected abstract IEnumerable<ISymbol> GetMembers(INamedTypeSymbol type, string name);
        protected abstract bool HasMethodBody(IMethodSymbol method, CancellationToken cancellationToken);

        private BreakpointResolutionResult CreateBreakpoint(ISymbol methodSymbol)
        {
            var location = methodSymbol.Locations.First(loc => loc.IsInSource);

            var document = _solution.GetDocument(location.SourceTree);
            var textSpan = new TextSpan(location.SourceSpan.Start, 0);
            var vsDebugName = methodSymbol.ToDisplayString(s_vsDebugNameFormat);

            return BreakpointResolutionResult.CreateSpanResult(document, textSpan, vsDebugName);
        }

        public async Task<IEnumerable<BreakpointResolutionResult>> DoAsync(CancellationToken cancellationToken)
        {
            try
            {
                ParseText(out var nameParts, out var parameterCount);

                // Notes:  In C#, indexers can't be resolved by any name.  This is acceptable, because the old language
                //         service wasn't able to resolve them either.  In VB, parameterized properties will work in
                //         the same way as any other property.
                //         Destructors in C# can be resolved using the method name "Finalize". The resulting string
                //         representation will use C# language format ("C.~C()").  I verified that this works with
                //         "Break at Function" (breakpoint is correctly set and can be hit), so I don't see a reason
                //         to prohibit this (even though the old language service didn't support it).
                var members = await FindMembersAsync(nameParts, cancellationToken).ConfigureAwait(false);

                // Filter down the list of symbols to "applicable methods", specifically:
                // - "regular" methods
                // - constructors
                // - destructors
                // - properties
                // - operators?
                // - conversions?
                // where "applicable" means that the method or property represents a valid place to set a breakpoint
                // and that it has the expected number of parameters
                return members.Where(m => IsApplicable(m, parameterCount, cancellationToken)).
                    Select(CreateBreakpoint).ToImmutableArrayOrEmpty();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ImmutableArray<BreakpointResolutionResult>.Empty;
            }
        }

        private async Task<IEnumerable<ISymbol>> FindMembersAsync(
            IList<NameAndArity> nameParts, CancellationToken cancellationToken)
        {
            try
            {
                switch (nameParts.Count)
                {
                    case 0:
                        // If there were no name parts, then we don't have any members to return.
                        // We only expect to hit this condition when the name provided does not parse.
                        return SpecializedCollections.EmptyList<ISymbol>();

                    case 1:
                        // They're just searching for a method name.  Have to look through every type to find
                        // it.
                        return FindMembers(await GetAllTypesAsync(cancellationToken).ConfigureAwait(false), nameParts[0]);

                    case 2:
                        // They have a type name and a method name.  Find a type with a matching name and a
                        // method in that type.
                        var types = await GetAllTypesAsync(cancellationToken).ConfigureAwait(false);
                        types = types.Where(t => MatchesName(t, nameParts[0], _identifierComparer));
                        return FindMembers(types, nameParts[1]);

                    default:
                        // They have a namespace or nested type qualified name.  Walk up to the root namespace trying to match.
                        var containers = await _solution.GetGlobalNamespacesAsync(cancellationToken).ConfigureAwait(false);
                        return FindMembers(containers, nameParts.ToArray());
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ImmutableArray<ISymbol>.Empty;
            }
        }

        private static bool MatchesName(INamespaceOrTypeSymbol typeOrNamespace, NameAndArity nameAndArity, IEqualityComparer<string> comparer)
        {
            switch (typeOrNamespace)
            {
                case INamespaceSymbol namespaceSymbol:
                    return comparer.Equals(namespaceSymbol.Name, nameAndArity.Name) && nameAndArity.Arity == 0;
                case INamedTypeSymbol typeSymbol:
                    return comparer.Equals(typeSymbol.Name, nameAndArity.Name) &&
                        (nameAndArity.Arity == 0 || nameAndArity.Arity == typeSymbol.TypeArguments.Length);
                default:
                    return false;
            }
        }

        private static bool MatchesNames(INamedTypeSymbol type, NameAndArity[] names, IEqualityComparer<string> comparer)
        {
            Debug.Assert(type != null);
            Debug.Assert(names.Length >= 2);

            INamespaceOrTypeSymbol container = type;

            // The last element in "names" is the method/property name, but we're only matching against types here,
            // so we'll skip the last one.
            for (var i = names.Length - 2; i >= 0; i--)
            {
                if (!MatchesName(container, names[i], comparer))
                {
                    return false;
                }

                container = ((INamespaceOrTypeSymbol)container.ContainingType) ?? container.ContainingNamespace;

                // We ran out of containers to match against before we matched all the names, so this type isn't a match.
                if (container == null && i > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<ISymbol> FindMembers(IEnumerable<INamespaceOrTypeSymbol> containers, params NameAndArity[] names)
        {
            // Recursively expand the list of containers to include all types in all nested containers, then filter down to a
            // set of candidate types by walking the up the enclosing containers matching by simple name.
            var types = containers.SelectMany(GetTypeMembersRecursive).Where(t => MatchesNames(t, names, _identifierComparer));

            var lastName = names.Last();

            return FindMembers(types, lastName);
        }

        private IEnumerable<ISymbol> FindMembers(IEnumerable<INamedTypeSymbol> types, NameAndArity nameAndArity)
        {
            // Get the matching members from all types (including constructors and explicit interface
            // implementations).  If there is a partial method, prefer returning the implementation over
            // the definition (since the definition will not be a candidate for setting a breakpoint).
            var members = types.SelectMany(t => GetMembers(t, nameAndArity.Name))
                               .Select(s => GetPartialImplementationPartOrNull(s) ?? s);

            return nameAndArity.Arity == 0
                ? members
                : members.OfType<IMethodSymbol>().Where(m => m.TypeParameters.Length == nameAndArity.Arity);
        }

        private async Task<IEnumerable<INamedTypeSymbol>> GetAllTypesAsync(CancellationToken cancellationToken)
        {
            var namespaces = await _solution.GetGlobalNamespacesAsync(cancellationToken).ConfigureAwait(false);
            return namespaces.GetAllTypes(cancellationToken);
        }

        private static IMethodSymbol GetPartialImplementationPartOrNull(ISymbol symbol)
            => (symbol.Kind == SymbolKind.Method) ? ((IMethodSymbol)symbol).PartialImplementationPart : null;

        /// <summary>
        /// Is this method or property a valid place to set a breakpoint and does it match the expected parameter count?
        /// </summary>
        private bool IsApplicable(ISymbol methodOrProperty, int? parameterCount, CancellationToken cancellationToken)
        {
            // You can only set a breakpoint on methods (including constructors/destructors) and properties.
            var kind = methodOrProperty.Kind;
            if (kind is not (SymbolKind.Method or SymbolKind.Property))
            {
                return false;
            }

            // You can't set a breakpoint on an abstract method or property.
            if (methodOrProperty.IsAbstract)
            {
                return false;
            }

            // If parameters were provided, check to make sure the method or property has the expected number
            // of parameters (but we don't actually validate the type or name of the supplied parameters).
            if (parameterCount != null)
            {
                var mismatch = IsMismatch(methodOrProperty, parameterCount);

                if (mismatch)
                {
                    return false;
                }
            }

            // Finally, check to make sure we have source, and if we've got a method symbol, make sure it
            // has a body to set a breakpoint on.
            if ((methodOrProperty.Language == _language) && methodOrProperty.Locations.Any(static location => location.IsInSource))
            {
                if (methodOrProperty.IsKind(SymbolKind.Method))
                {
                    return HasMethodBody((IMethodSymbol)methodOrProperty, cancellationToken);
                }

                // Non-abstract properties are always applicable, because you can set a breakpoint on the
                // accessor methods (get and/or set).
                return true;
            }

            return false;
        }

        private static bool IsMismatch(ISymbol methodOrProperty, int? parameterCount)
            => methodOrProperty switch
            {
                IMethodSymbol method => method.Parameters.Length != parameterCount,
                IPropertySymbol property => property.Parameters.Length != parameterCount,
                _ => false,
            };

        private static IEnumerable<INamedTypeSymbol> GetTypeMembersRecursive(INamespaceOrTypeSymbol container)
            => container switch
            {
                INamespaceSymbol namespaceSymbol => namespaceSymbol.GetMembers().SelectMany(GetTypeMembersRecursive),
                INamedTypeSymbol typeSymbol => typeSymbol.GetTypeMembers().SelectMany(GetTypeMembersRecursive).Concat(typeSymbol),
                _ => null,
            };
    }
}
