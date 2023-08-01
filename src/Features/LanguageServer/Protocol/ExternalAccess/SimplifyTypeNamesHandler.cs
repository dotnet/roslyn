// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Simplify;

[ExportCSharpVisualBasicStatelessLspService(typeof(SimplifyTypeNamesHandler)), Shared]
[Method(SimplifyTypeNamesMethodName)]
internal class SimplifyTypeNamesHandler : ILspServiceDocumentRequestHandler<SimplifyTypeNamesParams, string[]?>
{
    private const string SimplifyTypeNamesMethodName = "textDocument/simplifyTypeNames";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SimplifyTypeNamesHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(SimplifyTypeNamesParams request) => request.TextDocument;

    public async Task<string[]?> HandleRequestAsync(
        SimplifyTypeNamesParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = request.PlacementTextDocument is null
            ? context.Document
            : context.Solution!.GetDocument(request.PlacementTextDocument);

        if (document is null)
            return null;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return null;

        var fullyQualifiedTypeNames = request.FullyQualifiedTypeNames;
        var result = new string[fullyQualifiedTypeNames.Length];
        for (var i = 0; i < fullyQualifiedTypeNames.Length; i++)
        {
            var typeName = fullyQualifiedTypeNames[i];
            var simplifiedName = typeName[(typeName.LastIndexOf('.') + 1)..];
            if (simplifiedName == typeName)
            {
                result[i] = typeName;
                continue;
            }

            var typeNameSyntax = SyntaxFactory.IdentifierName(simplifiedName);
            var typeInfo = semanticModel.GetSpeculativeTypeInfo(
                request.AbsoluteIndex,
                typeNameSyntax,
                SpeculativeBindingOption.BindAsTypeOrNamespace);

            // If the necessary using statements are in scope,
            // the simplified type name will be recognized as a class, otherwise it won't.
            result[i] = typeInfo.Type?.TypeKind == TypeKind.Class && typeInfo.Type.ToDisplayString() == typeName
                ? simplifiedName
                : typeName;
        }

        return result;
    }
}
