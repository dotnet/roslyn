// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal abstract class AbstractDesignerAttributeService : IDesignerAttributeService
    {
        // we hold onto workspace to make sure given input (Document) belong to right workspace.
        // since remote host is from workspace service, different workspace can have different expectation
        // on remote host, so we need to make sure given input always belong to right workspace where
        // the session belong to.
        private readonly Workspace _workspace;

        protected AbstractDesignerAttributeService(Workspace workspace)
        {
            _workspace = workspace;
        }

        protected abstract bool ProcessOnlyFirstTypeDefined();
        protected abstract IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(SyntaxNode root);
        protected abstract bool HasAttributesOrBaseTypeOrIsPartial(SyntaxNode typeNode);

        public async Task<DesignerAttributeResult> ScanDesignerAttributesAsync(Document document, CancellationToken cancellationToken)
        {
            // make sure given input is right one
            Contract.ThrowIfFalse(_workspace == document.Project.Solution.Workspace);

            // same service run in both inproc and remote host, but remote host will not have RemoteHostClient service, 
            // so inproc one will always run
            var client = await document.Project.Solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client != null && !document.IsOpen())
            {
                // run designer attributes scanner on remote host
                // we only run closed files to make open document to have better responsiveness. 
                // also we cache everything related to open files anyway, no saving by running
                // them in remote host
                return await ScanDesignerAttributesInRemoteHostAsync(client, document, cancellationToken).ConfigureAwait(false);
            }

            return await ScanDesignerAttributesInCurrentProcessAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DesignerAttributeResult> ScanDesignerAttributesInRemoteHostAsync(RemoteHostClient client, Document document, CancellationToken cancellationToken)
        {
            return await client.TryRunCodeAnalysisRemoteAsync<DesignerAttributeResult>(
                document.Project.Solution,
                nameof(IRemoteDesignerAttributeService.ScanDesignerAttributesAsync),
                document.Id,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<DesignerAttributeResult> ScanDesignerAttributesInCurrentProcessAsync(Document document, CancellationToken cancellationToken)
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
                        compilation ??= await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                        designerAttribute = compilation.DesignerCategoryAttributeType();
                        if (designerAttribute == null)
                        {
                            // The DesignerCategoryAttribute doesn't exist. either not applicable or
                            // no idea on design attribute status, just leave things as it is.
                            return new DesignerAttributeResult(designerAttributeArgument, documentHasError, applicable: false);
                        }
                    }

                    if (model == null)
                    {
                        model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    }

                    if (!(model.GetDeclaredSymbol(typeNode, cancellationToken) is INamedTypeSymbol definedType))
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
                            return new DesignerAttributeResult(designerAttributeArgument, documentHasError, applicable: true);
                        }
                    }
                }

                // check only first type
                if (ProcessOnlyFirstTypeDefined())
                {
                    break;
                }
            }

            return new DesignerAttributeResult(designerAttributeArgument, documentHasError, applicable: true);
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
    }
}
