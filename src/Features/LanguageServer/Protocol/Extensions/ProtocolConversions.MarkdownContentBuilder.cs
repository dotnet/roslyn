// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static partial class ProtocolConversions
    {
        private readonly struct MarkdownContentBuilder : IDisposable
        {
            private readonly ArrayBuilder<string> _linesBuilder;

            public MarkdownContentBuilder()
            {
                _linesBuilder = ArrayBuilder<string>.GetInstance();
            }

            public void Append(string text)
            {
                if (_linesBuilder.Count == 0)
                {
                    _linesBuilder.Add(text);
                }
                else
                {
                    _linesBuilder[^1] = _linesBuilder[^1] + text;
                }
            }

            public void AppendLine(string text = "")
            {
                _linesBuilder.Add(text);
            }

            public bool IsLineEmpty()
            {
                return _linesBuilder.Count == 0 ? true : string.IsNullOrEmpty(_linesBuilder[^1]);
            }

            public string Build()
            {
                return string.Join(Environment.NewLine, _linesBuilder);
            }

            public void Dispose()
            {
                _linesBuilder.Free();
            }
        }
    }
}
