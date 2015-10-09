// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

// ildasm /out:ModoptTests.il ModoptTestOrignal.dll
// notepad ModoptTests.il 
//      *  change "ModoptTestOrignal" to "ModoptTests" in  .assembly & module sections
//      * add modopt modifiers 
//      * remove numbers of at the end of M|Method
// ilasm /dll /output:ModoptTests.dll ModoptTests.il
namespace Metadata
{
    public class LeastModoptsWinAmbiguous
    {
        // 2
        public virtual byte M1(byte /*modopt*/ t, byte /*modopt*/ v) { return 22; }
        // 2
        public virtual byte /*modopt*/ M2(byte /*modopt*/ t, byte v) { return 33; }

        public virtual byte /*modopt*/ GetByte() { return 6; }
    }

    public class LeastModoptsWin : LeastModoptsWinAmbiguous
    {
        // 2
        public virtual byte /*modopt*/ /*modopt*/ M(byte t, byte v) { return 11; }
        // 1
        public virtual byte /*modopt*/ M3(byte t, byte v) { return 51; }
        // 1 - modreq (Not participate in OR)
        public virtual byte /*modreq*/ M4(byte t, byte v) { return 44; }
    }

    public class ModoptPropAmbiguous
    {
        // 2
        public virtual string /*modopt*/ /*modopt*/ P { get { return "2 modopts"; } }
        // 1
        public virtual string /*modopt*/ P1 { get { return "1 modopt"; } }
        // not-in
        public virtual string /*modreq*/ P2 { get { return "1 modreq"; } }
    }
    // 
    public interface IFooAmbiguous<T, R>
    {
        // not in
        R M(T /*modreq*/ t);
        // 1
        R /*modopt*/ M1(T t);
        // 1
        R M2(T /*modopt*/ t);
    }

    public interface IFoo
    {
        // 2
        string /*modopt*/ M<T>(T /*modopt*/ t);
        // 1 
        string /*modopt*/ M1<T>(T t);
    }

    public class Modreq
    {
        public virtual void M(uint x) { Console.Write(x); }
    }
}
