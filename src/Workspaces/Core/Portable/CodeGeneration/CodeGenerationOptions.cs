// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// Options for controlling the code produced by the <see cref="CodeGenerator"/>.
    /// </summary>
    internal class CodeGenerationOptions
    {
        public static readonly CodeGenerationOptions Default = new CodeGenerationOptions();

        /// <summary>
        /// A location used to determine the best place to generate a member.  This is only used for
        /// determining which part of a partial type to generate in.  If a type only has one part, or
        /// an API is used that specifies the type, then this is not used.  A part is preferred if
        /// it surrounds this context location. If no part surrounds this location then a part is
        /// preferred if it comes from the same SyntaxTree as this location.  If there is no
        /// such part, then any part may be used for generation.
        /// 
        /// This option is not necessary if <see cref="AfterThisLocation"/> or <see cref="BeforeThisLocation"/> are
        /// provided.
        /// </summary>
        public Location ContextLocation { get; }

        /// <summary>
        /// A hint to the code generation service to specify where the generated code should be
        /// placed.  Code will be generated after this location if the location is valid in the type
        /// or symbol being generated into, and it is possible to generate the code after it.
        /// 
        /// If this option is provided, neither <see cref="ContextLocation"/> nor <see cref="BeforeThisLocation"/> are
        /// needed.
        /// </summary>
        public Location AfterThisLocation { get; }

        /// <summary>
        /// A hint to the code generation service to specify where the generated code should be
        /// placed.  Code will be generated before this location if the location is valid in the type
        /// or symbol being generated into, and it is possible to generate the code after it. 
        /// 
        /// If this option is provided, neither <see cref="ContextLocation"/> nor <see cref="AfterThisLocation"/> are
        /// needed.
        /// </summary>
        public Location BeforeThisLocation { get; }

        /// <summary>
        /// True if the code generation service should add <see cref="Simplifier.AddImportsAnnotation"/>,
        /// and when not generating directly into a declaration, should try to automatically add imports to the file
        /// for any generated code.
        /// Defaults to true.
        /// </summary>
        public bool AddImports { get; }

        /// <summary>
        /// True if, when adding a System import, the import should be placed above non-System
        /// imports.  Defaults to true.  Only used if <see cref="AddImports"/> is true.
        /// </summary>
        public bool PlaceSystemNamespaceFirst { get; }

        /// <summary>
        /// Contains additional imports to be automatically added.  This is useful for adding
        /// imports that are part of a list of statements.
        /// </summary>
        public IEnumerable<INamespaceSymbol> AdditionalImports { get; }

        /// <summary>
        /// True if members of a symbol should also be generated along with the declaration.  If
        /// false, only the symbol's declaration will be generated.
        /// </summary>
        public bool GenerateMembers { get; }

        /// <summary>
        /// True if the code generator should merge namespaces which only contain other namespaces
        /// into a single declaration with a dotted name.  False if the nesting should be preserved
        /// and each namespace declaration should be nested and should only have a single non-dotted
        /// name.
        /// 
        /// Merging can only occur if the namespace only contains a single member that is also a
        /// namespace.
        /// </summary>
        public bool MergeNestedNamespaces { get; }

        /// <summary>
        /// True if the code generation should put multiple attributes in a single attribute
        /// declaration, or if should have a separate attribute declaration for each attribute.  For
        /// example, in C# setting this to True this would produce "[Goo, Bar]" while setting it to
        /// False would produce "[Goo][Bar]"
        /// </summary>
        public bool MergeAttributes { get; }

        /// <summary>
        /// True if the code generator should always generate accessibility modifiers, even if they
        /// are the same as the defaults for that symbol.  For example, a private field in C# does
        /// not need its accessibility specified as it will be private by default.  However, if this
        /// option is set to true 'private' will still be generated.
        /// </summary>
        public bool GenerateDefaultAccessibility { get; }

        /// <summary>
        /// True if the code generator should generate empty bodies for methods along with the
        /// method declaration.  If false, only method declarations will be generated.
        /// </summary>
        public bool GenerateMethodBodies { get; }

        /// <summary>
        /// True if the code generator should generate documentation comments where available
        /// </summary>
        public bool GenerateDocumentationComments { get; }

        /// <summary>
        /// True if the code generator should automatically attempt to choose the appropriate location
        /// to insert members.  If false and a generation location is not specified by AfterThisLocation,
        /// or BeforeThisLocation, members will be inserted at the end of the destination definition.
        /// </summary>
        public bool AutoInsertionLocation { get; }

        /// <summary>
        /// If <see cref="AutoInsertionLocation"/> is <see langword="false"/>, determines if members will be
        /// sorted before being added to the end of the list of members.
        /// </summary>
        public bool SortMembers { get; }

        /// <summary>
        /// True if the code generator should attempt to reuse the syntax of the constituent entities, such as members, access modifier tokens, etc. while attempting to generate code.
        /// If any of the member symbols have zero declaring syntax references (non-source symbols) OR two or more declaring syntax references (partial definitions), then syntax is not reused.
        /// If false, then the code generator will always synthesize a new syntax node and ignore the declaring syntax references.
        /// </summary>
        public bool ReuseSyntax { get; }

        public ParseOptions ParseOptions { get; }

        public CodeGenerationOptions(
            Location contextLocation = null,
            Location afterThisLocation = null,
            Location beforeThisLocation = null,
            bool addImports = true,
            bool placeSystemNamespaceFirst = true,
            IEnumerable<INamespaceSymbol> additionalImports = null,
            bool generateMembers = true,
            bool mergeNestedNamespaces = true,
            bool mergeAttributes = true,
            bool generateDefaultAccessibility = true,
            bool generateMethodBodies = true,
            bool generateDocumentationComments = false,
            bool autoInsertionLocation = true,
            bool sortMembers = true,
            bool reuseSyntax = false,
            ParseOptions parseOptions = null)
        {
            CheckLocation(contextLocation, nameof(contextLocation));
            CheckLocation(afterThisLocation, nameof(afterThisLocation));
            CheckLocation(beforeThisLocation, nameof(beforeThisLocation));

            this.ContextLocation = contextLocation;
            this.AfterThisLocation = afterThisLocation;
            this.BeforeThisLocation = beforeThisLocation;
            this.AddImports = addImports;
            this.PlaceSystemNamespaceFirst = placeSystemNamespaceFirst;
            this.AdditionalImports = additionalImports ?? SpecializedCollections.EmptyEnumerable<INamespaceSymbol>();
            this.GenerateMembers = generateMembers;
            this.MergeNestedNamespaces = mergeNestedNamespaces;
            this.MergeAttributes = mergeAttributes;
            this.GenerateDefaultAccessibility = generateDefaultAccessibility;
            this.GenerateMethodBodies = generateMethodBodies;
            this.GenerateDocumentationComments = generateDocumentationComments;
            this.AutoInsertionLocation = autoInsertionLocation;
            this.SortMembers = sortMembers;
            this.ReuseSyntax = reuseSyntax;

            this.ParseOptions = parseOptions ?? this.BestLocation?.SourceTree.Options;
        }

        private void CheckLocation(Location location, string name)
        {
            if (location is { IsInSource: false })
            {
                throw new ArgumentException(WorkspacesResources.Location_must_be_null_or_from_source, name);
            }
        }

        internal Location BestLocation
        {
            get
            {
                return this.AfterThisLocation != null
                    ? this.AfterThisLocation
                    : this.BeforeThisLocation != null
                        ? this.BeforeThisLocation
                        : this.ContextLocation;
            }
        }

        public CodeGenerationOptions With(
            Optional<Location> contextLocation = default,
            Optional<Location> afterThisLocation = default,
            Optional<Location> beforeThisLocation = default,
            Optional<bool> addImports = default,
            Optional<bool> placeSystemNamespaceFirst = default,
            Optional<IEnumerable<INamespaceSymbol>> additionalImports = default,
            Optional<bool> generateMembers = default,
            Optional<bool> mergeNestedNamespaces = default,
            Optional<bool> mergeAttributes = default,
            Optional<bool> generateDefaultAccessibility = default,
            Optional<bool> generateMethodBodies = default,
            Optional<bool> generateDocumentationComments = default,
            Optional<bool> autoInsertionLocation = default,
            Optional<bool> sortMembers = default,
            Optional<bool> reuseSyntax = default,
            Optional<ParseOptions> parseOptions = default)
        {
            var newContextLocation = contextLocation.HasValue ? contextLocation.Value : this.ContextLocation;
            var newAfterThisLocation = afterThisLocation.HasValue ? afterThisLocation.Value : this.AfterThisLocation;
            var newBeforeThisLocation = beforeThisLocation.HasValue ? beforeThisLocation.Value : this.BeforeThisLocation;
            var newAddImports = addImports.HasValue ? addImports.Value : this.AddImports;
            var newPlaceSystemNamespaceFirst = placeSystemNamespaceFirst.HasValue ? placeSystemNamespaceFirst.Value : this.PlaceSystemNamespaceFirst;
            var newAdditionalImports = additionalImports.HasValue ? additionalImports.Value : this.AdditionalImports;
            var newGenerateMembers = generateMembers.HasValue ? generateMembers.Value : this.GenerateMembers;
            var newMergeNestedNamespaces = mergeNestedNamespaces.HasValue ? mergeNestedNamespaces.Value : this.MergeNestedNamespaces;
            var newMergeAttributes = mergeAttributes.HasValue ? mergeAttributes.Value : this.MergeAttributes;
            var newGenerateDefaultAccessibility = generateDefaultAccessibility.HasValue ? generateDefaultAccessibility.Value : this.GenerateDefaultAccessibility;
            var newGenerateMethodBodies = generateMethodBodies.HasValue ? generateMethodBodies.Value : this.GenerateMethodBodies;
            var newGenerateDocumentationComments = generateDocumentationComments.HasValue ? generateDocumentationComments.Value : this.GenerateDocumentationComments;
            var newAutoInsertionLocation = autoInsertionLocation.HasValue ? autoInsertionLocation.Value : this.AutoInsertionLocation;
            var newSortMembers = sortMembers.HasValue ? sortMembers.Value : this.SortMembers;
            var newReuseSyntax = reuseSyntax.HasValue ? reuseSyntax.Value : this.ReuseSyntax;
            var newParseOptions = parseOptions.HasValue ? parseOptions.Value : this.ParseOptions;

            return new CodeGenerationOptions(
                newContextLocation,
                newAfterThisLocation,
                newBeforeThisLocation,
                newAddImports,
                newPlaceSystemNamespaceFirst,
                newAdditionalImports,
                newGenerateMembers,
                newMergeNestedNamespaces,
                newMergeAttributes,
                newGenerateDefaultAccessibility,
                newGenerateMethodBodies,
                newGenerateDocumentationComments,
                newAutoInsertionLocation,
                newSortMembers,
                newReuseSyntax,
                newParseOptions);
        }
    }
}
