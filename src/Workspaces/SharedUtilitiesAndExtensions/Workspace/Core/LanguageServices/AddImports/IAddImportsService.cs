// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    [DataContract]
    internal record struct AddImportPlacementOptions(
        [property: DataMember(Order = 0)] bool PlaceSystemNamespaceFirst = true,
        [property: DataMember(Order = 1)] bool PlaceImportsInsideNamespaces = false,
        [property: DataMember(Order = 2)] bool AllowInHiddenRegions = false)
    {
        public AddImportPlacementOptions()
            : this(PlaceSystemNamespaceFirst: true)
        {
        }

        public static readonly AddImportPlacementOptions Default = new();

        internal static AddImportPlacementOptions Create(AnalyzerConfigOptions configOptions, IAddImportsService addImportsService, bool allowInHiddenRegions, AddImportPlacementOptions? fallbackOptions)
        {
            fallbackOptions ??= Default;

            return new(
                PlaceSystemNamespaceFirst: configOptions.GetEditorConfigOption(GenerationOptions.PlaceSystemNamespaceFirst, fallbackOptions.Value.PlaceSystemNamespaceFirst),
                PlaceImportsInsideNamespaces: addImportsService.PlaceImportsInsideNamespaces(configOptions, fallbackOptions.Value.PlaceImportsInsideNamespaces),
                AllowInHiddenRegions: allowInHiddenRegions);
        }
    }

    internal interface AddImportPlacementOptionsProvider
#if !CODE_STYLE
        : OptionsProvider<AddImportPlacementOptions>
#endif
    {
    }

#if !CODE_STYLE
    internal static class AddImportPlacementOptionsProviders
    {
        public static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, AddImportPlacementOptions? fallbackOptions, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var services = document.Project.Solution.Workspace.Services;
            var configOptions = documentOptions.AsAnalyzerConfigOptions(services.GetRequiredService<Options.IOptionService>(), document.Project.Language);
            var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();

            // Normally we don't allow generation into a hidden region in the file.  However, if we have a
            // modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
            // our edit to their domain appropriate.
            var spanMapper = document.Services.GetService<ISpanMappingService>();
            var allowInHiddenRegions = spanMapper != null && spanMapper.SupportsMappingImportDirectives;

            return AddImportPlacementOptions.Create(configOptions, addImportsService, allowInHiddenRegions, fallbackOptions);
        }

        public static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, AddImportPlacementOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
            => await GetAddImportPlacementOptionsAsync(document, await fallbackOptionsProvider.GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }
#endif

    internal interface IAddImportsService : ILanguageService
    {
        bool PlaceImportsInsideNamespaces(AnalyzerConfigOptions configOptions, bool fallbackValue);

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
