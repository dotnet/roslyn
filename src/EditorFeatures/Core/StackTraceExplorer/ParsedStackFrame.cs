// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
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
            var className = OriginalText[ClassSpan.Start..ClassSpan.End];
            var methodName = OriginalText[MethodSpan.Start..MethodSpan.End];

            var symbolName = $"{className}.{methodName}";

            foreach (var project in solution.Projects)
            {
                var foundSymbols = await FindSymbols.DeclarationFinder.FindSourceDeclarationsWithPatternAsync(
                    project,
                    symbolName,
                    SymbolFilter.Member,
                    cancellationToken).ConfigureAwait(false);

                // Only use the first symbol for now. 
                // todo: should we return multiple and let a user decide? 
                var foundSymbol = foundSymbols.Length == 1
                    ? foundSymbols[0]
                    : null;

                if (foundSymbol is not null)
                {
                    return foundSymbol;
                }
            }

            return null;
        }
    }
}
