// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal delegate Cci.DebugSourceDocument DebugDocumentProvider(string path, string basePath);
}
