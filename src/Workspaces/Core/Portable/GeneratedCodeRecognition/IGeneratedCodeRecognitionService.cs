// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.GeneratedCodeRecognition
{
    internal interface IGeneratedCodeRecognitionService : IWorkspaceService
    {
        bool IsGeneratedCode(Document document);
    }
}