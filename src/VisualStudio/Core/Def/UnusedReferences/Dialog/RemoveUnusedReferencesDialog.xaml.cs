// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog;

/// <summary>
/// Interaction logic for RemoveUnusedReferencesDialog.xaml
/// </summary>
internal partial class RemoveUnusedReferencesDialog : DialogWindow
{
    public string RemoveUnusedReferences => ServicesVSResources.Remove_Unused_References;
    public string HelpText => ServicesVSResources.Choose_which_action_you_would_like_to_perform_on_the_unused_references;
    public string Apply => ServicesVSResources.Apply;
    public string Cancel => ServicesVSResources.Cancel;

    private readonly UnusedReferencesTableProvider _tableProvider;

    public RemoveUnusedReferencesDialog(UnusedReferencesTableProvider tableProvider)
        : base()
    {
        _tableProvider = tableProvider;

        InitializeComponent();
    }

    public bool? ShowModal(JoinableTaskFactory joinableTaskFactory, Solution solution, string projectFilePath, ImmutableArray<ReferenceUpdate> referenceUpdates)
    {
        bool? result = null;

        try
        {
            _tableProvider.AddTableData(solution, projectFilePath, referenceUpdates);

            using var tableControl = _tableProvider.CreateTableControl();
            TablePanel.Child = tableControl.Control;

            // The table control updates its view of the datasource on a background thread.
            // This will force the table to update.
            joinableTaskFactory.Run(tableControl.ForceUpdateAsync);

            result = ShowModal();

            TablePanel.Child = null;
        }
        finally
        {
            // Ensure we clear out the table data to prevent leaks.
            _tableProvider.ClearTableData();
        }

        return result;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
