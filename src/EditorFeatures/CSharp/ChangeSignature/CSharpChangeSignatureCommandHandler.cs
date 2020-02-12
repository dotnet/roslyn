﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ChangeSignature
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.ChangeSignature)]
    internal class CSharpChangeSignatureCommandHandler : AbstractChangeSignatureCommandHandler
    {
        [ImportingConstructor]
        public CSharpChangeSignatureCommandHandler(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }
    }
}
