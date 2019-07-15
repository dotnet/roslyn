// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal class SignatureHelpState
    {
        public int ArgumentIndex;
        public int ArgumentCount;
        public string ArgumentName;
        public IList<string> ArgumentNames;

        public SignatureHelpState(int argumentIndex, int argumentCount, string argumentName, IList<string> argumentNames)
        {
            ArgumentIndex = argumentIndex;
            ArgumentCount = argumentCount;
            ArgumentName = argumentName;
            ArgumentNames = argumentNames;
        }
    }
}
