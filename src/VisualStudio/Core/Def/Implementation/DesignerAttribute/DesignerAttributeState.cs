// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionCrawler.State;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal abstract partial class AbstractDesignerAttributeIncrementalAnalyzer : IIncrementalAnalyzer
    {
        private class DesignerAttributeState : AbstractDocumentAnalyzerState<Data>
        {
            private const string FormatVersion = "1";

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
                using (var reader = StreamObjectReader.TryGetReader(stream))
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
                using (var writer = new StreamObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    writer.WriteString(FormatVersion);
                    data.TextVersion.WriteTo(writer);
                    data.SemanticVersion.WriteTo(writer);
                    writer.WriteString(data.DesignerAttributeArgument);
                }
            }
        }
    }
}
