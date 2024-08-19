// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[ExportCSharpVisualBasicStatelessLspService(typeof(RazorComponentInfoHandler)), Shared]
[Method(RazorComponentInfoName)]
internal sealed class RazorComponentInfoHandler : ILspServiceRequestHandler<RazorComponentInfoParams, RazorComponentInfo?>
{
    public const string RazorComponentInfoName = "roslyn/razorComponentInfo";
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorComponentInfoHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<RazorComponentInfo?> HandleRequestAsync(RazorComponentInfoParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var project = context.Solution?.GetProject(request.Project);

        if (project is null)
        {
            return null;
        }

        // Create a document in-memory to represent the file Razor wants to add
        //var newFilePath = ProtocolConversions.GetDocumentFilePathFromUri(request.NewDocument.Uri);
        //var newSource = SourceText.From(request.NewContents);
        //var newFileLoader = new SourceTextLoader(newSource, newFilePath);
        //var newDocumentId = DocumentId.CreateNewId(project.Id);
        //var newSolution = project.Solution.AddDocument(
        //    DocumentInfo.Create(
        //        newDocumentId,
        //        name: newFilePath,
        //        loader: newFileLoader,
        //        filePath: newFilePath));

        //var newDocument = newSolution.GetRequiredDocument(newDocumentId);

        //var formattingService = newDocument.GetLanguageService<INewDocumentFormattingService>();
        //if (formattingService is not null)
        //{
        //    var hintDocument = project.Documents.FirstOrDefault();
        //    var cleanupOptions = await newDocument.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);
        //    newDocument = await formattingService.FormatNewDocumentAsync(newDocument, hintDocument, cleanupOptions, cancellationToken).ConfigureAwait(false);
        //}

        var document = context.Solution.GetDocument(request.Document);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxTree = semanticModel.SyntaxTree;
        var root = (CompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken);


        var (methods, fields) = GetInfoInsideRazorDocument(root, semanticModel, cancellationToken);

        var result = new RazorComponentInfo
        {
            Methods = methods,
            Fields = fields
        };

        return result;
    }

    private static (List<MethodInsideRazorElementInfo>, List<SymbolInsideRazorElementInfo>) GetInfoInsideRazorDocument(CompilationUnitSyntax root, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var invocationExpressions = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
        var identifierNames = root.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();
        
        List<MethodInsideRazorElementInfo> methods = [];
        List<SymbolInsideRazorElementInfo> fields = [];
        
        foreach (var invocation in invocationExpressions)
        {
            var invocationOperation = semanticModel.GetOperation(invocation, cancellationToken) as IInvocationOperation;
            var invocationDataFlow = semanticModel.AnalyzeDataFlow(invocation);
            if (invocationOperation is null)
            {
                continue;
            }


            var targetMethod = invocationOperation.TargetMethod;
            if (targetMethod is null)
            {
                continue;
            }

            var operationReturnType = invocationOperation.Type;
            if (operationReturnType is null)
            {
                continue;
            }

            var parameterTypes = targetMethod.GetParameters().Select(parameter => parameter.Type.ToDisplayString()).ToList();
            if (parameterTypes is null)
            {
                continue;
            }

            var methodInfo = new MethodInsideRazorElementInfo
            {
                Name = targetMethod.Name,
                ReturnType = operationReturnType.ToNameDisplayString(),
                ParameterTypes = parameterTypes
            };

            methods.Add(methodInfo);
        }

        foreach (var identifier in identifierNames)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(identifier, cancellationToken);
            if (symbolInfo.Symbol is IFieldSymbol or IPropertySymbol)
            {
                var field = new SymbolInsideRazorElementInfo
                {
                    Name = symbolInfo.Symbol.Name,
                    Type = symbolInfo.Symbol.GetType().ToString()
                };

                fields.Add(field);
            }
        }

        return (methods, fields);
    }
}
