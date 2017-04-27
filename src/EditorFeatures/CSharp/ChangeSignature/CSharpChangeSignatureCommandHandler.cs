// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.ChangeSignature;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ChangeSignature
{
    [VSC.ExportCommandHandler(PredefinedCommandHandlerNames.ChangeSignature, ContentTypeNames.CSharpContentType)]
    internal class CSharpChangeSignatureCommandHandler : AbstractChangeSignatureCommandHandler
    {
        [ImportingConstructor]
        public CSharpChangeSignatureCommandHandler(IWaitIndicator waitIndicator)
            : base(waitIndicator)
        {
        }
    }
}
