// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    [DataContract]
    internal record struct AddImportPlacementOptions(
        [property: DataMember(Order = 0)] bool PlaceSystemNamespaceFirst,
        [property: DataMember(Order = 1)] bool PlaceImportsInsideNamespaces,
        [property: DataMember(Order = 2)] bool AllowInHiddenRegions)
    {
#if !CODE_STYLE
        public static async Task<AddImportPlacementOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
            => FromDocument(document, await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false));

        public static AddImportPlacementOptions FromDocument(Document document, Options.OptionSet documentOptions)
        {
            var service = document.GetRequiredLanguageService<IAddImportsService>();

            return new(
                PlaceSystemNamespaceFirst: documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language),
                PlaceImportsInsideNamespaces: service.PlaceImportsInsideNamespaces(documentOptions),
                AllowInHiddenRegions: CanAddImportsInHiddenRegions(document));
        }

        private static bool CanAddImportsInHiddenRegions(Document document)
        {
            // Normally we don't allow generation into a hidden region in the file.  However, if we have a
            // modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
            // our edit to their domain appropriate.
            var spanMapper = document.Services.GetService<ISpanMappingService>();
            return spanMapper != null && spanMapper.SupportsMappingImportDirectives;
        }
#endif
    }

    internal interface IAddImportsService : ILanguageService
    {
#if !CODE_STYLE
        bool PlaceImportsInsideNamespaces(Options.OptionSet optionSet);
#endif

        /// <summary>
        /// Returns true if the tree already has an existing import syntactically equivalent to
        /// <paramref name="import"/> in scope at <paramref name="contextLocation"/>.  This includes
        /// global imports for VB.
        /// </summary>
        bool HasExistingImport(Compilation compilation, SyntaxNode root, SyntaxNode? contextLocation, SyntaxNode import, SyntaxGenerator generator);

        /// <summary>
        /// Given a context location in a provided syntax tree, returns the appropriate container
        /// that <paramref name="import"/> should be added to.
        /// </summary>
        SyntaxNode GetImportContainer(SyntaxNode root, SyntaxNode? contextLocation, SyntaxNode import, AddImportPlacementOptions options);

        SyntaxNode AddImports(
            Compilation compilation, SyntaxNode root, SyntaxNode? contextLocation,
            IEnumerable<SyntaxNode> newImports, SyntaxGenerator generator, AddImportPlacementOptions options, CancellationToken cancellationToken);
    }

    internal static class IAddImportServiceExtensions
    {
        public static SyntaxNode AddImport(
            this IAddImportsService service, Compilation compilation, SyntaxNode root,
            SyntaxNode contextLocation, SyntaxNode newImport, SyntaxGenerator generator, AddImportPlacementOptions options,
            CancellationToken cancellationToken)
        {
            return service.AddImports(compilation, root, contextLocation,
                SpecializedCollections.SingletonEnumerable(newImport), generator, options, cancellationToken);
        }
    }
}
