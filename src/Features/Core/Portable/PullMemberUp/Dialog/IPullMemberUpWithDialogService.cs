// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PullMemberUp.Dialog
{
    internal interface IPullMemberUpWithDialogService : ILanguageService
    {
        Task<Solution> ComputeInterfaceRefactoring(PullMemberDialogResult result, Document contextDocument, CancellationToken cancellationToken);

        Task<Solution> ComputeClassRefactoring(PullMemberDialogResult result, Document contextDocument, CancellationToken cancellationToken);
    }
}
