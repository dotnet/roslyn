// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;

[TestClass]
public class RandomRealParserTests
{
    [TestMethod]
    public void TestRandomDoubleStrings()
    {
        var start = DateTime.UtcNow;
        // compare our atod on random strings against C's strtod
        Parallel.For(0, 150, part =>
        {
            Random r = new Random(start.GetHashCode() + part);
            var b = new StringBuilder();
            for (int i = 0; i < 100000; i++)
            {
                b.Clear();
                int beforeCount = r.Next(3);
                int afterCount = r.Next(1 + part % 50);
                int exp = r.Next(-330, 330);
                if (beforeCount == 0) b.Append('0');
                for (int j = 0; j < beforeCount; j++) b.Append((char)('0' + r.Next(10)));
                b.Append('.');
                for (int j = 0; j < afterCount; j++) b.Append((char)('0' + r.Next(10)));
                b.Append('e');
                if (exp >= 0 && r.Next(2) == 0) b.Append('+');
                b.Append(exp);
                var s = b.ToString();
                double d1;
                d1 = CLibraryShim.RealConversions.atod(s);
                double d2;
                if (!RealParser.TryParseDouble(s, out d2)) d2 = 1.0 / 0.0;
                Assert.AreEqual(d1, d2, 0.0, $"{s} differ\n  RealParser=>{d2:G17}\n  atod=>{d1:G17}\n");
            }
        });
    }

    [TestMethod]
    public void TestRandomFloatStrings()
    {
        var start = DateTime.UtcNow;
        // compare our atof on random strings against C's strtof
        Parallel.For(0, 150, part =>
        {
            Random r = new Random(start.GetHashCode() + part);
            var b = new StringBuilder();
            for (int i = 0; i < 100000; i++)
            {
                b.Clear();
                int beforeCount = r.Next(3);
                int afterCount = r.Next(1 + part % 50);
                int exp = r.Next(-50, 40);
                if (beforeCount == 0) b.Append('0');
                for (int j = 0; j < beforeCount; j++) b.Append((char)('0' + r.Next(10)));
                b.Append('.');
                for (int j = 0; j < afterCount; j++) b.Append((char)('0' + r.Next(10)));
                b.Append('e');
                if (exp >= 0 && r.Next(2) == 0) b.Append('+');
                b.Append(exp);
                var s = b.ToString();
                float d1;
                d1 = CLibraryShim.RealConversions.atof(s);
                float d2;
                if (!RealParser.TryParseFloat(s, out d2)) d2 = 1.0f / 0.0f;
                Assert.AreEqual(d1, d2, 0.0, $"{s} differ\n  RealParser=>{d2:G17}\n  atof=>{d1:G17}\n");
            }
        });
    }
}
