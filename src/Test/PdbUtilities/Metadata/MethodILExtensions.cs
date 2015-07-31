// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace Roslyn.Test.MetadataUtilities
{
    public static class MethodILExtensions
    {
        public static unsafe string GetMethodIL(this ImmutableArray<byte> ilArray)
        {
            var result = new StringBuilder();
            fixed (byte* ilPtr = ilArray.ToArray())
            {
                int offset = 0;
                while (true)
                {
                    // skip padding:
                    while (offset < ilArray.Length && ilArray[offset] == 0)
                    {
                        offset++;
                    }

                    if (offset == ilArray.Length)
                    {
                        break;
                    }

                    var reader = new BlobReader(ilPtr + offset, ilArray.Length - offset);
                    var methodIL = MethodBodyBlock.Create(reader);

                    if (methodIL == null)
                    {
                        result.AppendFormat("<invalid byte 0x{0:X2} at offset {1}>", ilArray[offset], offset);
                        offset++;
                    }
                    else
                    {
                        ILVisualizerAsTokens.Instance.DumpMethod(
                            result,
                            methodIL.MaxStack,
                            methodIL.GetILContent(),
                            ImmutableArray.Create<ILVisualizer.LocalInfo>(),
                            ImmutableArray.Create<ILVisualizer.HandlerSpan>());

                        offset += methodIL.Size;
                    }
                }
            }

            return result.ToString();
        }
    }
}
