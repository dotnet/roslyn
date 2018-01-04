// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    /// <summary>
    /// Represents an error in a regular expression.  The error contains the message to show a user
    /// (which should be as close as possible to the error that the .Net regex library would produce),
    /// as well as the span of the error.  This span is in actual user character coordinates.  For
    /// example, if the user has the string "...\\p{0}..." then the span of the error would be for
    /// the range of characters for '\\p{0}' (even though the regex engine would only see the \\ translated
    /// as a virtual char to the single \ character.
    /// </summary>
    internal struct RegexDiagnostic : IEquatable<RegexDiagnostic>
    {
        public readonly string Message;
        public readonly TextSpan Span;

        public RegexDiagnostic(string message, TextSpan span)
        {
            Debug.Assert(message != null);
            Message = message;
            Span = span;
        }

        public override bool Equals(object obj)
            => obj is RegexDiagnostic rd && Equals(rd);

        public bool Equals(RegexDiagnostic other)
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

        public static bool operator ==(RegexDiagnostic diagnostic1, RegexDiagnostic diagnostic2)
            => diagnostic1.Equals(diagnostic2);

        public static bool operator !=(RegexDiagnostic diagnostic1, RegexDiagnostic diagnostic2)
            => !(diagnostic1 == diagnostic2);
    }
}
