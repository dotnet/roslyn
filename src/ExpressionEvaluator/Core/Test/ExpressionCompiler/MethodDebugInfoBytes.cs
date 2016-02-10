// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;
using Microsoft.DiaSymReader;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class MethodDebugInfoBytes
    {
        public readonly ImmutableArray<byte> Bytes;
        public readonly ISymUnmanagedMethod Method;

        public MethodDebugInfoBytes(ImmutableArray<byte> bytes, ISymUnmanagedMethod method)
        {
            this.Bytes = bytes;
            this.Method = method;
        }

        /// <remarks>
        /// This is a helper class for creating mostly-correct <see cref="MethodDebugInfoBytes"/> objects (e.g. circular forwards, extra records, etc).
        /// To create totally broken objects (e.g. corrupted bytes, alternate scope structures, etc), construct <see cref="MethodDebugInfoBytes"/> objects directly.
        /// </remarks>
        internal sealed class Builder
        {
            private const byte Version = 4;
            private const byte Padding = 0;

            private readonly ISymUnmanagedMethod _method;

            private ArrayBuilder<byte> _bytesBuilder;
            private int _recordCount;

            public Builder(string[][] importStringGroups = null, bool suppressUsingInfo = false, ISymUnmanagedConstant[] constants = null)
            {
                _bytesBuilder = ArrayBuilder<byte>.GetInstance();
                if (importStringGroups != null && !suppressUsingInfo)
                {
                    var groupSizes = importStringGroups.Select(g => (short)g.Length).ToArray();
                    AddUsingInfo(groupSizes);
                }

                var namespaces = importStringGroups == null
                    ? default(ImmutableArray<ISymUnmanagedNamespace>)
                    : importStringGroups.SelectMany(names => names.Select(name => (ISymUnmanagedNamespace)new MockSymUnmanagedNamespace(name))).ToImmutableArray();
                var childScope = new MockSymUnmanagedScope(default(ImmutableArray<ISymUnmanagedScope>), namespaces, constants);
                var rootScope = new MockSymUnmanagedScope(ImmutableArray.Create<ISymUnmanagedScope>(childScope), default(ImmutableArray<ISymUnmanagedNamespace>));
                _method = new MockSymUnmanagedMethod(rootScope);
            }

            public Builder AddUsingInfo(params short[] groupSizes)
            {
                var numGroupSizes = groupSizes.Length;
                var recordSize = BitArithmeticUtilities.Align(4 + 4 + 2 + 2 * numGroupSizes, 4); // Record size, including header.

                // Record header
                _bytesBuilder.Add(Version);
                _bytesBuilder.Add((byte)CustomDebugInfoKind.UsingInfo);
                _bytesBuilder.Add(Padding);
                _bytesBuilder.Add(Padding);
                _bytesBuilder.Add4(recordSize);

                // Record body
                _bytesBuilder.Add2((short)numGroupSizes);
                foreach (var groupSize in groupSizes)
                {
                    _bytesBuilder.Add2(groupSize);
                }

                if ((_bytesBuilder.Count % 4) != 0)
                {
                    _bytesBuilder.Add2(0);
                }

                Assert.Equal(0, _bytesBuilder.Count % 4);
                _recordCount++;
                return this;
            }

            public Builder AddForward(int targetToken)
            {
                return AddForward(targetToken, isModuleLevel: false);
            }

            public Builder AddModuleForward(int targetToken)
            {
                return AddForward(targetToken, isModuleLevel: true);
            }

            private Builder AddForward(int targetToken, bool isModuleLevel)
            {
                // Record header
                _bytesBuilder.Add(Version);
                _bytesBuilder.Add((byte)(isModuleLevel ? CustomDebugInfoKind.ForwardToModuleInfo : CustomDebugInfoKind.ForwardInfo));
                _bytesBuilder.Add(Padding);
                _bytesBuilder.Add(Padding);
                _bytesBuilder.Add4(12); // Record size, including header.

                // Record body
                _bytesBuilder.Add4(targetToken);

                Assert.Equal(0, _bytesBuilder.Count % 4);
                _recordCount++;
                return this;
            }

            public MethodDebugInfoBytes Build()
            {
                // Global header
                _bytesBuilder.Insert(0, Version);
                _bytesBuilder.Insert(1, (byte)_recordCount);
                _bytesBuilder.Insert(2, Padding);
                _bytesBuilder.Insert(3, Padding);

                Assert.Equal(0, _bytesBuilder.Count % 4);

                var info = new MethodDebugInfoBytes(_bytesBuilder.ToImmutableAndFree(), _method);
                _bytesBuilder = null; // We'll blow up if any other methods are called.
                return info;
            }
        }
    }
}
