using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal interface ISyntaxTreeInfo
    {
        /// <summary>
        /// Returns true when the identifier is probably (but not guaranteed) to be within the
        /// syntax tree.  Returns false when the identifier is guaranteed to not be within the
        /// syntax tree.
        /// </summary>
        bool ProbablyContainsIdentifier(string identifier);

        /// <summary>
        /// Returns true when the identifier is probably (but not guaranteed) escaped within the
        /// text of the syntax tree.  Returns false when the identifier is guaranteed to not be
        /// escaped within the text of the syntax tree.  An identifier that is not escaped within
        /// the text can be found by searching the text directly.  An identifier that is escaped can
        /// only be found by parsing the text and syntactically interpreting any escaping
        /// mechanisms found in the language ("\uXXXX" or "@XXXX" in C# or "[XXXX]" in Visual
        /// Basic).
        /// </summary>
        bool ProbablyContainsEscapedIdentifier(string identifier);

        bool ContainsPredefinedType(PredefinedType type);
        bool ContainsPredefinedOperator(PredefinedOperator op);

        bool ContainsForEachStatement { get; }
        bool ContainsLockStatement { get; }
        bool ContainsUsingStatement { get; }

        bool ContainsThisConstructorInitializer { get; }
        bool ContainsBaseConstructorInitializer { get; }
        bool ContainsQueryExpression { get; }

        bool ContainsElementAccessExpression { get; }
        bool ContainsIndexerMemberCref { get; }
    }
}