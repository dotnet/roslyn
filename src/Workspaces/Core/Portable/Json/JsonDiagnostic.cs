// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Json
{
    /// <summary>
    /// Represents an error in a json expression.  The error contains the message to show a user
    /// as well as the span of the error.  This span is in actual user character coordinates.
    /// </summary>
    internal struct JsonDiagnostic : IEquatable<JsonDiagnostic>
    {
        public readonly string Message;
        public readonly TextSpan Span;

        public JsonDiagnostic(string message, TextSpan span)
        {
            Debug.Assert(message != null);
            Message = message;
            Span = span;
        }

        public override bool Equals(object obj)
            => obj is JsonDiagnostic rd && Equals(rd);

        public bool Equals(JsonDiagnostic other)
            => Message == other.Message &&
               Span.Equals(other.Span);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -954867195;
                hashCode = hashCode * -1521134295 + base.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Message);
                hashCode = hashCode * -1521134295 + EqualityComparer<TextSpan>.Default.GetHashCode(Span);
                return hashCode;
            }
        }

        public static bool operator ==(JsonDiagnostic diagnostic1, JsonDiagnostic diagnostic2)
            => diagnostic1.Equals(diagnostic2);

        public static bool operator !=(JsonDiagnostic diagnostic1, JsonDiagnostic diagnostic2)
            => !(diagnostic1 == diagnostic2);
    }
}
