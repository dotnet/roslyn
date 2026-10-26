// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal static class CodeStyleSettingsCommands
{
    internal const int GenerateEditorConfigCommandId = 0x0100;
    internal const int HelpCommandId = 0x0101;

    internal static ImmutableArray<MenuCommand> CreateCommands(Guid commandSet, Action generateEditorConfig, Action showHelp, CancellationToken disposalToken)
        => [CreateCommand(GenerateEditorConfigCommandId, generateEditorConfig, commandSet, disposalToken), CreateCommand(HelpCommandId, showHelp, commandSet, disposalToken)];

    internal static IDisposable Register(IMenuCommandService commandService, Guid commandSet, Action generateEditorConfig, Action showHelp, CancellationToken disposalToken)
    {
        var commands = CreateCommands(commandSet, generateEditorConfig, showHelp, disposalToken);
        foreach (var command in commands)
            commandService.AddCommand(command);
        return new Registration(commandService, commands);
    }

    private sealed class Registration(IMenuCommandService commandService, ImmutableArray<MenuCommand> commands) : IDisposable
    {
        public void Dispose()
        {
            foreach (var command in commands)
                commandService.RemoveCommand(command);
        }
    }

    // Use package-owned commands so the shell can load their handlers even before a preview is created.
    private static MenuCommand CreateCommand(int id, Action action, Guid commandSet, CancellationToken disposalToken)
        => new OleMenuCommand((_, _) =>
        {
            if (!disposalToken.IsCancellationRequested)
                action();
        }, new CommandID(commandSet, id));

    internal static void GenerateEditorConfig(IServiceProvider serviceProvider, OptionStore pendingOptions, string language)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Logger.Log(FunctionId.ToolsOptions_GenerateEditorconfig);
        var enumerator = serviceProvider.GetMefService<EditorConfigOptionsEnumerator>();
        var editorconfig = GenerateText(enumerator.GetOptions(language), pendingOptions, language);
        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = ServicesVSResources.All_files_filter,
            FileName = ".editorconfig",
            Title = ServicesVSResources.Save_dot_editorconfig_file,
            InitialDirectory = GetInitialDirectory(serviceProvider),
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            IOUtilities.PerformIO(() => File.WriteAllText(dialog.FileName, editorconfig.ToString()));
    }

    internal static string GenerateText(IEnumerable<(string feature, ImmutableArray<IOption2> options)> groupedOptions, OptionStore pendingOptions, string language)
        => EditorConfigFileGenerator.Generate(groupedOptions, pendingOptions, language);

    private static string GetInitialDirectory(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (serviceProvider.GetService(typeof(SVsSolution)) is IVsSolution solution &&
            !ErrorHandler.Failed(solution.GetSolutionInfo(out _, out var path, out _)))
        {
            return Path.GetDirectoryName(path) ?? string.Empty;
        }
        return string.Empty;
    }

    internal static void ShowHelp()
        => VisualStudioNavigateToLinkService.StartBrowser(GridOptionPreviewControl.CodeStylePageHeaderLearnMoreUri);
}
