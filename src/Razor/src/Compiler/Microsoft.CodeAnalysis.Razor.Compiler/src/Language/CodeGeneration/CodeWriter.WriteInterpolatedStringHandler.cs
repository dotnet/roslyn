// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public sealed partial class CodeWriter
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteInterpolatedStringHandler
    {
        private readonly CodeWriter _writer;

        public WriteInterpolatedStringHandler(int literalLength, int formattedCount, CodeWriter writer)
        {
            _writer = writer;
        }

        public void AppendLiteral(string value)
            => _writer.Write(value);

        public void AppendFormatted(ReadOnlyMemory<char> value)
            => _writer.Write(value);

        public void AppendFormatted(string? value)
        {
            if (value is not null)
            {
                _writer.Write(value);
            }
        }

        public void AppendFormatted<T>(T value)
        {
            if (value is null)
            {
                return;
            }

            switch (value)
            {
                case ReadOnlyMemory<char> memory:
                    _writer.Write(memory);
                    break;

                case string s:
                    _writer.Write(s);
                    break;

                case BuilderVariableName name:
                    name.WriteTo(_writer);
                    break;

                case RenderModeVariableName name:
                    name.WriteTo(_writer);
                    break;

                case FormNameVariableName name:
                    name.WriteTo(_writer);
                    break;

                case ComponentNodeWriter.SeqName name:
                    name.WriteTo(_writer);
                    break;

                case ComponentNodeWriter.ParameterName name:
                    name.WriteTo(_writer);
                    break;

                case ComponentNodeWriter.TypeInferenceArgName name:
                    name.WriteTo(_writer);
                    break;

                case IWriteableValue writeableValue:
                    Debug.Assert(!typeof(T).IsValueType, $"Handle {typeof(T).FullName} to avoid boxing to {nameof(IWriteableValue)}");
                    writeableValue.WriteTo(_writer);
                    break;

                default:
                    _writer.Write(value.ToString() ?? string.Empty);
                    break;
            }
        }
    }
}
