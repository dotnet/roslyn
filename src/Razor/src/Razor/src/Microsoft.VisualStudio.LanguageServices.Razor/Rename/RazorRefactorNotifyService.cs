// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.VisualStudio.Razor.Rename;

[Export(typeof(IRazorRefactorNotifyService))]
[method: ImportingConstructor]
internal sealed class RazorRefactorNotifyService(
    ILoggerFactory loggerFactory) : IRazorRefactorNotifyService
{
    private readonly IFileSystem _fileSystem = new FileSystem();
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RazorRefactorNotifyService>();

    public bool TryOnAfterGlobalSymbolRenamed(CodeAnalysis.Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
    {
        return OnAfterGlobalSymbolRenamed(symbol, newName, throwOnFailure, _fileSystem);
    }

    private bool OnAfterGlobalSymbolRenamed(ISymbol symbol, string newName, bool throwOnFailure, IFileSystem fileSystem)
    {
        // If the user is renaming a Razor component, we need to rename the .razor file or things will break for them,
        // however this method gets called for every symbol rename in Roslyn, so chances are low that it's a Razor component.
        // We have a few heuristics we can use to quickly work out if we care about the symbol, and go from there.

        ClassDeclarationSyntax? classDecl = null;
        foreach (var reference in symbol.OriginalDefinition.DeclaringSyntaxReferences)
        {
            var syntaxTree = reference.SyntaxTree;

            // First, we can check the file path of the syntax tree. Razor generated files have a very specific path format
            if (syntaxTree.FilePath.IndexOf(typeof(RazorSourceGenerator).FullName) == -1 ||
                !syntaxTree.FilePath.EndsWith("_razor.g.cs"))
            {
                continue;
            }

            // Now, we try to get the class declaration from the syntax tree. This method checks specifically for the
            // structure that Razor generates, so it acts as an extra check that this is actually a Razor file, but
            // it doesn't have anything to do with what is actually being renamed.
            if (!syntaxTree.GetRoot().TryGetClassDeclaration(out var thisClassDecl))
            {
                continue;
            }

            // We're pretty sure by now that the rename is of a symbol in a Razor file, but it might not be the component
            // itself. Let's check.
            if (reference.Span != thisClassDecl.Span)
            {
                continue;
            }

            classDecl = thisClassDecl;
            break;
        }

        // If we didn't find a class declaration that matches the symbol reference, its not a Razor file, or not the component
        if (classDecl is null)
        {
            return true;
        }

        // Now for the actual renaming, which is potentially the dodgiest bit. We need to figure out the original .razor file
        // name, but we have no idea which document we're dealing with, nor which project, and there is no API we can use from
        // Roslyn that can give us that info from the symbol. Additionally the changedDocumentIds parameter won't have it,
        // because edits to generated files are filtered out before we get called, and even if it did, we wouldn't know which
        // one was the right one.
        // So we can do one final check, which is that all Razor generated documents begin with a pragma checksum that
        // contains the Razor file name, and not only does that give us final validation, it also lets us know which file
        // to rename. To do this "properly" would mean jumping over to OOP, and probably running all generators in all
        // projects, and then looking at host outputs etc. In other words, it would be very slow and inefficient. This is
        // quick.

        if (classDecl.Parent is null ||
            classDecl.Parent.GetLeadingTrivia() is not [{ } firstTrivia, ..] ||
            !firstTrivia.IsKind(CodeAnalysis.CSharp.SyntaxKind.PragmaChecksumDirectiveTrivia) ||
            firstTrivia.ToString().Split(' ') is not ["#pragma", "checksum", { } quotedRazorFileName, ..])
        {
            return true;
        }

        // Let's make sure the pragma actually contained a Razor file name, just in case the compiler changes
        if (quotedRazorFileName.Trim('"') is not { } razorFileName ||
            !FileUtilities.IsRazorComponentFilePath(razorFileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!fileSystem.FileExists(razorFileName))
        {
            return true;
        }

        Debug.Assert(Path.GetExtension(razorFileName) == ".razor");
        var newFileName = Path.Combine(Path.GetDirectoryName(razorFileName), newName + ".razor");

        // If the new file name already exists, then the rename can continue, it will just be moving from ComponentA to
        // ComponentB, but ComponentA remains. Hopefully this is what the user intended :)
        if (fileSystem.FileExists(newFileName))
        {
            return true;
        }

        try
        {
            // Roslyn has no facility to rename an additional file, so there is no real benefit to do anything but
            // rename the file on disk, and let all of the other systems handle it.
            fileSystem.Move(razorFileName, newFileName);

            // Now try to rename the associated files too
            if (fileSystem.FileExists($"{razorFileName}.cs"))
            {
                fileSystem.Move($"{razorFileName}.cs", $"{newFileName}.cs");
            }

            if (fileSystem.FileExists($"{razorFileName}.css"))
            {
                fileSystem.Move($"{razorFileName}.css", $"{newFileName}.css");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename Razor component file during symbol rename.");
            if (throwOnFailure)
            {
                throw;
            }

            // If we've tried to actually rename the file, and it didn't work, then we can be okay to block the rename operation
            // because otherwise the user will just get lots of broken references. Chances are its too late to block them anyway
            // but here we are.
            return false;
        }

        return true;
    }

    public bool TryOnBeforeGlobalSymbolRenamed(CodeAnalysis.Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
    {
        return true;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(RazorRefactorNotifyService instance)
    {
        public bool OnAfterGlobalSymbolRenamed(ISymbol symbol, string newName, bool throwOnFailure, IFileSystem fileSystem)
            => instance.OnAfterGlobalSymbolRenamed(symbol, newName, throwOnFailure, fileSystem);
    }
}
