using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed partial class LegacyEditorConfigDocumentOptionsProvider
    {
        private class EmptyCodingConventionContext : ICodingConventionContext
        {
            public static ICodingConventionContext Instance { get; } = new EmptyCodingConventionContext();

            public ICodingConventionsSnapshot CurrentConventions { get; } = EmptyCodingConventionsSnapshot.Instance;

            event CodingConventionsChangedAsyncEventHandler ICodingConventionContext.CodingConventionsChangedAsync
            {
                add { }
                remove { }
            }

            public void Dispose() { }

            public Task WriteConventionValueAsync(string conventionName, string conventionValue, CancellationToken cancellationToken)
                => Task.CompletedTask;

            private class EmptyCodingConventionsSnapshot : ICodingConventionsSnapshot
            {
                public static EmptyCodingConventionsSnapshot Instance { get; } = new EmptyCodingConventionsSnapshot();

                public IReadOnlyDictionary<string, object> AllRawConventions { get; } =
                    (IReadOnlyDictionary<string, object>)SpecializedCollections.EmptyDictionary<string, object>();

                public IUniversalCodingConventions UniversalConventions { get; } = EmptyUniversalCodingConventions.Instance;

                public int Version => 0;

                public bool TryGetConventionValue<T>(string conventionName, out T conventionValue)
                {
                    conventionValue = default;
                    return false;
                }

                private class EmptyUniversalCodingConventions : IUniversalCodingConventions
                {
                    public static EmptyUniversalCodingConventions Instance { get; } = new EmptyUniversalCodingConventions();

                    public bool TryGetAllowTrailingWhitespace(out bool allowTrailingWhitespace)
                    {
                        allowTrailingWhitespace = false;
                        return false;
                    }

                    public bool TryGetEncoding(out Encoding encoding)
                    {
                        encoding = null;
                        return false;
                    }

                    public bool TryGetIndentSize(out int indentSize)
                    {
                        indentSize = default;
                        return false;
                    }

                    public bool TryGetIndentStyle(out IndentStyle indentStyle)
                    {
                        indentStyle = default;
                        return false;
                    }

                    public bool TryGetLineEnding(out string lineEnding)
                    {
                        lineEnding = null;
                        return false;
                    }

                    public bool TryGetRequireFinalNewline(out bool requireFinalNewline)
                    {
                        requireFinalNewline = false;
                        return false;
                    }

                    public bool TryGetTabWidth(out int tabWidth)
                    {
                        tabWidth = default;
                        return false;
                    }
                }
            }
        }
    }
}
