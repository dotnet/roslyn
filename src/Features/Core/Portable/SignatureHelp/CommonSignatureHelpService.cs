// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal abstract class CommonSignatureHelpService : SignatureHelpServiceWithProviders
    {
        protected CommonSignatureHelpService(Workspace workspace, string language)
            : base(workspace, language)
        {
        }
    }
}
