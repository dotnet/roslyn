// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
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
