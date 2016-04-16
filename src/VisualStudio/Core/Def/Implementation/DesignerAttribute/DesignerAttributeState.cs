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
                try
                {
                    using (var reader = new ObjectReader(stream))
                    {
                        var format = reader.ReadString();
                        if (!string.Equals(format, FormatVersion))
                        {
                            return null;
                        }

                        var textVersion = VersionStamp.ReadFrom(reader);
                        var dataVersion = VersionStamp.ReadFrom(reader);
                        var designerAttributeArgument = reader.ReadString();

                        return new Data(textVersion, dataVersion, designerAttributeArgument);
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            protected override void WriteTo(Stream stream, Data data, CancellationToken cancellationToken)
            {
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
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
