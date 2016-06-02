// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.Utilities;
using Xunit;
using ILOpCode = Microsoft.CodeAnalysis.CodeGen.ILOpCode;

namespace Microsoft.CodeAnalysis.UnitTests
{
    using Roslyn.Reflection;
    using Roslyn.Reflection.Metadata.Ecma335.Blobs;

    public class MethodBodyEncoderTests
    {
        private static unsafe MethodBodyBlock ReadMethodBody(byte[] body)
        {
            fixed (byte* bodyPtr = &body[0])
            {
                return MethodBodyBlock.Create(new BlobReader(bodyPtr, body.Length));
            }
        }

        private static void WriteFakeILWithBranches(BlobBuilder builder, BranchBuilder branchBuilder, int size)
        {
            Assert.Equal(0, builder.Count);

            const byte filling = 0x01;
            int ilOffset = 0;
            foreach (var branch in branchBuilder.Branches)
            {
                builder.WriteBytes(filling, branch.ILOffset - ilOffset);

                Assert.Equal(branch.ILOffset, builder.Count);
                builder.WriteByte(branch.ShortOpCode);
                builder.WriteByte(0xff);

                ilOffset = branch.ILOffset + 2;
            }

            builder.WriteBytes(filling, size - ilOffset);
            Assert.Equal(size, builder.Count);
        }

        [Fact]
        public void TinyBody()
        {
            var bodyBuilder = new BlobBuilder();
            var codeBuilder = new BlobBuilder();
            var branchBuilder = new BranchBuilder();
            var il = new InstructionEncoder(codeBuilder, branchBuilder);

            var bodyEncoder = new MethodBodyEncoder(
                bodyBuilder, 
                maxStack: 2, 
                exceptionRegionCount: 0, 
                localVariablesSignature: default(StandaloneSignatureHandle), 
                attributes: MethodBodyAttributes.None);

            Assert.True(bodyEncoder.IsTiny(10));
            Assert.True(bodyEncoder.IsTiny(63));
            Assert.False(bodyEncoder.IsTiny(64));

            codeBuilder.WriteBytes(1, 61);
            var l1 = il.DefineLabel();
            il.MarkLabel(l1);

            Assert.Equal(61, branchBuilder.Labels.Single());

            il.Branch(ILOpCode.Br, l1);

            var brInfo = branchBuilder.Branches.Single();
            Assert.Equal(61, brInfo.ILOffset);
            Assert.Equal(l1, brInfo.Label);
            Assert.Equal((byte)ILOpCode.Br_s, brInfo.ShortOpCode);

            AssertEx.Equal(new byte[] { 1, (byte)ILOpCode.Br_s, unchecked((byte)-1) }, codeBuilder.ToArray(60, 3));

            int bodyOffset;
            bodyEncoder.WriteInstructions(codeBuilder, branchBuilder, out bodyOffset);

            var bodyBytes = bodyBuilder.ToArray();

            AssertEx.Equal(new byte[] 
            {
                0xFE, // tiny header
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
            }, bodyBytes);

            var body = ReadMethodBody(bodyBytes);
            Assert.Equal(0, body.ExceptionRegions.Length);
            Assert.Equal(default(StandaloneSignatureHandle), body.LocalSignature);
            Assert.Equal(8, body.MaxStack);
            Assert.Equal(bodyBytes.Length, body.Size);

            var ilBytes = body.GetILBytes();
            AssertEx.Equal(new byte[]
            {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
            }, ilBytes);
        }

        [Fact]
        public void FatBody()
        {
            var bodyBuilder = new BlobBuilder();
            var codeBuilder = new BlobBuilder();
            var branchBuilder = new BranchBuilder();
            var il = new InstructionEncoder(codeBuilder, branchBuilder);

            var bodyEncoder = new MethodBodyEncoder(
                bodyBuilder,
                maxStack: 2,
                exceptionRegionCount: 0,
                localVariablesSignature: default(StandaloneSignatureHandle),
                attributes: MethodBodyAttributes.None);

            Assert.True(bodyEncoder.IsTiny(10));
            Assert.True(bodyEncoder.IsTiny(63));
            Assert.False(bodyEncoder.IsTiny(64));

            codeBuilder.WriteBytes(1, 62);
            var l1 = il.DefineLabel();
            il.MarkLabel(l1);

            Assert.Equal(62, branchBuilder.Labels.Single());

            il.Branch(ILOpCode.Br, l1);

            var brInfo = branchBuilder.Branches.Single();
            Assert.Equal(62, brInfo.ILOffset);
            Assert.Equal(l1, brInfo.Label);
            Assert.Equal((byte)ILOpCode.Br_s, brInfo.ShortOpCode);

            AssertEx.Equal(new byte[] { 1, 1, (byte)ILOpCode.Br_s, unchecked((byte)-1) }, codeBuilder.ToArray(60, 4));

            int bodyOffset;
            bodyEncoder.WriteInstructions(codeBuilder, branchBuilder, out bodyOffset);

            var bodyBytes = bodyBuilder.ToArray();

            AssertEx.Equal(new byte[]
            {
                0x03, 0x30, 0x02, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // fat header
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
            }, bodyBytes);

            var body = ReadMethodBody(bodyBytes);
            Assert.Equal(0, body.ExceptionRegions.Length);
            Assert.Equal(default(StandaloneSignatureHandle), body.LocalSignature);
            Assert.Equal(2, body.MaxStack);
            Assert.Equal(bodyBytes.Length, body.Size);

            var ilBytes = body.GetILBytes();
            AssertEx.Equal(new byte[]
            {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
            }, ilBytes);
        }

        [Fact]
        public void Branches1()
        {
            var branchBuilder = new BranchBuilder();

            var l0 = branchBuilder.AddLabel();
            var l64 = branchBuilder.AddLabel();
            var l255 = branchBuilder.AddLabel();

            branchBuilder.MarkLabel(0, l0);
            branchBuilder.MarkLabel(64, l64);
            branchBuilder.MarkLabel(255, l255);

            branchBuilder.AddBranch(0, l255, (byte)ILOpCode.Bge_s);
            branchBuilder.AddBranch(16, l0, (byte)ILOpCode.Bge_un_s); // blob boundary
            branchBuilder.AddBranch(33, l255, (byte)ILOpCode.Ble_s); // blob boundary
            branchBuilder.AddBranch(35, l0, (byte)ILOpCode.Ble_un_s); // branches immediately next to each other
            branchBuilder.AddBranch(37, l255, (byte)ILOpCode.Blt_s); // branches immediately next to each other
            branchBuilder.AddBranch(40, l64, (byte)ILOpCode.Blt_un_s);
            branchBuilder.AddBranch(254, l0, (byte)ILOpCode.Brfalse_s); // long branch at the end

            var dstBuilder = new BlobBuilder();
            var srcBuilder = new BlobBuilder(size: 17);
            WriteFakeILWithBranches(srcBuilder, branchBuilder, size: 256);

            branchBuilder.FixupBranches(srcBuilder, dstBuilder);

            AssertEx.Equal(new byte[] 
            {
                (byte)ILOpCode.Bge, 0xFA, 0x00, 0x00, 0x00,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                (byte)ILOpCode.Bge_un_s, 0xEE,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                (byte)ILOpCode.Ble, 0xD9, 0x00, 0x00, 0x00,
                (byte)ILOpCode.Ble_un_s, 0xDB,
                (byte)ILOpCode.Blt, 0xD5, 0x00, 0x00, 0x00,
                0x01,
                (byte)ILOpCode.Blt_un_s, 0x16,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01,
                (byte)ILOpCode.Brfalse, 0xFD, 0xFE, 0xFF, 0xFF,
            }, dstBuilder.ToArray());
        }

        [Fact]
        public void BranchErrors()
        {
            var codeBuilder = new BlobBuilder();

            var il = new InstructionEncoder(codeBuilder);
            Assert.Throws<InvalidOperationException>(() => il.DefineLabel());

            il = new InstructionEncoder(codeBuilder, new BranchBuilder());
            il.DefineLabel();
            il.DefineLabel();
            var l2 = il.DefineLabel();

            var branchBuilder = new BranchBuilder();
            il = new InstructionEncoder(codeBuilder, branchBuilder);
            var l0 = il.DefineLabel();

            Assert.Throws<ArgumentException>(() => il.Branch(ILOpCode.Nop, l0));
            Assert.Throws<ArgumentNullException>(() => il.Branch(ILOpCode.Br, default(LabelHandle)));
            Assert.Throws<ArgumentException>(() => il.Branch(ILOpCode.Br, l2));
        }
    }
}
