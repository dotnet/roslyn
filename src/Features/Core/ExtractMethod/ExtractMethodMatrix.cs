// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class ExtractMethodMatrix
    {
        private static readonly Dictionary<Key, VariableStyle> matrix;

        static ExtractMethodMatrix()
        {
            matrix = new Dictionary<Key, VariableStyle>();
            BuildMatrix();
        }

        public static VariableStyle GetVariableStyle(
            bool captured,
            bool dataFlowIn,
            bool dataFlowOut,
            bool alwaysAssigned,
            bool variableDeclared,
            bool readInside,
            bool writtenInside,
            bool readOutside,
            bool writtenOutside,
            bool unsafeAddressTaken)
        {
#if false
            // decide not to treat capture variable special
            if (captured)
            {
                // if a variable is captured, it can only be passed as ref parameter.
                return VariableStyle.OnlyAsRefParam;
            }
#endif
            // bug # 12258, 12114
            // use "out" if "&" is taken for the variable
            if (unsafeAddressTaken)
            {
                return VariableStyle.Out;
            }

            var key = new Key(
                dataFlowIn,
                dataFlowOut,
                alwaysAssigned,
                variableDeclared,
                readInside,
                writtenInside,
                readOutside,
                writtenOutside);

            // special cases
            if (!matrix.ContainsKey(key))
            {
                // Interesting case.  Due to things like constant analysis there can be regions that
                // the compiler considers data not to flow in (because analysis proves that that
                // path will never be taken).  However, the variable can still be read/written inside
                // the region.  For purposes of extract method, we check for this case, and we
                // pretend it's as if data flowed into the region.  
                if (!dataFlowIn && (readInside || writtenInside))
                {
                    key = new Key(true, dataFlowOut, alwaysAssigned, variableDeclared, readInside, writtenInside, readOutside, writtenOutside);
                }

                // another interesting case (bug # 10875)
                // basically, it can happen in malformed code where a variable is not properly assigned but used outside of the selection + unreachable code region
                // for such cases, treat it like "MoveOut"
                if (!dataFlowOut && !alwaysAssigned && variableDeclared && !writtenInside && readOutside)
                {
                    key = new Key(dataFlowIn, /*dataFlowOut*/ true, alwaysAssigned, variableDeclared, readInside, writtenInside, readOutside, writtenOutside);
                }
            }

            Contract.ThrowIfFalse(matrix.ContainsKey(key));

            return matrix[key];
        }

        private static void BuildMatrix()
        {
            // meaning of each boolean values (total of 69 different cases)
            // data flowin/data flow out/always assigned/variable declared/ read inside/written inside/read outside/written outside
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.MoveIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.SplitIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: false, writtenOutside: true), VariableStyle.MoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.MoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: false, writtenOutside: false), VariableStyle.None);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.MoveIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.SplitIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.MoveIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.SplitIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitIn);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Ref);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Ref);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: true, writtenOutside: false), VariableStyle.NotUsed);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.NotUsed);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Out);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Out);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Out);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Out);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithErrorInput);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithErrorInput);
            matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
        }

        private struct Key : IEquatable<Key>
        {
            public bool DataFlowIn { get; private set; }
            public bool DataFlowOut { get; private set; }
            public bool AlwaysAssigned { get; private set; }
            public bool VariableDeclared { get; private set; }
            public bool ReadInside { get; private set; }
            public bool WrittenInside { get; private set; }
            public bool ReadOutside { get; private set; }
            public bool WrittenOutside { get; private set; }

            public Key(
                bool dataFlowIn,
                bool dataFlowOut,
                bool alwaysAssigned,
                bool variableDeclared,
                bool readInside,
                bool writtenInside,
                bool readOutside,
                bool writtenOutside) :
                this()
            {
                this.DataFlowIn = dataFlowIn;
                this.DataFlowOut = dataFlowOut;
                this.AlwaysAssigned = alwaysAssigned;
                this.VariableDeclared = variableDeclared;
                this.ReadInside = readInside;
                this.WrittenInside = writtenInside;
                this.ReadOutside = readOutside;
                this.WrittenOutside = writtenOutside;
            }

            public bool Equals(Key key)
            {
                return this.DataFlowIn == key.DataFlowIn &&
                       this.DataFlowOut == key.DataFlowOut &&
                       this.AlwaysAssigned == key.AlwaysAssigned &&
                       this.VariableDeclared == key.VariableDeclared &&
                       this.ReadInside == key.ReadInside &&
                       this.WrittenInside == key.WrittenInside &&
                       this.ReadOutside == key.ReadOutside &&
                       this.WrittenOutside == key.WrittenOutside;
            }

            public override bool Equals(object obj)
            {
                if (obj is Key)
                {
                    return Equals((Key)obj);
                }

                return false;
            }

            public override int GetHashCode()
            {
                var hashCode = 0;

                hashCode = this.DataFlowIn ? 1 << 7 | hashCode : hashCode;
                hashCode = this.DataFlowOut ? 1 << 6 | hashCode : hashCode;
                hashCode = this.AlwaysAssigned ? 1 << 5 | hashCode : hashCode;
                hashCode = this.VariableDeclared ? 1 << 4 | hashCode : hashCode;
                hashCode = this.ReadInside ? 1 << 3 | hashCode : hashCode;
                hashCode = this.WrittenInside ? 1 << 2 | hashCode : hashCode;
                hashCode = this.ReadOutside ? 1 << 1 | hashCode : hashCode;
                hashCode = this.WrittenOutside ? 1 << 0 | hashCode : hashCode;

                return hashCode;
            }
        }
    }
}
