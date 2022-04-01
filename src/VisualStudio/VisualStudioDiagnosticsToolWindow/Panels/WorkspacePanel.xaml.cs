// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.DiagnosticsWindow
{
    /// <summary>
    /// Interaction logic for TelemetryPanel.xaml
    /// </summary>
    public sealed partial class WorkspacePanel : UserControl
    {
        private readonly DiagnosticsWindow _window;

        public WorkspacePanel(DiagnosticsWindow window)
        {
            _window = window;
            InitializeComponent();
        }

        private void OnDiagnose(object sender, RoutedEventArgs e)
        {
            _ = OnDiagnoseImplAsync();

            async Task OnDiagnoseImplAsync()
            {
                DiagnoseButton.IsEnabled = false;
                GenerationProgresBar.IsIndeterminate = true;
                Result.Text = "Comparing in-proc solution snapshot with files on disk ...";

                var text = await Task.Run(
                    async () =>
                    {
                        try
                        {
                            return await DiagnoseAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            return e.ToString();
                        }
                    }).ConfigureAwait(true);

                GenerationProgresBar.IsIndeterminate = false;
                DiagnoseButton.IsEnabled = true;
                Result.Text = text;
            }
        }

        private async Task<string> DiagnoseAsync(CancellationToken cancellationToken)
        {
            var workspace = _window.Workspace;
            if (workspace == null)
            {
                return "Workspace unavailable";
            }

            var output = new StringBuilder();
            await CompareClosedDocumentsWithFileSystemAsync(workspace, output, cancellationToken).ConfigureAwait(false);
            return output.ToString();
        }

        private static async Task CompareClosedDocumentsWithFileSystemAsync(Workspace workspace, StringBuilder output, CancellationToken cancellationToken)
        {
            var solution = workspace.CurrentSolution;
            var outOfDateCount = 0;
            var gate = new object();

            var tasks = from project in solution.Projects
                        from document in project.Documents
                        where document.FilePath != null
                        where !workspace.IsDocumentOpen(document.Id)
                        select CompareDocumentAsync(document);

            async Task CompareDocumentAsync(Document document)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var snapshotText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var snapshotChecksum = snapshotText.GetChecksum();

                using var fileStream = File.OpenRead(document.FilePath);
                var fileText = SourceText.From(fileStream, snapshotText.Encoding, snapshotText.ChecksumAlgorithm);
                var fileChecksum = fileText.GetChecksum();

                if (!fileChecksum.SequenceEqual(snapshotChecksum))
                {
                    lock (gate)
                    {
                        output.AppendLine($"{document.FilePath}: {BitConverter.ToString(snapshotChecksum.ToArray())} : {BitConverter.ToString(fileChecksum.ToArray())}");
                        outOfDateCount++;
                    }
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            output.AppendLine(outOfDateCount == 0 ?
                "All closed documents up to date." :
                $"{Environment.NewLine}{outOfDateCount} documents out of date.");
        }
    }
}
