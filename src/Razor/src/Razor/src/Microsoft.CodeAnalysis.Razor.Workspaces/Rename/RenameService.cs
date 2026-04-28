// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Rename;

internal class RenameService(
    IRazorComponentSearchEngine componentSearchEngine,
    IFileSystem fileSystem,
    LanguageServerFeatureOptions languageServerFeatureOptions) : IRenameService
{
    private readonly IRazorComponentSearchEngine _componentSearchEngine = componentSearchEngine;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;

    public async Task<RenameResult> TryGetRazorRenameEditsAsync(
        DocumentContext documentContext,
        DocumentPositionInfo positionInfo,
        string newName,
        ISolutionQueryOperations solutionQueryOperations,
        CancellationToken cancellationToken)
    {
        // We only support renaming of .razor components, not .cshtml tag helpers
        if (!documentContext.Snapshot.FileKind.IsComponent())
        {
            return new(Edit: null);
        }

        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetOriginTagHelpers(codeDocument, positionInfo.HostDocumentIndex, out var originTagHelpers))
        {
            return new(Edit: null);
        }

        var originComponentDocumentSnapshot = await _componentSearchEngine
            .TryLocateComponentAsync(originTagHelpers.Primary, solutionQueryOperations, cancellationToken)
            .ConfigureAwait(false);
        if (originComponentDocumentSnapshot is null)
        {
            return new(Edit: null);
        }

        var originComponentDocumentFilePath = originComponentDocumentSnapshot.FilePath;
        var newPath = MakeNewPath(originComponentDocumentFilePath, newName);
        if (_fileSystem.FileExists(newPath))
        {
            // We found a tag, but the new name would cause a conflict, so we can't proceed with the rename,
            // even if C# might have worked.
            return new(Edit: null, FallbackToCSharp: false);
        }

        using var documentChanges = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();

        var fileRename = GetRenameFileEdit(originComponentDocumentFilePath, newPath);
        documentChanges.Add(fileRename);

        AddAdditionalFileRenames(ref documentChanges.AsRef(), originComponentDocumentFilePath, newPath);

        foreach (var documentChange in documentChanges)
        {
            if (documentChange.TryGetFirst(out var textDocumentEdit) &&
                textDocumentEdit.TextDocument.DocumentUri == fileRename.OldDocumentUri)
            {
                textDocumentEdit.TextDocument.DocumentUri = fileRename.NewDocumentUri;
            }
        }

        return new(Edit: new()
        {
            DocumentChanges = documentChanges.ToArrayAndClear()
        });
    }

    public bool TryGetRazorFileRenameEdit(
        DocumentContext documentContext,
        string newName,
        [NotNullWhen(true)] out WorkspaceEdit? workspaceEdit)
    {
        var oldPath = documentContext.Snapshot.FilePath;
        var newPath = MakeNewPath(oldPath, newName);

        using var documentChanges = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();

        AddAdditionalFileRenames(ref documentChanges.AsRef(), oldPath, newPath);

        if (documentChanges.Count == 0)
        {
            workspaceEdit = null;
            return false;
        }

        workspaceEdit = new()
        {
            DocumentChanges = documentChanges.ToArrayAndClear()
        };
        return true;
    }

    private void AddAdditionalFileRenames(
        ref PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>> documentChanges,
        string oldFilePath, string newFilePath)
    {
        TryAdd(".cs", ref documentChanges);
        TryAdd(".css", ref documentChanges);

        void TryAdd(
            string extension,
            ref PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>> documentChanges)
        {
            var changedPath = oldFilePath + extension;

            if (_fileSystem.FileExists(changedPath))
            {
                documentChanges.Add(GetRenameFileEdit(changedPath, newFilePath + extension));
            }
        }
    }

    private RenameFile GetRenameFileEdit(string oldFilePath, string newFilePath)
        => new()
        {
            OldDocumentUri = new(LspFactory.CreateFilePathUri(oldFilePath, _languageServerFeatureOptions)),
            NewDocumentUri = new(LspFactory.CreateFilePathUri(newFilePath, _languageServerFeatureOptions)),
        };

    private static string MakeNewPath(string originalPath, string newName)
    {
        var newFileName = $"{newName}{Path.GetExtension(originalPath)}";
        var directoryName = Path.GetDirectoryName(originalPath).AssumeNotNull();
        return Path.Combine(directoryName, newFileName);
    }

    private readonly record struct OriginTagHelpers(TagHelperDescriptor Primary, TagHelperDescriptor? Associated);

    private static bool TryGetOriginTagHelpers(RazorCodeDocument codeDocument, int absoluteIndex, out OriginTagHelpers originTagHelpers)
    {
        var owner = codeDocument.GetRequiredSyntaxRoot().FindInnermostNode(absoluteIndex);
        if (owner is null)
        {
            Debug.Fail("Owner should never be null.");
            originTagHelpers = default;
            return false;
        }

        if (!TryGetTagHelperBinding(owner, absoluteIndex, out var binding))
        {
            originTagHelpers = default;
            return false;
        }

        // Can only have 1 component TagHelper belonging to an element at a time
        var primaryTagHelper = binding.TagHelpers.FirstOrDefault(static d => d.Kind == TagHelperKind.Component);
        if (primaryTagHelper is null)
        {
            originTagHelpers = default;
            return false;
        }

        var tagHelpers = codeDocument.GetRequiredTagHelpers();
        var associatedTagHelper = TryFindAssociatedTagHelper(primaryTagHelper, tagHelpers);

        originTagHelpers = new(primaryTagHelper, associatedTagHelper);
        return true;
    }

    private static bool TryGetTagHelperBinding(RazorSyntaxNode owner, int absoluteIndex, [NotNullWhen(true)] out TagHelperBinding? binding)
    {
        // End tags are easy, because there is only one possible binding result
        if (owner is MarkupTagHelperEndTagSyntax { Parent: MarkupTagHelperElementSyntax { TagHelperInfo.BindingResult: var endTagBindingResult } })
        {
            binding = endTagBindingResult;
            return true;
        }

        // A rename of a start tag could have an "owner" of one of its attributes, so we do a bit more checking
        // to support this case
        if (owner.FirstAncestorOrSelf<MarkupTagHelperStartTagSyntax>() is not { } tagHelperStartTag)
        {
            binding = null;
            return false;
        }

        // Ensure the rename action was invoked on the component name instead of a component parameter. This serves as an issue
        // mitigation till `textDocument/prepareRename` is supported and we can ensure renames aren't triggered in unsupported
        // contexts. (https://github.com/dotnet/razor/issues/4285)
        if (!tagHelperStartTag.Name.Span.IntersectsWith(absoluteIndex))
        {
            binding = null;
            return false;
        }

        if (tagHelperStartTag is { Parent: MarkupTagHelperElementSyntax { TagHelperInfo.BindingResult: var startTagBindingResult } })
        {
            binding = startTagBindingResult;

            // If the component is fully qualified, we need to make sure that the caret is in the actual component name part
            // not a namespace part.
            if (binding.TagHelpers is [{ IsFullyQualifiedNameMatch: true }, ..])
            {
                var lastDotIndex = tagHelperStartTag.Name.Content.LastIndexOf('.');
                Debug.Assert(lastDotIndex != -1, "Fully qualified component names should contain a dot.");
                if (absoluteIndex < tagHelperStartTag.Name.SpanStart + lastDotIndex + 1)
                {
                    binding = null;
                    return false;
                }
            }

            return true;
        }

        binding = null;
        return false;
    }

    private static TagHelperDescriptor? TryFindAssociatedTagHelper(
        TagHelperDescriptor primary,
        TagHelperCollection tagHelpers)
    {
        var typeName = primary.TypeName;
        var assemblyName = primary.AssemblyName;

        foreach (var tagHelper in tagHelpers)
        {
            if (typeName == tagHelper.TypeName &&
                assemblyName == tagHelper.AssemblyName &&
                !tagHelper.Equals(primary))
            {
                return tagHelper;
            }
        }

        return null;
    }
}
