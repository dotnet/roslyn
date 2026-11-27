// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.RazorExtension.NestedFiles;

/// <summary>
/// Handles the "View Page" command from nested file editors (ie: foo.(cshtml|razor).(cs|css|js))
/// to navigate back to the parent Razor file.
/// </summary>
internal sealed class ViewPageCommandHandler(IServiceProvider serviceProvider)
{
    private static readonly string[] s_nestedExtensions = [".cs", ".css", ".js"];
    private static readonly string[] s_razorExtensions = [".cshtml", ".razor"];

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public void OnBeforeQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (sender is not OleMenuCommand command)
        {
            return;
        }

        if (!SelectionHelper.IsRazorFileUIContextActive(_serviceProvider)
            || TryGetParentRazorFilePath() is null)
        {
            command.Visible = false;
            return;
        }

        command.Supported = true;
        command.Visible = true;
        command.Enabled = true;
        command.Text = Resources.View_Page;
    }

    public void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (TryGetParentRazorFilePath() is string parentFilePath)
        {
            VsShellUtilities.OpenDocument(_serviceProvider, parentFilePath);
        }
    }

    /// <summary>
    /// Gets the file path of the currently selected/active item via IVsMonitorSelection,
    /// then checks if it's a nested Razor file and returns the parent path if so.
    /// Works for both Solution Explorer selection and active editor documents
    /// because IVsMonitorSelection tracks the active window frame's hierarchy item.
    /// </summary>
    private string? TryGetParentRazorFilePath()
    {
        var filePath = SelectionHelper.GetCurrentSelectionPath(_serviceProvider);

        if (filePath is null)
        {
            return null;
        }

        // Check if the file has a nested extension (.cs, .css, .js)
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!s_nestedExtensions.Contains(extension))
        {
            return null;
        }

        // Strip the nested extension to get the potential parent path
        var parentPath = filePath.Substring(0, filePath.Length - extension.Length);

        // Verify the parent has a Razor extension (.cshtml or .razor)
        var parentExtension = Path.GetExtension(parentPath).ToLowerInvariant();

        if (s_razorExtensions.Contains(parentExtension))
        {
            // Only show if the parent file exists
            return File.Exists(parentPath) ? parentPath : null;
        }

        return null;
    }
}
