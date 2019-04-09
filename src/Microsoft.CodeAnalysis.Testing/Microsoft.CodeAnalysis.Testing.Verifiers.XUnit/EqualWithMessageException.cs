// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    public class EqualWithMessageException : EqualException
    {
        public EqualWithMessageException(object expected, object actual, string userMessage)
            : base(expected, actual)
        {
            UserMessage = userMessage;
        }

        public EqualWithMessageException(string expected, string actual, int expectedIndex, int actualIndex, string userMessage)
            : base(expected, actual, expectedIndex, actualIndex)
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
