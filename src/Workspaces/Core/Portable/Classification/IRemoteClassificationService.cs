// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IRemoteClassificationService
    {
        Task AddSemanticClassificationsAsync(DocumentId documentId, TextSpan textSpan);
    }
}