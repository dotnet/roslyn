// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
