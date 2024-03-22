// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using static Microsoft.VisualStudio.VSConstants;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

[Guid(Guids.StackTraceExplorerToolWindowIdString)]
internal class StackTraceExplorerToolWindow : ToolWindowPane, IOleCommandTarget
{
    private bool _initialized;

    [MemberNotNullWhen(true, nameof(_initialized))]
    public StackTraceExplorerRoot? Root { get; private set; }

    public StackTraceExplorerToolWindow() : base(null)
    {
        Caption = ServicesVSResources.Stack_Trace_Explorer;
        var dockPanel = new DockPanel
        {
            LastChildFill = true
        };

        dockPanel.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) =>
        {
            Root?.ViewModel.DoPasteAsync(default).FileAndForget("StackTraceExplorerPaste");
        }));

        Content = dockPanel;
    }

    /// <summary>
    /// Checks the contents of the clipboard for a valid stack trace and 
    /// opens stack trace explorer if anything parses correctly
    /// </summary>
    public async Task<bool> ShouldShowOnActivatedAsync(CancellationToken cancellationToken)
    {
        if (Root is null)
        {
            return false;
        }

        var text = ClipboardHelpers.GetTextNoRetry();
        if (RoslynString.IsNullOrEmpty(text))
        {
            return false;
        }

        if (Root.ViewModel.ContainsTab(text))
        {
            return false;
        }

        var result = await StackTraceAnalyzer.AnalyzeAsync(text, cancellationToken).ConfigureAwait(false);
        if (result.ParsedFrames.Any(static frame => FrameTriggersActivate(frame)))
        {
            await Root.ViewModel.AddNewTabAsync(result, text, cancellationToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private static bool FrameTriggersActivate(ParsedFrame frame)
    {
        if (frame is not ParsedStackFrame parsedFrame)
        {
            return false;
        }

        var methodDeclaration = parsedFrame.Root.MethodDeclaration;

        // Find the first token
        var firstNodeOrToken = methodDeclaration.ChildAt(0);
        while (firstNodeOrToken.IsNode)
        {
            firstNodeOrToken = firstNodeOrToken.Node.ChildAt(0);
        }

        if (firstNodeOrToken.Token.LeadingTrivia.IsDefault)
        {
            return false;
        }

        // If the stack frame starts with "at" we consider it a well formed stack frame and 
        // want to automatically open the window. This helps avoids some false positive cases 
        // where the window shows on code that parses as a stack frame but may not be. The explorer
        // should still handle those cases if explicitly pasted in, but can lead to false positives 
        // when automatically opening.
        return firstNodeOrToken.Token.LeadingTrivia.Any(static t => t.Kind == StackFrameKind.AtTrivia);
    }

    public void InitializeIfNeeded(RoslynPackage roslynPackage)
    {
        if (_initialized)
        {
            return;
        }

        var workspace = roslynPackage.ComponentModel.GetService<VisualStudioWorkspace>();
        var formatMapService = roslynPackage.ComponentModel.GetService<IClassificationFormatMapService>();
        var formatMap = formatMapService.GetClassificationFormatMap(StandardContentTypeNames.Text);
        var typeMap = roslynPackage.ComponentModel.GetService<ClassificationTypeMap>();
        var threadingContext = roslynPackage.ComponentModel.GetService<IThreadingContext>();
        var themingService = roslynPackage.ComponentModel.GetService<IWpfThemeService>();

        Root = new StackTraceExplorerRoot(new StackTraceExplorerRootViewModel(threadingContext, workspace, formatMap, typeMap))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var contentRoot = (DockPanel)Content;
        themingService?.ApplyThemeToElement(contentRoot);
        contentRoot.Children.Add(Root);

        contentRoot.MouseRightButtonUp += (s, e) =>
        {
            var uiShell = roslynPackage.GetServiceOnMainThread<SVsUIShell, IVsUIShell>();
            var relativePoint = e.GetPosition(contentRoot);
            var screenPosition = contentRoot.PointToScreen(relativePoint);

            var points = new[] {
                new POINTS()
                {
                    x = (short)screenPosition.X,
                    y = (short)screenPosition.Y
                }
            };

            var refCommandId = new Guid(Guids.StackTraceExplorerCommandIdString);
            var result = uiShell.ShowContextMenu(0, ref refCommandId, 0x0300, points, null);
            Debug.Assert(result == S_OK);
        };

        _initialized = true;
    }

    public override void OnToolWindowCreated()
    {
        // Hide the frame by default when VS starts
        if (Frame is IVsWindowFrame windowFrame)
        {
            windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
        }
    }

    int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        if (pguidCmdGroup == GUID_VSStandardCommandSet97)
        {
            var command = (VSStd97CmdID)nCmdID;
            switch (command)
            {
                case VSStd97CmdID.Paste:
                    Root?.ViewModel.DoPasteSynchronously(default);
                    return S_OK;
            }
        }

        // Return OLECMDERR_E_UNKNOWNGROUP if we don't handle the command
        // see https://docs.microsoft.com/en-us/windows/win32/api/docobj/nf-docobj-iolecommandtarget-exec#return-value
        return -2147221244;
    }
}
