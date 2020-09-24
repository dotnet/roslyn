// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class EditorBrowsableHelpers
    {
        public struct EditorBrowsableInfo
        {
            public Compilation Compilation { get; }
            public INamedTypeSymbol? HideModuleNameAttribute { get; }
            public IMethodSymbol? EditorBrowsableAttributeConstructor { get; }
            public ImmutableArray<IMethodSymbol> TypeLibTypeAttributeConstructors { get; }
            public ImmutableArray<IMethodSymbol> TypeLibFuncAttributeConstructors { get; }
            public ImmutableArray<IMethodSymbol> TypeLibVarAttributeConstructors { get; }
            public bool IsDefault => Compilation == null;

            public EditorBrowsableInfo(Compilation compilation)
            {
                Compilation = compilation;
                HideModuleNameAttribute = compilation.HideModuleNameAttribute();
                EditorBrowsableAttributeConstructor = GetSpecialEditorBrowsableAttributeConstructor(compilation);
                TypeLibTypeAttributeConstructors = GetSpecialTypeLibTypeAttributeConstructors(compilation);
                TypeLibFuncAttributeConstructors = GetSpecialTypeLibFuncAttributeConstructors(compilation);
                TypeLibVarAttributeConstructors = GetSpecialTypeLibVarAttributeConstructors(compilation);
            }
        }

        /// <summary>
        /// Checks a given symbol for browsability based on its declaration location, attributes 
        /// explicitly limiting browsability, and whether showing of advanced members is enabled. 
        /// The optional editorBrowsableInfo parameters may be used to specify the symbols of the
        /// constructors of the various browsability limiting attributes because finding these 
        /// repeatedly over a large list of symbols can be slow. If these are not provided,
        /// they will be found in the compilation.
        /// </summary>
        public static bool IsEditorBrowsable(
            this ISymbol symbol,
            bool hideAdvancedMembers,
            Compilation compilation,
            EditorBrowsableInfo editorBrowsableInfo = default)
        {
            return IsEditorBrowsableWithState(
                symbol,
                hideAdvancedMembers,
                compilation,
                editorBrowsableInfo).isBrowsable;
        }

        // In addition to given symbol's browsability, also returns its EditorBrowsableState if it contains EditorBrowsableAttribute.
        public static (bool isBrowsable, bool isEditorBrowsableStateAdvanced) IsEditorBrowsableWithState(
            this ISymbol symbol,
            bool hideAdvancedMembers,
            Compilation compilation,
            EditorBrowsableInfo editorBrowsableInfo = default)
        {
            // Namespaces can't have attributes, so just return true here.  This also saves us a 
            // costly check if this namespace has any locations in source (since a merged namespace
            // needs to go collect all the locations).
            if (symbol.Kind == SymbolKind.Namespace)
            {
                return (isBrowsable: true, isEditorBrowsableStateAdvanced: false);
            }

            // check for IsImplicitlyDeclared so we don't spend time examining VB's embedded types.
            // This saves a few percent in typing scenarios.  An implicitly declared symbol can't
            // have attributes, so it can't be hidden by them.
            if (symbol.IsImplicitlyDeclared)
            {
                return (isBrowsable: true, isEditorBrowsableStateAdvanced: false);
            }

            if (editorBrowsableInfo.IsDefault)
            {
                editorBrowsableInfo = new EditorBrowsableInfo(compilation);
            }

            // Ignore browsability limiting attributes if the symbol is declared in source.
            // Check all locations since some of VB's embedded My symbols are declared in 
            // both source and the MyTemplateLocation.
            if (symbol.Locations.All(loc => loc.IsInSource))
            {
                // The HideModuleNameAttribute still applies to Modules defined in source
                return (!IsBrowsingProhibitedByHideModuleNameAttribute(symbol, editorBrowsableInfo.HideModuleNameAttribute), isEditorBrowsableStateAdvanced: false);
            }

            var (isProhibited, isEditorBrowsableStateAdvanced) = IsBrowsingProhibited(symbol, hideAdvancedMembers, editorBrowsableInfo);

            return (!isProhibited, isEditorBrowsableStateAdvanced);
        }

        private static (bool isProhibited, bool isEditorBrowsableStateAdvanced) IsBrowsingProhibited(
            ISymbol symbol,
            bool hideAdvancedMembers,
            EditorBrowsableInfo editorBrowsableInfo)
        {
            var attributes = symbol.GetAttributes();
            if (attributes.Length == 0)
            {
                return (isProhibited: false, isEditorBrowsableStateAdvanced: false);
            }

            var (isProhibited, isEditorBrowsableStateAdvanced) = IsBrowsingProhibitedByEditorBrowsableAttribute(attributes, hideAdvancedMembers, editorBrowsableInfo.EditorBrowsableAttributeConstructor);

            return ((isProhibited
                || IsBrowsingProhibitedByTypeLibTypeAttribute(attributes, editorBrowsableInfo.TypeLibTypeAttributeConstructors)
                || IsBrowsingProhibitedByTypeLibFuncAttribute(attributes, editorBrowsableInfo.TypeLibFuncAttributeConstructors)
                || IsBrowsingProhibitedByTypeLibVarAttribute(attributes, editorBrowsableInfo.TypeLibVarAttributeConstructors)
                || IsBrowsingProhibitedByHideModuleNameAttribute(symbol, editorBrowsableInfo.HideModuleNameAttribute, attributes)), isEditorBrowsableStateAdvanced);
        }

        private static bool IsBrowsingProhibitedByHideModuleNameAttribute(
            ISymbol symbol, INamedTypeSymbol? hideModuleNameAttribute, ImmutableArray<AttributeData> attributes = default)
        {
            if (hideModuleNameAttribute == null || !symbol.IsModuleType())
            {
                return false;
            }

            attributes = attributes.IsDefault ? symbol.GetAttributes() : attributes;

            foreach (var attribute in attributes)
            {
                if (Equals(attribute.AttributeClass, hideModuleNameAttribute))
                {
                    return true;
                }
            }

            return false;
        }

        private static (bool isProhibited, bool isEditorBrowsableStateAdvanced) IsBrowsingProhibitedByEditorBrowsableAttribute(
            ImmutableArray<AttributeData> attributes, bool hideAdvancedMembers, IMethodSymbol? constructor)
        {
            if (constructor == null)
            {
                return (isProhibited: false, isEditorBrowsableStateAdvanced: false);
            }

            foreach (var attribute in attributes)
            {
                if (Equals(attribute.AttributeConstructor, constructor) &&
                    attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments.First().Value is int)
                {
#nullable disable // Should use unboxed value from previous 'is int' https://github.com/dotnet/roslyn/issues/39166
                    var state = (EditorBrowsableState)attribute.ConstructorArguments.First().Value;
#nullable enable

                    if (EditorBrowsableState.Never == state)
                    {
                        return (isProhibited: true, isEditorBrowsableStateAdvanced: false);
                    }

                    if (EditorBrowsableState.Advanced == state)
                    {
                        return (isProhibited: hideAdvancedMembers, isEditorBrowsableStateAdvanced: true);
                    }
                }
            }

            return (isProhibited: false, isEditorBrowsableStateAdvanced: false);
        }

        private static bool IsBrowsingProhibitedByTypeLibTypeAttribute(
            ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                attributes,
                constructors,
                TypeLibTypeFlagsFHidden);
        }

        private static bool IsBrowsingProhibitedByTypeLibFuncAttribute(
            ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                attributes,
                constructors,
                TypeLibFuncFlagsFHidden);
        }

        private static bool IsBrowsingProhibitedByTypeLibVarAttribute(
            ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                attributes,
                constructors,
                TypeLibVarFlagsFHidden);
        }

        private const int TypeLibTypeFlagsFHidden = 0x0010;
        private const int TypeLibFuncFlagsFHidden = 0x0040;
        private const int TypeLibVarFlagsFHidden = 0x0040;

        private static bool IsBrowsingProhibitedByTypeLibAttributeWorker(
            ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> attributeConstructors, int hiddenFlag)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.ConstructorArguments.Length == 1)
                {
                    foreach (var constructor in attributeConstructors)
                    {
                        if (Equals(attribute.AttributeConstructor, constructor))
                        {
                            // Check for both constructor signatures. The constructor that takes a TypeLib*Flags reports an int argument.
                            var argumentValue = attribute.ConstructorArguments.First().Value;

                            int actualFlags;
                            if (argumentValue is int i)
                            {
                                actualFlags = i;
                            }
                            else if (argumentValue is short sh)
                            {
                                actualFlags = sh;
                            }
                            else
                            {
                                continue;
                            }

                            if ((actualFlags & hiddenFlag) == hiddenFlag)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// First, remove symbols from the set if they are overridden by other symbols in the set.
        /// If a symbol is overridden only by symbols outside of the set, then it is not removed. 
        /// This is useful for filtering out symbols that cannot be accessed in a given context due
        /// to the existence of overriding members. Second, remove remaining symbols that are
        /// unsupported (e.g. pointer types in VB) or not editor browsable based on the EditorBrowsable
        /// attribute.
        /// </summary>
        public static ImmutableArray<T> FilterToVisibleAndBrowsableSymbols<T>(
            this ImmutableArray<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
        {
            symbols = symbols.RemoveOverriddenSymbolsWithinSet();

            // Since all symbols are from the same compilation, find the required attribute
            // constructors once and reuse.
            var editorBrowsableInfo = new EditorBrowsableInfo(compilation);

            // PERF: HasUnsupportedMetadata may require recreating the syntax tree to get the base class, so first
            // check to see if we're referencing a symbol defined in source.
            static bool isSymbolDefinedInSource(Location l) => l.IsInSource;
            return symbols.WhereAsArray((s, arg) =>
                (s.Locations.Any(isSymbolDefinedInSource) || !s.HasUnsupportedMetadata) &&
                !s.IsDestructor() &&
                s.IsEditorBrowsable(
                    arg.hideAdvancedMembers,
                    arg.editorBrowsableInfo.Compilation,
                    arg.editorBrowsableInfo),
                (hideAdvancedMembers, editorBrowsableInfo));
        }

        public static ImmutableArray<T> FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols<T>(
            this ImmutableArray<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
        {
            return symbols.FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, compilation)
                .WhereAsArray(s => !s.RequiresUnsafeModifier());
        }

        private static ImmutableArray<T> RemoveOverriddenSymbolsWithinSet<T>(this ImmutableArray<T> symbols) where T : ISymbol
        {
            var overriddenSymbols = new HashSet<ISymbol>();

            foreach (var symbol in symbols)
            {
                var overriddenMember = symbol.OverriddenMember();
                if (overriddenMember != null && !overriddenSymbols.Contains(overriddenMember))
                {
                    overriddenSymbols.Add(overriddenMember);
                }
            }

            return symbols.WhereAsArray(s => !overriddenSymbols.Contains(s));
        }

        /// <summary>
        /// Finds the constructor which takes exactly one argument, which must be of type EditorBrowsableState.
        /// It does not require that the EditorBrowsableAttribute and EditorBrowsableState types be those
        /// shipped by Microsoft, but it does demand the types found follow the expected pattern. If at any
        /// point that pattern appears to be violated, return null to indicate that an appropriate constructor
        /// could not be found.
        /// </summary>
        public static IMethodSymbol? GetSpecialEditorBrowsableAttributeConstructor(Compilation compilation)
        {
            var editorBrowsableAttributeType = compilation.EditorBrowsableAttributeType();
            var editorBrowsableStateType = compilation.EditorBrowsableStateType();

            if (editorBrowsableAttributeType == null || editorBrowsableStateType == null)
            {
                return null;
            }

            var candidateConstructors = editorBrowsableAttributeType.Constructors
                                                                    .Where(c => c.Parameters.Length == 1 && Equals(c.Parameters[0].Type, editorBrowsableStateType));

            // Ensure the constructor adheres to the expected EditorBrowsable pattern
            candidateConstructors = candidateConstructors.Where(c => (!c.IsVararg &&
                                                                      !c.Parameters[0].IsRefOrOut() &&
                                                                      !c.Parameters[0].CustomModifiers.Any()));

            // If there are multiple constructors that look correct then the discovered types do not match the
            // expected pattern, so return null.
            if (candidateConstructors.Count() <= 1)
            {
                return candidateConstructors.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public static ImmutableArray<IMethodSymbol> GetSpecialTypeLibTypeAttributeConstructors(Compilation compilation)
        {
            return GetSpecialTypeLibAttributeConstructorsWorker(
                compilation,
                "System.Runtime.InteropServices.TypeLibTypeAttribute",
                "System.Runtime.InteropServices.TypeLibTypeFlags");
        }

        public static ImmutableArray<IMethodSymbol> GetSpecialTypeLibFuncAttributeConstructors(Compilation compilation)
        {
            return GetSpecialTypeLibAttributeConstructorsWorker(
                compilation,
                "System.Runtime.InteropServices.TypeLibFuncAttribute",
                "System.Runtime.InteropServices.TypeLibFuncFlags");
        }

        public static ImmutableArray<IMethodSymbol> GetSpecialTypeLibVarAttributeConstructors(Compilation compilation)
        {
            return GetSpecialTypeLibAttributeConstructorsWorker(
                compilation,
                "System.Runtime.InteropServices.TypeLibVarAttribute",
                "System.Runtime.InteropServices.TypeLibVarFlags");
        }

        /// <summary>
        /// The TypeLib*Attribute classes that accept TypeLib*Flags with FHidden as an option all have two constructors,
        /// one accepting a TypeLib*Flags and the other a short. This methods gets those two constructor symbols for any
        /// of these attribute classes. It does not require that the either of these types be those shipped by Microsoft,
        /// but it does demand the types found follow the expected pattern. If at any point that pattern appears to be
        /// violated, return an empty enumerable to indicate that no appropriate constructors were found.
        /// </summary>
        private static ImmutableArray<IMethodSymbol> GetSpecialTypeLibAttributeConstructorsWorker(
            Compilation compilation,
            string attributeMetadataName,
            string flagsMetadataName)
        {
            var typeLibAttributeType = compilation.GetTypeByMetadataName(attributeMetadataName);
            var typeLibFlagsType = compilation.GetTypeByMetadataName(flagsMetadataName);
            var shortType = compilation.GetSpecialType(SpecialType.System_Int16);

            if (typeLibAttributeType == null || typeLibFlagsType == null || shortType == null)
            {
                return ImmutableArray<IMethodSymbol>.Empty;
            }

            var candidateConstructors = typeLibAttributeType.Constructors
                                                            .Where(c => c.Parameters.Length == 1 &&
                                                                        (Equals(c.Parameters[0].Type, typeLibFlagsType) || Equals(c.Parameters[0].Type, shortType)));

            candidateConstructors = candidateConstructors.Where(c => (!c.IsVararg &&
                                                                      !c.Parameters[0].IsRefOrOut() &&
                                                                      !c.Parameters[0].CustomModifiers.Any()));

            return candidateConstructors.ToImmutableArrayOrEmpty();
        }
    }
}
