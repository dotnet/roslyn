using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal class MethodILBuilder
    {
        private Microsoft.Cci.MemoryStream ilBits;
        private Microsoft.Cci.BinaryWriter writer;
        private Emit.Module moduleBeingBuilt;

        private List<Microsoft.Cci.SequencePoint> seqPoints;
        public Microsoft.Cci.SequencePoint[] GetSequencePoints()
        {
            return seqPoints.ToArray();
        }

        public MethodILBuilder(Emit.Module moduleBeingBuilt)
        {
            this.ilBits = new Microsoft.Cci.MemoryStream();
            this.writer = new Microsoft.Cci.BinaryWriter(this.ilBits);
            this.moduleBeingBuilt = moduleBeingBuilt;
        }

        public Microsoft.Cci.MemoryStream Bits
        {
            get
            {
                return this.ilBits;
            }
        }

        /// <summary>
        /// The next instruction emitted is w/in the scope. Use the returned token to close the scope.
        /// </summary>
        /// <returns></returns>
        public int BeginScope()
        {
            return 0;
        }

        /// <summary>
        /// The previously emitted instruction was the last one in the scope of the token submitted.
        /// </summary>
        /// <param name="tok"></param>
        public void EndScope(int tok)
        {
        }

        public void DefineSeqPoint(uint beginLine, uint beginColumn, uint endLine, uint endColumn,
            Microsoft.Cci.DebugSourceDocument document)
        {
            if (seqPoints == null)
            {
                seqPoints = new List<Microsoft.Cci.SequencePoint>();
            }

            seqPoints.Add(new Microsoft.Cci.SequencePoint()
            {
                Offset = writer.BaseStream.Position,
                StartLine = beginLine,
                StartColumn = beginColumn,
                EndLine = endLine,
                EndColumn = endColumn,
                PrimarySourceDocument = document
            });
        }

        public void PutOpcode(Microsoft.Cci.OperationCode opcode)
        {
            if (opcode < Microsoft.Cci.OperationCode.Arglist)
            {
                writer.WriteByte((byte)opcode);
            }
            else
            {
                writer.WriteByte((byte)((ushort)opcode >> 8));
                writer.WriteByte((byte)((ushort)opcode & 0xff));
            }
        }

        public void PutOpcode(Microsoft.Cci.OperationCode opcode, int target)
        {
            PutOpcode(opcode);
            switch (opcode)
            {
                case Microsoft.Cci.OperationCode.Beq:
                case Microsoft.Cci.OperationCode.Bge:
                case Microsoft.Cci.OperationCode.Bge_Un:
                case Microsoft.Cci.OperationCode.Bgt:
                case Microsoft.Cci.OperationCode.Bgt_Un:
                case Microsoft.Cci.OperationCode.Ble:
                case Microsoft.Cci.OperationCode.Ble_Un:
                case Microsoft.Cci.OperationCode.Blt:
                case Microsoft.Cci.OperationCode.Blt_Un:
                case Microsoft.Cci.OperationCode.Bne_Un:
                case Microsoft.Cci.OperationCode.Br:
                case Microsoft.Cci.OperationCode.Brfalse:
                case Microsoft.Cci.OperationCode.Brtrue:
                case Microsoft.Cci.OperationCode.Leave:
                case Microsoft.Cci.OperationCode.Ldc_I4:
                    // ^ assume operation.Value is int;
                    writer.WriteInt(target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void PutOpcode(Microsoft.Cci.OperationCode opcode, Symbol symbol)
        {
            PutOpcode(opcode);

            var opcodeInfo = Microsoft.Cci.PeWriter.OperationCodeInfo(opcode);

            switch (opcodeInfo.OperandType)
            {
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    writer.WriteUint(moduleBeingBuilt.GetFakeSymbolTokenForIL(moduleBeingBuilt.Translate(symbol)));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void PutOpcode(Microsoft.Cci.OperationCode opcode, string str)
        {
            PutOpcode(opcode);

            var opcodeInfo = Microsoft.Cci.PeWriter.OperationCodeInfo(opcode);

            switch (opcodeInfo.OperandType)
            {
                case OperandType.InlineString:
                    writer.WriteUint(moduleBeingBuilt.GetFakeStringTokenForIL(str));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}