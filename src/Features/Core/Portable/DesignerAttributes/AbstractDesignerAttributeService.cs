// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal abstract class AbstractDesignerAttributeService : IDesignerAttributeService
    {
        private const string StreamName = "<DesignerAttribute>";
        private const string FormatVersion = "3";

        protected abstract bool ProcessOnlyFirstTypeDefined();
        protected abstract IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(SyntaxNode root);
        protected abstract bool HasAttributesOrBaseTypeOrIsPartial(SyntaxNode typeNode);

        public async Task<DesignerAttributeDocumentData> ScanDesignerAttributesAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            // Delay getting any of these until we need them, but hold on to them once we have them.
            Compilation compilation = null;
            INamedTypeSymbol designerAttribute = null;
            SemanticModel model = null;

            string designerAttributeArgument = null;
            var documentHasError = false;

            // get type defined in current tree
            foreach (var typeNode in GetAllTopLevelTypeDefined(root))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (HasAttributesOrBaseTypeOrIsPartial(typeNode))
                {
                    if (designerAttribute == null)
                    {
                        compilation = compilation ?? await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                        designerAttribute = compilation.DesignerCategoryAttributeType();
                        if (designerAttribute == null)
                        {
                            // The DesignerCategoryAttribute doesn't exist. either not applicable or
                            // no idea on design attribute status, just leave things as it is.
                            return new DesignerAttributeDocumentData(document.FilePath, designerAttributeArgument, documentHasError, notApplicable: true);
                        }
                    }

                    if (model == null)
                    {
                        model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    }

                    var definedType = model.GetDeclaredSymbol(typeNode, cancellationToken) as INamedTypeSymbol;
                    if (definedType == null)
                    {
                        continue;
                    }

                    // walk up type chain
                    foreach (var type in definedType.GetBaseTypesAndThis())
                    {
                        if (type.IsErrorType())
                        {
                            documentHasError = true;
                            continue;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // if it has designer attribute, set it
                        var attribute = type.GetAttributes().Where(d => designerAttribute.Equals(d.AttributeClass)).FirstOrDefault();
                        if (attribute != null && attribute.ConstructorArguments.Length == 1)
                        {
                            designerAttributeArgument = GetArgumentString(attribute.ConstructorArguments[0]);
                            return new DesignerAttributeDocumentData(document.FilePath, designerAttributeArgument, documentHasError, notApplicable: false);
                        }
                    }
                }

                // check only first type
                if (ProcessOnlyFirstTypeDefined())
                {
                    break;
                }
            }

            return new DesignerAttributeDocumentData(document.FilePath, designerAttributeArgument, documentHasError, notApplicable: false);
        }

        private static string GetArgumentString(TypedConstant argument)
        {
            if (argument.Type == null ||
                argument.Type.SpecialType != SpecialType.System_String ||
                argument.IsNull)
            {
                return null;
            }

            return ((string)argument.Value).Trim();
        }

        internal static async Task<ImmutableDictionary<string, DesignerAttributeDocumentData>> TryAnalyzeProjectInCurrentProcessAsync(
            Project project, CancellationToken cancellationToken)
        {
            var projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
            var semanticVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            // Get whatever data we've current persisted.
            var designerAttributeData = await ReadExistingDataAsync(project, cancellationToken).ConfigureAwait(false);

            // If we have no persisted data, or the persisted data is for a previous version of 
            // the project, then compute the results for the current project snapshot.
            if (designerAttributeData == null ||
                !project.CanReusePersistedDependentSemanticVersion(projectVersion, semanticVersion, designerAttributeData.SemanticVersion))
            {
                designerAttributeData = await ComputeAndPersistDesignerAttributeProjectDataAsync(
                    project, semanticVersion, cancellationToken).ConfigureAwait(false);
            }

            return designerAttributeData.PathToDocumentData;
        }

        private static async Task<DesignerAttributeProjectData> ComputeAndPersistDesignerAttributeProjectDataAsync(
            Project project, VersionStamp semanticVersion, CancellationToken cancellationToken)
        {
            var service = project.LanguageServices.GetService<IDesignerAttributeService>();

            var builder = ImmutableDictionary.CreateBuilder<string, DesignerAttributeDocumentData>();
            foreach (var document in project.Documents)
            {
                var result = await service.ScanDesignerAttributesAsync(document, cancellationToken).ConfigureAwait(false);
                builder[document.FilePath] = result;
            }

            var data = new DesignerAttributeProjectData(semanticVersion, builder.ToImmutable());
            PersistProjectData(project, data, cancellationToken);
            return data;
        }

        private static async Task<DesignerAttributeProjectData> ReadExistingDataAsync(
            Project project, CancellationToken cancellationToken)
        {
            try
            {
                var solution = project.Solution;
                var workspace = project.Solution.Workspace;

                var storageService = workspace.Services.GetService<IPersistentStorageService>();
                using (var persistenceService = storageService.GetStorage(solution))
                using (var stream = await persistenceService.ReadStreamAsync(project, StreamName, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream, cancellationToken))
                {
                    if (reader != null)
                    {
                        var version = reader.ReadString();
                        if (version == FormatVersion)
                        {
                            var semanticVersion = VersionStamp.ReadFrom(reader);

                            var resultCount = reader.ReadInt32();
                            var builder = ImmutableDictionary.CreateBuilder<string, DesignerAttributeDocumentData>();

                            for (var i = 0; i < resultCount; i++)
                            {
                                var filePath = reader.ReadString();
                                var attribute = reader.ReadString();
                                var containsErrors = reader.ReadBoolean();
                                var notApplicable = reader.ReadBoolean();

                                builder[filePath] = new DesignerAttributeDocumentData(filePath, attribute, containsErrors, notApplicable);
                            }

                            return new DesignerAttributeProjectData(semanticVersion, builder.ToImmutable());
                        }
                    }
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return null;
        }

        private static void PersistProjectData(
            Project project, DesignerAttributeProjectData data, CancellationToken cancellationToken)
        {
            try
            {
                var solution = project.Solution;
                var workspace = project.Solution.Workspace;

                var storageService = workspace.Services.GetService<IPersistentStorageService>();
                using (var persistenceService = storageService.GetStorage(solution))
                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    writer.WriteString(FormatVersion);
                    data.SemanticVersion.WriteTo(writer);

                    writer.WriteInt32(data.PathToDocumentData.Count);

                    foreach (var kvp in data.PathToDocumentData)
                    {
                        var result = kvp.Value;
                        writer.WriteString(result.FilePath);
                        writer.WriteString(result.DesignerAttributeArgument);
                        writer.WriteBoolean(result.ContainsErrors);
                        writer.WriteBoolean(result.NotApplicable);
                    }
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }
        }
    }
}