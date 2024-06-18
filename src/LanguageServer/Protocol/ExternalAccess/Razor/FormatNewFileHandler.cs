// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[ExportCSharpVisualBasicStatelessLspService(typeof(FormatNewFileHandler)), Shared]
[Method(FormatNewFileMethodName)]
internal sealed class FormatNewFileHandler : ILspServiceRequestHandler<FormatNewFileParams, string?>
{
    public const string FormatNewFileMethodName = "roslyn/formatNewFile";
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FormatNewFileHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<string?> HandleRequestAsync(FormatNewFileParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var project = context.Solution?.GetProject(request.Project);

        if (project is null)
        {
            return null;
        }

        // Create a document in-memory to represent the file Razor wants to add
        var filePath = ProtocolConversions.GetDocumentFilePathFromUri(request.Document.Uri);
        var source = SourceText.From(request.Contents);
        var fileLoader = new SourceTextLoader(source, filePath);
        var documentId = DocumentId.CreateNewId(project.Id);
        var solution = project.Solution.AddDocument(
            DocumentInfo.Create(
                documentId,
                name: filePath,
                loader: fileLoader,
                filePath: filePath));

        var document = solution.GetRequiredDocument(documentId);

        // Run the new document formatting service, to make sure the right namespace type is used, among other things
        var formattingService = document.GetLanguageService<INewDocumentFormattingService>();
        if (formattingService is not null)
        {
            var hintDocument = project.Documents.FirstOrDefault();
            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(_globalOptions, cancellationToken).ConfigureAwait(false);
            document = await formattingService.FormatNewDocumentAsync(document, hintDocument, cleanupOptions, cancellationToken).ConfigureAwait(false);
        }

        // Unlike normal new file formatting, Razor also wants to remove unnecessary usings
        var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var removeImportsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
        if (removeImportsService is not null)
        {
            document = await removeImportsService.RemoveUnnecessaryImportsAsync(document, syntaxFormattingOptions, cancellationToken).ConfigureAwait(false);
        }

        // Now format the document so indentation etc. is correct
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        root = Formatter.Format(root, solution.Services, syntaxFormattingOptions, cancellationToken);

        return root.ToFullString();
    }
}
