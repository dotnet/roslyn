// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    /// <summary>
    /// A line of text that was parsed by <see cref="StackTraceAnalyzer" />
    /// to provide metadata bout the line. 
    /// </summary>
    internal class ParsedStackFrame : ParsedFrame
    {
        public ParsedStackFrame(string originalText, TextSpan classSpan, TextSpan methodSpan, TextSpan argsSpan)
            : base(originalText)
        {
            Contract.ThrowIfTrue(classSpan.IsEmpty);
            Contract.ThrowIfTrue(methodSpan.IsEmpty);

            ClassSpan = classSpan;
            MethodSpan = methodSpan;
            ArgsSpan = argsSpan;
        }

        /// <summary>
        /// The full classname parsed from the line. 
        /// e.x: [|Microsoft.CodeAnalysis.Editor.CallstackExplorer|].Example(arg1, arg2)
        /// </summary>
        public TextSpan ClassSpan { get; }

        /// <summary>
        /// The method name span
        /// e.x: Microsoft.CodeAnalysis.Editor.CallstackExplorer.[|Example|](arg1, arg2)
        /// </summary>
        public TextSpan MethodSpan { get; }

        /// <summary>
        /// The span of comma seperated arguments.
        /// e.x: Microsoft.CodeAnalysis.Editor.CallstackExplorer.Example([|arg1, arg2|])
        /// </summary>
        public TextSpan ArgsSpan { get; }

        public virtual async Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
        {
            var className = GetClassText(includeTrailingDot: false);
            var methodName = GetMethodText();

            foreach (var project in solution.Projects)
            {
                var service = project.GetLanguageService<IStackTraceExplorerService>();
                if (service is null)
                {
                    continue;
                }

                var metadataName = service.GetClassMetadataName(className);
                var memberName = service.GetMethodSymbolName(methodName);

                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var type = compilation.GetTypeByMetadataName(metadataName);
                if (type is null)
                {
                    continue;
                }

                var members = type.GetMembers();
                var matchingMembers = members.WhereAsArray(m => MemberMatchesMethodName(m, memberName));
                if (matchingMembers.Length == 0)
                {
                    continue;
                }

                if (matchingMembers.Length == 1)
                {
                    return matchingMembers[0];
                }
            }

            return null;

            static bool MemberMatchesMethodName(ISymbol member, string memberToSearchFor)
            {
                var displayName = member.ToDisplayString();
                var dotIndex = displayName.LastIndexOf(".");
                var memberName = dotIndex >= 0
                    ? displayName[(dotIndex + 1)..]
                    : displayName;

                return string.Equals(memberName, memberToSearchFor, System.StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets all of the text prior to the <see cref="ClassSpan"/>
        /// </summary>
        /// <returns></returns>
        public string GetLeadingText()
        {
            return OriginalText[..ClassSpan.Start];
        }

        /// <summary>
        /// Gets the text representing the fully qualified class name
        /// </summary>
        public string GetClassText(bool includeTrailingDot = true)
        {
            return OriginalText[ClassSpan.Start..(includeTrailingDot ? ClassSpan.End : ClassSpan.End - 1)];
        }

        /// <summary>
        /// Gets the method text, including the arguments to the method
        /// </summary>
        public string GetMethodText()
        {

            return OriginalText[MethodSpan.Start..ArgEndIndex];
        }

        public virtual string GetTrailingText()
        {
            return OriginalText[ArgEndIndex..];
        }

        // +1 to the argspan end because we want to include the closing paren
        protected int ArgEndIndex => ArgsSpan.End + 1;
    }
}
