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
        public ParsedStackFrame(string originalText, TextSpan typeSpan, TextSpan methodSpan, TextSpan argsSpan)
            : base(originalText)
        {
            Contract.ThrowIfTrue(typeSpan.IsEmpty);
            Contract.ThrowIfTrue(methodSpan.IsEmpty);

            TypeSpan = typeSpan;
            MethodSpan = methodSpan;
            ArgsSpan = argsSpan;
        }

        /// <summary>
        /// The full type name parsed from the line. 
        /// e.x: [|Microsoft.CodeAnalysis.Editor.CallstackExplorer.|]Example(arg1, arg2)
        /// </summary>
        public TextSpan TypeSpan { get; }

        /// <summary>
        /// The method name span
        /// e.x: Microsoft.CodeAnalysis.Editor.CallstackExplorer.[|Example|](arg1, arg2)
        /// </summary>
        public TextSpan MethodSpan { get; }

        /// <summary>
        /// The span of comma seperated arguments.
        /// e.x: Microsoft.CodeAnalysis.Editor.CallstackExplorer.Example[|(arg1, arg2)|]
        /// </summary>
        public TextSpan ArgsSpan { get; }

        public virtual async Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
        {
            // The original span for type includes the trailing '.', which we don't want when
            // looking for the class by metadata name
            var fullyQualifiedTypeName = OriginalText[TypeSpan.Start..(TypeSpan.End - 1)];
            var methodName = GetMethodText();

            foreach (var project in solution.Projects)
            {
                var service = project.GetLanguageService<IStackTraceExplorerService>();
                if (service is null)
                {
                    continue;
                }

                var metadataName = service.GetClassMetadataName(fullyQualifiedTypeName);
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
        /// Gets all of the text prior to the <see cref="TypeSpan"/>
        /// </summary>
        /// <returns></returns>
        public string GetTextBeforeType()
        {
            return OriginalText[..TypeSpan.Start];
        }

        /// <summary>
        /// Gets the text representing the fully qualified type name
        /// </summary>
        public string GetQualifiedTypeText()
        {
            return OriginalText[TypeSpan.Start..TypeSpan.End];
        }

        /// <summary>
        /// Gets the method text, including the arguments to the method
        /// </summary>
        public string GetMethodText()
        {
            return OriginalText[MethodSpan.Start..ArgsSpan.End];
        }

        /// <summary>
        /// Gets the text after the last parsed span available.
        /// </summary>
        public virtual string GetTrailingText()
        {
            if (ArgsSpan.End + 1 == OriginalText.Length)
            {
                return string.Empty;
            }

            return OriginalText[(ArgsSpan.End + 1)..];
        }
    }
}
