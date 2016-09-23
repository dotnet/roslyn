// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal abstract class SignatureHelpProvider
    {
        protected SignatureHelpProvider()
        {
        }

        public abstract bool IsTriggerCharacter(char ch);
        public abstract bool IsRetriggerCharacter(char ch);

        public abstract Task ProvideSignaturesAsync(SignatureContext context);
    }
}
