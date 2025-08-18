// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Service interface for triggering analyzer refresh on specific documents.
/// </summary>
internal interface IDiagnosticInvalidationService : IWorkspaceService
{
    /// <summary>
    /// Triggers analyzer refresh for a specific document using the most targeted method available.
    /// </summary>
    Task TriggerDocumentRefreshAsync(Document document);
}

/// <summary>
/// Lightweight service for targeted diagnostic invalidation.
/// Uses multiple fallback strategies to trigger document-specific reanalysis.
/// Called after AI analysis completes and cache is updated.
/// </summary>
[ExportWorkspaceService(typeof(IDiagnosticInvalidationService), ServiceLayer.Default), Shared]
internal sealed class DiagnosticInvalidationService : IDiagnosticInvalidationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DiagnosticInvalidationService()
    {
    }

    /// <summary>
    /// Uses safe refresh methods to trigger document reanalysis.
    /// Tries document version change first, then falls back to global refresh.
    /// </summary>
    public async Task TriggerDocumentRefreshAsync(Document document)
    {
        if (document == null)
            return;

        try
        {
            // METHOD 1: Try document version change (targeted to document)  
            if (await TryDocumentVersionRefreshAsync(document).ConfigureAwait(false))
                return;

            // METHOD 2: Fallback to global refresh (least targeted but reliable)
            await TryGlobalDiagnosticRefreshAsync(document).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Silent failure - cache is updated, diagnostics will appear eventually
        }
    }

    /// <summary>
    /// METHOD 1: Force document version change to trigger reanalysis.
    /// This is safer than text manipulation and more targeted than global refresh.
    /// </summary>
    private static async Task<bool> TryDocumentVersionRefreshAsync(Document document)
    {
        try
        {
            var workspace = document.Project.Solution.Workspace;
            
            // Check if workspace allows document changes
            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
                return false;

            var currentText = await document.GetTextAsync().ConfigureAwait(false);
            var solution = document.Project.Solution;
            
            // Create new solution with the same text but new version
            // This forces a document change event without actually changing content
            var newSolution = solution.WithDocumentText(document.Id, currentText);
            
            return workspace.TryApplyChanges(newSolution);
        }
        catch (InvalidOperationException)
        {
            // Workspace doesn't allow changes right now - this was causing our original error
            return false;
        }
        catch (Exception)
        {
            // Other errors
            return false;
        }
    }

    /// <summary>
    /// METHOD 2: Global diagnostic refresh as fallback.
    /// </summary>
    private static async Task<bool> TryGlobalDiagnosticRefreshAsync(Document document)
    {
        try
        {
            var diagnosticService = document.Project.Solution.Services.GetService<IDiagnosticAnalyzerService>();
            if (diagnosticService != null)
            {
                diagnosticService.RequestDiagnosticRefresh();
                await Task.Delay(50).ConfigureAwait(false); // Small delay for processing
                return true;
            }
        }
        catch (Exception)
        {
            // Service not available
        }
        
        return false;
    }
}