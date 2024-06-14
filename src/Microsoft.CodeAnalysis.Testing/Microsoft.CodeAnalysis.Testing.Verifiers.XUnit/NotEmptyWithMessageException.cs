// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    [Obsolete(ObsoleteMessages.FrameworkPackages)]
    public class NotEmptyWithMessageException : NotEmptyException
    {
        public NotEmptyWithMessageException(string userMessage)
        {
            UserMessage = userMessage;
        }

        public override string Message
        {
            get
            {
                if (string.IsNullOrEmpty(UserMessage))
                {
                    return base.Message;
                }

                return UserMessage + Environment.NewLine + base.Message;
            }
        }
    }
}
