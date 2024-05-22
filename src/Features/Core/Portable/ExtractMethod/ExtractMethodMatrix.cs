// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal class ExtractMethodMatrix
{
    private static readonly Dictionary<Key, VariableStyle> s_matrix;

    static ExtractMethodMatrix()
    {
        s_matrix = [];
        BuildMatrix();
    }

    public static bool TryGetVariableStyle(
        bool bestEffort,
        bool dataFlowIn,
        bool dataFlowOut,
        bool alwaysAssigned,
        bool variableDeclared,
        bool readInside,
        bool writtenInside,
        bool readOutside,
        bool writtenOutside,
        bool unsafeAddressTaken,
        out VariableStyle variableStyle)
    {
        // bug # 12258, 12114
        // use "out" if "&" is taken for the variable
        if (unsafeAddressTaken)
        {
            variableStyle = VariableStyle.Out;
            return true;
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
        if (!s_matrix.ContainsKey(key))
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

            // interesting case in invalid code (bug #19136)
            // variable is passed by reference, and another argument is an out variable with the same name
            if (dataFlowIn && variableDeclared)
            {
                key = new Key(/*dataFlowIn:*/ false, dataFlowOut, alwaysAssigned, variableDeclared, readInside, writtenInside, readOutside, writtenOutside);
            }
        }

        if (s_matrix.TryGetValue(key, out variableStyle))
        {
            return true;
        }

        if (bestEffort)
        {
            // In best effort mode, even though we didn't know precisely what to do, we still
            // allow the user to keep going, assuming that this variable is a very basic one.
            variableStyle = VariableStyle.InputOnly;
            return true;
        }

        // Some combination we didn't anticipate.  Can't do anything here.  Log the issue
        // and bail out.
        FatalError.ReportAndCatch(new Exception($"extract method encountered unknown states: {key.ToString()}"), ErrorSeverity.Critical);

        return false;
    }

    private static void BuildMatrix()
    {
        // meaning of each boolean values (total of 69 different cases)
        // data flowin/data flow out/always assigned/variable declared/ read inside/written inside/read outside/written outside
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.MoveIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.SplitIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: false, writtenOutside: true), VariableStyle.MoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.MoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: false, writtenOutside: false), VariableStyle.None);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.MoveIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.SplitIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: false, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.Ref);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.MoveIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.SplitIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitIn);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.None);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.SplitOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Ref);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Ref);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: true, writtenOutside: false), VariableStyle.NotUsed);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.NotUsed);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: false, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Out);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Out);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.Out);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Out);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: false, dataFlowOut: true, alwaysAssigned: true, variableDeclared: true, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.OutWithMoveOut);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: false, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.InputOnly);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: false, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithErrorInput);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: false, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: false), VariableStyle.OutWithErrorInput);
        s_matrix.Add(new Key(dataFlowIn: true, dataFlowOut: true, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: true, readOutside: true, writtenOutside: true), VariableStyle.Ref);
    }

    private readonly struct Key : IEquatable<Key>
    {
        public bool DataFlowIn { get; }
        public bool DataFlowOut { get; }
        public bool AlwaysAssigned { get; }
        public bool VariableDeclared { get; }
        public bool ReadInside { get; }
        public bool WrittenInside { get; }
        public bool ReadOutside { get; }
        public bool WrittenOutside { get; }

        public Key(
            bool dataFlowIn,
            bool dataFlowOut,
            bool alwaysAssigned,
            bool variableDeclared,
            bool readInside,
            bool writtenInside,
            bool readOutside,
            bool writtenOutside)
            : this()
        {
            DataFlowIn = dataFlowIn;
            DataFlowOut = dataFlowOut;
            AlwaysAssigned = alwaysAssigned;
            VariableDeclared = variableDeclared;
            ReadInside = readInside;
            WrittenInside = writtenInside;
            ReadOutside = readOutside;
            WrittenOutside = writtenOutside;
        }

        public bool Equals(Key key)
        {
            return DataFlowIn == key.DataFlowIn &&
                   DataFlowOut == key.DataFlowOut &&
                   AlwaysAssigned == key.AlwaysAssigned &&
                   VariableDeclared == key.VariableDeclared &&
                   ReadInside == key.ReadInside &&
                   WrittenInside == key.WrittenInside &&
                   ReadOutside == key.ReadOutside &&
                   WrittenOutside == key.WrittenOutside;
        }

        public override bool Equals(object obj)
        {
            if (obj is Key key)
            {
                return Equals(key);
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 0;

            hashCode = DataFlowIn ? 1 << 7 | hashCode : hashCode;
            hashCode = DataFlowOut ? 1 << 6 | hashCode : hashCode;
            hashCode = AlwaysAssigned ? 1 << 5 | hashCode : hashCode;
            hashCode = VariableDeclared ? 1 << 4 | hashCode : hashCode;
            hashCode = ReadInside ? 1 << 3 | hashCode : hashCode;
            hashCode = WrittenInside ? 1 << 2 | hashCode : hashCode;
            hashCode = ReadOutside ? 1 << 1 | hashCode : hashCode;
            hashCode = WrittenOutside ? 1 << 0 | hashCode : hashCode;

            return hashCode;
        }

        public override string ToString() => GetHashCode().ToString("X");
    }
}
