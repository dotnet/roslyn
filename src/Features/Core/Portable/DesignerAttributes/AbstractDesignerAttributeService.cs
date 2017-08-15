// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Versions;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal abstract class AbstractDesignerAttributeService : IDesignerAttributeService
    {
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
                            return new DesignerAttributeDocumentData(document.FilePath, designerAttributeArgument);
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
                            continue;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // if it has designer attribute, set it
                        var attribute = type.GetAttributes().Where(d => designerAttribute.Equals(d.AttributeClass)).FirstOrDefault();
                        if (attribute != null && attribute.ConstructorArguments.Length == 1)
                        {
                            designerAttributeArgument = GetArgumentString(attribute.ConstructorArguments[0]);
                            return new DesignerAttributeDocumentData(document.FilePath, designerAttributeArgument);
                        }
                    }
                }

                // check only first type
                if (ProcessOnlyFirstTypeDefined())
                {
                    break;
                }
            }

            return new DesignerAttributeDocumentData(document.FilePath, designerAttributeArgument);
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
            var designerAttributeData = await DesignerAttributeProjectData.ReadAsync(
                project, cancellationToken).ConfigureAwait(false);

            // If we have no persisted data, or the persisted data is for a previous version of 
            // the project, then compute the results for the current project snapshot.
            if (designerAttributeData == null ||
                !VersionStamp.CanReusePersistedVersion(semanticVersion, designerAttributeData.SemanticVersion))
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

            var tasks = project.Documents.Select(
                d => service.ScanDesignerAttributesAsync(d, cancellationToken)).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var builder = ImmutableDictionary.CreateBuilder<string, DesignerAttributeDocumentData>();
            foreach (var task in tasks)
            {
                var result = await task.ConfigureAwait(false);
                builder[result.FilePath] = result;
            }

            var data = new DesignerAttributeProjectData(semanticVersion, builder.ToImmutable());
            await data.PersistAsync(project, cancellationToken).ConfigureAwait(false);
            return data;
        }
    }
}
