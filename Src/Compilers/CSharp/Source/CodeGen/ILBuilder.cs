// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal class ILBuilder
    {
        // Roslyn Code (calls CCI code below) -----------------------------------------------------------------------

        private LocalSlotManager slotManager = new LocalSlotManager();
        private List<object> stream = new List<object>(); // it's kind of dumb to use object here; fix this.
        private int codesize = 0;

        public void AddOpCode(ILOpCode code)
        {
            stream.Add(code);
            this.PutOpcode(code);
            codesize += code.Size();
        }

        internal void AddInt8(sbyte int8)
        {
            stream.Add(int8);
            this.PutInt8(int8);
            codesize += 1;
        }

        internal void AddInt32(int int32)
        {
            stream.Add(int32);
            this.PutInt32(int32);
            codesize += 4;
        }

        internal void AddInt64(long int64)
        {
            stream.Add(int64);
            this.PutInt64(int64);
            codesize += 8;
        }

        internal void AddFloat(float floatValue)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            this.AddInt32(BitConverter.ToInt32(BitConverter.GetBytes(floatValue), 0));
        }

        internal void AddDouble(double doubleValue)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            this.AddInt64(BitConverter.DoubleToInt64Bits(doubleValue));
        }

        internal void AddString(string stringValue)
        {
            Debug.Assert(stringValue != null);

            // TODO--need to turn this guy into a token in conjuction with emitter
            stream.Add(stringValue);
            this.PutString(stringValue);
            codesize += 4; // <token>
        }

        internal void AddMethod(MethodSymbol methodSymbol)
        {
            Debug.Assert(methodSymbol != null);

            // TODO--need to turn this guy into a token in conjuction with emitter
            stream.Add(methodSymbol);
            this.PutSymbol(methodSymbol);
            codesize += 4; // <token>
        }

        internal void AddField(FieldSymbol fieldSymbol)
        {
            Debug.Assert(fieldSymbol != null);

            // TODO--need to turn this guy into a token in conjuction with emitter
            stream.Add(fieldSymbol);
            this.PutSymbol(fieldSymbol);
            codesize += 4; // <token>
        }

        internal void AddType(TypeSymbol typeSymbol)
        {
            Debug.Assert(typeSymbol != null);

            // TODO--need to turn this guy into a token in conjuction with emitter
            stream.Add(typeSymbol);
            this.PutSymbol(typeSymbol);
            codesize += 4; // <token>
        }

        public int CodeSize
        {
            get { return this.codesize; }
        }

        internal ushort MaxStack
        {
            get { return 8; }
        }

        public IList<object> GetStream()
        {
            return stream.AsReadOnly();
        }

        public void BeginBlock(Symbol labelSymbol)
        {
        }

        public LocalSlotManager GetSlotManager()
        {
            return this.slotManager;
        }

        public override string ToString()
        {
            return new ILVisualizer(this, this.slotManager).Visualize();
        }

        // CCI Code ----------------------------------------------------------------------------------

        private Microsoft.Cci.MemoryStream ilBits;
        private Microsoft.Cci.BinaryWriter writer;
        private Emit.Module moduleBeingBuilt;

        private List<Microsoft.Cci.SequencePoint> seqPoints = new List<Microsoft.Cci.SequencePoint>();
        public Microsoft.Cci.SequencePoint[] GetSequencePoints()
        {
            return seqPoints.ToArray();
        }

        public ILBuilder(Emit.Module moduleBeingBuilt)
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

        public void PutOpcode(ILOpCode code)
        {
            if (code.Size() == 1)
            {
                writer.WriteByte((byte)code);
            }
            else
            {
                Debug.Assert(code.Size() == 2);
                writer.WriteByte((byte)((ushort)code >> 8));
                writer.WriteByte((byte)((ushort)code & 0xff));
            }
        }

        public void PutInt8(sbyte b)
        {
            writer.WriteSbyte(b);
        }

        public void PutInt32(int i)
        {
            writer.WriteInt(i);
        }

        public void PutInt64(long l)
        {
            writer.WriteLong(l);
        }

        public void PutSymbol(Symbol symbol)
        {
            if (moduleBeingBuilt != null)
            {
                writer.WriteUint(moduleBeingBuilt.GetFakeSymbolTokenForIL(moduleBeingBuilt.Translate(symbol)));
            }
        }

        public void PutString(string str)
        {
            if (moduleBeingBuilt != null)
            {
                writer.WriteUint(moduleBeingBuilt.GetFakeStringTokenForIL(str));
            }
        }

        public LocalDefinition[] GetLocalDefinitions()
        {
            LocalDefinition[] result = new LocalDefinition[slotManager.NumSlots];

            if (moduleBeingBuilt != null)
            {
                for (int i = 0; i < slotManager.NumSlots; ++i)
                {
                    var local = slotManager.LocalAtSlot(i);
                    result[i] = new LocalDefinition(local.Name, moduleBeingBuilt.Translate(local.Type));
                }
            }

            return result;
        }
    }
}
