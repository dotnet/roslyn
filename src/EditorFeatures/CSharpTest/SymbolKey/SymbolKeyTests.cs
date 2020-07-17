// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId
{
    [UseExportProvider]
    public class SymbolKeyTests
    {
        [Fact, WorkItem(45437, "https://github.com/dotnet/roslyn/issues/45437")]
        public async Task TestGenericsAndNullability()
        {
            var typeSource = @"
#nullable enable

    public sealed class ConditionalWeakTableTest<TKey, TValue> /*: IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable*/
        where TKey : class
        where TValue : class
    {
        public ConditionalWeakTable() { }
        public void Add(TKey key, TValue value) { }
        public void AddOrUpdate(TKey key, TValue value) { }
        public void Clear() { }
        public TValue GetOrCreateValue(TKey key) => default;
        public TValue GetValue(TKey key, ConditionalWeakTableTest<TKey, TValue>.CreateValueCallback createValueCallback) => default;
        public bool Remove(TKey key) => false;

        public delegate TValue CreateValueCallback(TKey key);
    }".Replace("<", "&lt;").Replace(">", "&gt;");

            var workspaceXml = @$"
<Workspace>
    <Project Language=""C#"">
        <CompilationOptions Nullable=""Enable""/>
        <Document FilePath=""C.cs"">
{typeSource}
        </Document>
    </Project>
</Workspace>
";
            using var workspace = TestWorkspace.Create(workspaceXml);

            var solution = workspace.CurrentSolution;
            var project = solution.Projects.Single();

            var compilation = await project.GetCompilationAsync();

            var type = compilation.GetTypeByMetadataName("ConditionalWeakTableTest`2");
            var method = type.GetMembers("GetValue").OfType<IMethodSymbol>().Single();
            var callbackParamater = method.Parameters[1];
            var parameterType = callbackParamater.Type;
            Assert.Equal("global::ConditionalWeakTableTest<TKey!, TValue!>.CreateValueCallback!", parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier)));

            var symbolKey = SymbolKey.Create(method);
            var resolved = symbolKey.Resolve(compilation).Symbol;

            Assert.Equal(method, resolved);
        }
    }
}
