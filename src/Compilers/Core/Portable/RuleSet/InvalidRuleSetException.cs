// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents errors that occur while parsing RuleSet files.
    /// </summary>
    internal class InvalidRuleSetException : Exception
    {
        public InvalidRuleSetException() { }
        public InvalidRuleSetException(string message) : base(message) { }
        public InvalidRuleSetException(string message, Exception inner) : base(message, inner) { }
    }
}
