// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ExtractInterface
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.ExtractInterface)]
    internal class ExtractInterfaceCommandHandler : AbstractExtractInterfaceCommandHandler
    {
        [ImportingConstructor]
        public ExtractInterfaceCommandHandler(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }
    }
}
