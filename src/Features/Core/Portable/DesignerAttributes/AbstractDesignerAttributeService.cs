// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        protected abstract SyntaxNode GetFirstTopLevelClass(SyntaxNode root);
        protected abstract bool HasAttributesOrBaseTypeOrIsPartial(SyntaxNode typeNode);

        public async Task<DesignerAttributeResult> ScanDesignerAttributesAsync(Document document, CancellationToken cancellationToken)
        {
            // make sure given input is right one
            Contract.ThrowIfFalse(_workspace == document.Project.Solution.Workspace);

            // run designer attributes scanner on remote host
            // we only run closed files to make open document to have better responsiveness. 
            // also we cache everything related to open files anyway, no saving by running
            // them in remote host
            if (!document.IsOpen())
            {
                var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.TryRunRemoteAsync<DesignerAttributeResult>(
                        WellKnownServiceHubServices.CodeAnalysisService,
                        nameof(IRemoteDesignerAttributeService.ScanDesignerAttributesAsync),
                        document.Project.Solution,
                        new[] { document.Id },
                        callbackTarget: null,
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue)
                    {
                        return result.Value;
                    }
                }
            }

            return await ScanDesignerAttributesInCurrentProcessAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DesignerAttributeResult> ScanDesignerAttributesInCurrentProcessAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            var documentHasError = false;

            // get type defined in current tree
            var typeNode = this.GetFirstTopLevelClass(root);
            if (HasAttributesOrBaseTypeOrIsPartial(typeNode))
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var designerAttribute = compilation.DesignerCategoryAttributeType();
                if (designerAttribute == null)
                {
                    // The DesignerCategoryAttribute doesn't exist. either not applicable or
                    // no idea on design attribute status, just leave things as it is.
                    return new DesignerAttributeResult(
                        designerAttributeArgument: null, containsErrors: false, applicable: false);
                }

                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                var definedType = (INamedTypeSymbol)model.GetDeclaredSymbol(typeNode, cancellationToken);

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
                        var designerAttributeArgument = GetArgumentString(attribute.ConstructorArguments[0]);
                        return new DesignerAttributeResult(designerAttributeArgument, documentHasError, applicable: true);
                    }
                }
            }

            return new DesignerAttributeResult(
                designerAttributeArgument: null, documentHasError, applicable: true);
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
