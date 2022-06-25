// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class PickMembersDialog_InProc : InProcComponent
    {
        private PickMembersDialog_InProc()
        {
        }

        public static PickMembersDialog_InProc Create()
            => new PickMembersDialog_InProc();

        public bool CloseWindow()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                if (JoinableTaskFactory.Run(() => TryGetDialogAsync(cancellationTokenSource.Token)) is null)
                {
                    return false;
                }
            }

            ClickCancel();
            return true;
        }

        public void ClickCancel()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.CancelButton, cancellationTokenSource.Token));
            }
        }

        private static async Task<PickMembersDialog> GetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<PickMembersDialog>().Single();
        }

        private static async Task<PickMembersDialog> TryGetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<PickMembersDialog>().SingleOrDefault();
        }

        private static async Task ClickAsync(Func<PickMembersDialog.TestAccessor, ButtonBase> buttonSelector, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            var dialog = await GetDialogAsync(cancellationToken);
            var button = buttonSelector(dialog.GetTestAccessor());
            Contract.ThrowIfFalse(await button.SimulateClickAsync(JoinableTaskFactory));
        }
    }
}
