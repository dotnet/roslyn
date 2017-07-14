// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ExtractInterface
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.ExtractInterface, ContentTypeNames.CSharpContentType)]
    internal class ExtractInterfaceCommandHandler : AbstractExtractInterfaceCommandHandler
    {
    }
}
