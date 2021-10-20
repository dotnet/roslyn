// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater
{

    internal class OptionUpdater : SettingsUpdaterBase<IOption2, object>
    {
        public OptionUpdater(Workspace workspace, string editorconfigPath)
            : base(workspace, editorconfigPath)
        {
        }

        protected override SourceText? GetNewText(SourceText SourceText,
                                                  IReadOnlyList<(IOption2 option, object value)> settingsToUpdate,
                                                  CancellationToken token)
            => SettingsUpdateHelper.TryUpdateAnalyzerConfigDocument(SourceText, EditorconfigPath, Workspace.Options, settingsToUpdate);
    }
}
