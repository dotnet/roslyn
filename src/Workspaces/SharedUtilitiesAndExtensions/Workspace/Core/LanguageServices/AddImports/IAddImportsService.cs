// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using OptionSet = Microsoft.CodeAnalysis.Options.OptionSet;
#endif

namespace Microsoft.CodeAnalysis.AddImport
{
    [DataContract]
    internal record struct AddImportPlacementOptions(
        [property: DataMember(Order = 0)] bool PlaceSystemNamespaceFirst,
        [property: DataMember(Order = 1)] bool PlaceImportsInsideNamespaces,
        [property: DataMember(Order = 2)] bool AllowInHiddenRegions)
    {
        public static async Task<AddImportPlacementOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var options = document.Project.AnalyzerOptions.GetAnalyzerOptionSet(await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
#else
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
#endif
            return FromDocument(document, options);
        }

        private static bool CanAddImportsInHiddenRegions(Document document)
        {
#if CODE_STYLE
            return false;
#else
            // Normally we don't allow generation into a hidden region in the file.  However, if we have a
            // modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
            // our edit to their domain appropriate.
            var spanMapper = document.Services.GetService<ISpanMappingService>();
            return spanMapper != null && spanMapper.SupportsMappingImportDirectives;
#endif
        }

        public static AddImportPlacementOptions FromDocument(Document document, OptionSet documentOptions)
        {
            var service = document.GetRequiredLanguageService<IAddImportsService>();
            return new(
                PlaceSystemNamespaceFirst: documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language),
                PlaceImportsInsideNamespaces: service.PlaceImportsInsideNamespaces(documentOptions),
                AllowInHiddenRegions: CanAddImportsInHiddenRegions(document));
        }
    }

    internal interface IAddImportsService : ILanguageService
    {
        bool PlaceImportsInsideNamespaces(OptionSet optionSet);

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
