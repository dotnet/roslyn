// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    /// <summary>
    /// A line of text that was parsed by <see cref="StackTraceAnalyzer" />
    /// to provide metadata bout the line. Expected to be the parsed output 
    /// of a serialized <see cref="StackFrame"/>
    /// </summary>
    internal sealed class ParsedStackFrame : ParsedFrame
    {
        public ParsedStackFrame(
            string originalText,
            TextSpan typeSpan,
            TextSpan methodSpan,
            TextSpan argsSpan,
            TextSpan fileSpan = default)
            : base(originalText)
        {
            Contract.ThrowIfTrue(typeSpan.IsEmpty);
            Contract.ThrowIfTrue(methodSpan.IsEmpty);

            TypeSpan = typeSpan;
            MethodSpan = methodSpan;
            ArgsSpan = argsSpan;
            FileSpan = fileSpan;
        }

        /// <summary>
        /// The full type name parsed from the line. 
        /// ex: [|Microsoft.CodeAnalysis.Editor.CallstackExplorer.|]Example(arg1, arg2)
        /// </summary>
        public TextSpan TypeSpan { get; }

        /// <summary>
        /// The method name span
        /// ex: Microsoft.CodeAnalysis.Editor.CallstackExplorer.[|Example|](arg1, arg2)
        /// </summary>
        public TextSpan MethodSpan { get; }

        /// <summary>
        /// The span of comma seperated arguments.
        /// ex: Microsoft.CodeAnalysis.Editor.CallstackExplorer.Example[|(arg1, arg2)|]
        /// </summary>
        public TextSpan ArgsSpan { get; }

        /// <summary>
        /// The span representing file information on the stack trace line. Is not always available, so it's 
        /// possible this span is <see langword="default"/>
        /// </summary>
        public TextSpan FileSpan { get; }

        public async Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
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

                var metadataName = service.GetTypeMetadataName(fullyQualifiedTypeName);
                var memberName = service.GetMethodSymbolName(methodName);

                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var type = compilation.GetTypeByMetadataName(metadataName);
                if (type is null)
                {
                    continue;
                }

                var members = type.GetMembers();
                var matchingMembers = members
                    .OfType<IMethodSymbol>()
                    .Where(m => MemberMatchesMethodName(m, memberName))
                    .ToImmutableArrayOrEmpty();

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

            // TODO: Improve perf here. ToDisplayString is fairly expensive
            static bool MemberMatchesMethodName(ISymbol member, string memberToSearchFor)
            {
                var displayName = member.ToDisplayString();
                var dotIndex = displayName.LastIndexOf(".");
                var memberName = dotIndex >= 0
                    ? displayName[(dotIndex + 1)..]
                    : displayName;

                return string.Equals(memberName, memberToSearchFor);
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
        /// Gets the text after the last parsed span available. This is after
        /// file information if it is available, otherwise after the argument information.
        /// </summary>
        public string GetTrailingText()
        {
            var lastSpan = FileSpan == default
                ? ArgsSpan
                : FileSpan;

            if (lastSpan.End + 1 == OriginalText.Length)
            {
                return string.Empty;
            }

            return OriginalText[(lastSpan.End + 1)..];
        }

        /// <summary>
        /// If the frame has file information, gets the text between the method and file information.
        /// ex: at ConsoleApp4.MyClass.M[T](T t) in [|C:\repos\Test\MyClass.cs:line 7|]
        /// </summary>
        public string? GetFileText()
        {
            if (FileSpan == default)
            {
                return null;
            }

            return OriginalText[FileSpan.Start..FileSpan.End];
        }

        /// <summary>
        /// If the frame has file information, gets the text between the method and file information.
        /// ex: at ConsoleApp4.MyClass.M[T](T t)[| in |]C:\repos\Test\MyClass.cs:line 7
        /// </summary>
        public string? GetTextBetweenTypeAndFile()
        {
            if (FileSpan == default)
            {
                return null;
            }

            return OriginalText[(ArgsSpan.End + 1)..FileSpan.Start];
        }

        internal (Document? document, int line) GetDocumentAndLine(Solution solution)
        {
            var fileMatches = GetFileMatches(solution, out var lineNumber);
            if (fileMatches.IsEmpty)
            {
                return (null, 0);
            }

            return (fileMatches.First(), lineNumber);
        }

        /// <summary>
        /// If the <see cref="FileSpan"/> exists it attempts to map the file path that exists
        /// in that span to a document in the solution. It's possible the file path won't match exactly,
        /// so it does a best effort to match based on name.
        /// </summary>
        private ImmutableArray<Document> GetFileMatches(Solution solution, out int lineNumber)
        {
            var fileText = OriginalText[FileSpan.Start..FileSpan.End];
            var regex = new Regex(@"(?<fileName>.+):(line)\s*(?<lineNumber>[0-9]+)");
            var match = regex.Match(fileText);
            Debug.Assert(match.Success);

            var fileNameGroup = match.Groups["fileName"];
            var lineNumberGroup = match.Groups["lineNumber"];

            lineNumber = int.Parse(lineNumberGroup.Value);

            var fileName = fileNameGroup.Value;
            Debug.Assert(!string.IsNullOrEmpty(fileName));

            var documentName = Path.GetFileName(fileName);
            var potentialMatches = new HashSet<Document>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == fileName)
                    {
                        return ImmutableArray.Create(document);
                    }

                    else if (document.Name == documentName)
                    {
                        potentialMatches.Add(document);
                    }
                }
            }

            return potentialMatches.ToImmutableArray();
        }
    }
}
