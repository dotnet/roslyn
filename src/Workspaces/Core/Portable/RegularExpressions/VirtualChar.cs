// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    /// <summary>
    /// The Regex parser wants to work over an array of characters, however this array of characters
    /// is not the same as the array of characters a user types into a string in C# or VB.  For example
    /// In C# someone may write: @"\z".  This should appear to the user the same as if they wrote "\\z"
    /// and the same as "\\\u007a".  However, as these all have wildly different presentations for the
    /// user, there needs to be a way to map back the characters it sees ( '\' and 'z' ) back to the 
    /// ranges of characters the user wrote.  
    /// 
    /// VirtualChar serves this purpose.  It contains the interpretted value of any language character/
    /// character-escape-sequence, as well as the original SourceText span where that interpretted 
    /// character was created from.  This allows the regex engine to both process regexes from any
    /// language uniformly, but then also produce trees and diagnostics that map back properly to
    /// the original source text locations that make sense to the user.
    /// </summary>
    internal struct VirtualChar : IEquatable<VirtualChar>
    {
        public readonly char Char;
        public readonly TextSpan Span;

        public VirtualChar(char @char, TextSpan span)
        {
            if (span.IsEmpty)
            {
                throw new ArgumentException();
            }

            Char = @char;
            Span = span;
        }

        public override bool Equals(object obj)
            => obj is VirtualChar vc && Equals(vc);

        public bool Equals(VirtualChar other)
            => Char == other.Char &&
               Span.Equals(other.Span);

        public override int GetHashCode()
        {
            var hashCode = 244102310;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Char.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TextSpan>.Default.GetHashCode(Span);
            return hashCode;
        }

        public static bool operator ==(VirtualChar char1, VirtualChar char2)
            => char1.Equals(char2);

        public static bool operator !=(VirtualChar char1, VirtualChar char2)
            => !(char1 == char2);

        public static implicit operator char(VirtualChar vc) => vc.Char;
    }

    /// <summary>
    /// Helper service that takes the raw text of a string token and produces the individual
    /// characters that raw string token represents (i.e. with escapes collapsed).  The difference
    /// between this and the result from token.ValueText is that for each collapsed character returned
    /// the original span of text in the original token can be found.  i.e. if you had the following
    /// in C#:
    /// 
    /// "G\u006fo"
    /// 
    /// Then you'd get back:
    /// 
    /// 'G' -> [0, 1)
    /// 'o' -> [1, 7)
    /// 'o' -> [7, 1)
    /// 
    /// This allows for regex processing that can refer back to the users' original code instead of
    /// the escaped value we're processing.
    /// 
    /// </summary>
    internal interface IVirtualCharService : ILanguageService
    {
        ImmutableArray<VirtualChar> TryConvertToVirtualChars(SyntaxToken token);
    }

    internal abstract class AbstractVirtualCharService : IVirtualCharService
    {
        public ImmutableArray<VirtualChar> TryConvertToVirtualChars(SyntaxToken token)
        {
            if (token.ContainsDiagnostics)
            {
                return default;
            }

            var result = TryConvertToVirtualCharsWorker(token);

#if DEBUG
            // Do some invariant checking to make sure we processed the string token the same
            // way the C# compiler did.
            if (!result.IsDefault)
            {
                // Ensure that we properly broke up the token into a sequence of characters that
                // matches what the compiler did.
                var expectedValueText = token.ValueText;
                var actualValueText = new string(result.Select(vc => vc.Char).ToArray());
                Debug.Assert(expectedValueText == actualValueText);

                if (result.Length > 0)
                {
                    var currentVC = result[0];
                    Debug.Assert(currentVC.Span.Start > token.SpanStart, "First span has to start after the start of the string token (including its delimeter)");
                    Debug.Assert(currentVC.Span.Start == token.SpanStart + 1 || currentVC.Span.Start == token.SpanStart + 2, "First span should start on the second or third char of the string.");
                    for (var i = 1; i < result.Length; i++)
                    {
                        var nextVC = result[i];
                        Debug.Assert(currentVC.Span.End == nextVC.Span.Start, "Virtual character spans have to be touching.");
                        currentVC = nextVC;
                    }

                    var lastVC = result.Last();
                    Debug.Assert(lastVC.Span.End == token.Span.End - 1, "Last span has to end right before the end of hte string token (including its trailing delimeter).");
                }
            }
#endif

            return result;
        }

        protected abstract ImmutableArray<VirtualChar> TryConvertToVirtualCharsWorker(SyntaxToken token);
    }
}
