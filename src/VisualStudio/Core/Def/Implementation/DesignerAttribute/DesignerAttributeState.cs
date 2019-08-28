// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionCrawler.State;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal partial class DesignerAttributeIncrementalAnalyzer : IIncrementalAnalyzer
    {
        private class DesignerAttributeState : AbstractDocumentAnalyzerState<Data>
        {
            private const string FormatVersion = "2";

            /// <summary>
            /// remember last time what we reported
            /// </summary>
            private readonly ConcurrentDictionary<DocumentId, string> _lastReported = new ConcurrentDictionary<DocumentId, string>(concurrencyLevel: 2, capacity: 10);

            public bool Update(DocumentId id, string designerAttributeArgument)
            {
                if (_lastReported.TryGetValue(id, out var lastReported) &&
                    lastReported == designerAttributeArgument)
                {
                    // nothing is actually updated
                    return false;
                }

                // value updated
                _lastReported[id] = designerAttributeArgument;
                return true;
            }

            protected override string StateName
            {
                get
                {
                    return "<DesignerAttribute>";
                }
            }

            protected override int GetCount(Data data)
            {
                return 1;
            }

            protected override Data TryGetExistingData(Stream stream, Document value, CancellationToken cancellationToken)
            {
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        var format = reader.ReadString();
                        if (string.Equals(format, FormatVersion, StringComparison.InvariantCulture))
                        {
                            var textVersion = VersionStamp.ReadFrom(reader);
                            var dataVersion = VersionStamp.ReadFrom(reader);
                            var designerAttributeArgument = reader.ReadString();

                            return new Data(textVersion, dataVersion, designerAttributeArgument);
                        }
                    }
                }

                return null;
            }

            protected override void WriteTo(Stream stream, Data data, CancellationToken cancellationToken)
            {
                using var writer = new ObjectWriter(stream, cancellationToken: cancellationToken);
                writer.WriteString(FormatVersion);
                data.TextVersion.WriteTo(writer);
                data.SemanticVersion.WriteTo(writer);
                writer.WriteString(data.DesignerAttributeArgument);
            }

            public override bool Remove(DocumentId id)
            {
                // forget what I have reported
                _lastReported.TryRemove(id, out _);

                return base.Remove(id);
            }
        }
    }
}
