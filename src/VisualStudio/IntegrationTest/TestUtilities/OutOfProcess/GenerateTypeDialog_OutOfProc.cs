// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class GenerateTypeDialog_OutOfProc : OutOfProcComponent
    {
        private readonly GenerateTypeDialog_InProc _inProc;

        public GenerateTypeDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<GenerateTypeDialog_InProc>(visualStudioInstance);
        }

        public void VerifyOpen()
            => _inProc.VerifyOpen();

        public void VerifyClosed()
            => _inProc.VerifyClosed();

        public void SetAccessibility(string accessibility)
            => _inProc.SetAccessibility(accessibility);

        public void SetKind(string kind)
            => _inProc.SetKind(kind);

        public void SetTargetProject(string projectName)
            => _inProc.SetTargetProject(projectName);

        public void SetTargetFileToNewName(string newFileName)
            => _inProc.SetTargetFileToNewName(newFileName);

        public void SetTargetFileToExisting(string existingFileName)
            => _inProc.SetTargetFileToExisting(existingFileName);

        public void ClickOK()
            => _inProc.ClickOK();

        public void ClickCancel()
            => _inProc.ClickCancel();
    }
}
