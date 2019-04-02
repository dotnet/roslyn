// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class MoveToNamespaceDialog_OutOfProc : OutOfProcComponent
    {
        private readonly MoveToNamespaceDialog_InProc _inProc;

        public MoveToNamespaceDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<MoveToNamespaceDialog_InProc>(visualStudioInstance);
        }

        /// <summary>
        /// Verifies that the Move To Namespace dialog is currently open.
        /// </summary>
        public void VerifyOpen()
            => _inProc.VerifyOpen();

        /// <summary>
        /// Verifies that the Move To Namespace dialog is currently closed.
        /// </summary>
        public void VerifyClosed()
            => _inProc.VerifyClosed();

        public bool CloseWindow()
            => _inProc.CloseWindow();

        public void SetNamespace(string @namespace)
            => _inProc.SetSetNamespace(@namespace);

        /// <summary>
        /// Clicks the "OK" button and waits for the Move To Namespace operation to complete.
        /// </summary>
        public void ClickOK()
            => _inProc.ClickOK();

        /// <summary>
        /// Clicks the "Cancel" button and waits for the Move To Namespace operation to complete.
        /// </summary>
        public void ClickCancel()
            => _inProc.ClickCancel();
    }
}
