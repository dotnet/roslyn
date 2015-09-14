// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Roslyn.Compilers.UnitTests
{
    /// <summary>
    /// Unit tests for DiagnosticBag.
    /// </summary>
    public class DiagnosticBagTests
    {
        [Fact]
        public void EmptyDiagnosticBag()
        {
            DiagnosticBag bag1 = new DiagnosticBag();
            Assert.True(bag1.IsEmptyWithoutResolution);
            Assert.Equal(0, bag1.Count());
            using (var iterator = (IEnumerator<Diagnostic>)bag1.GetEnumerator())
            {
                Assert.False(iterator.MoveNext());
                iterator.Reset();
                Assert.False(iterator.MoveNext());
            }
        }

        [Fact]
        public void DiagnosticBag()
        {
            DiagnosticBag bag1 = new DiagnosticBag();
            bag1.Add(CreateDiagnostic(4));
            bag1.Add(CreateDiagnostic(7));
            Assert.False(bag1.IsEmptyWithoutResolution);
            Assert.Equal(2, bag1.Count());

            bool found4 = false, found7 = false;
            foreach (Diagnostic d in bag1)
            {
                if (d.Code == 4)
                {
                    Assert.False(found4);
                    found4 = true;
                }
                else if (d.Code == 7)
                {
                    Assert.False(found7);
                    found7 = true;
                }
                else
                    Assert.True(false);
            }

            DiagnosticBag bag2 = new DiagnosticBag();
            bag1.AddRange(bag2);
            Assert.False(bag1.IsEmptyWithoutResolution);
            Assert.Equal(2, bag1.Count());

            found4 = false; found7 = false;
            foreach (Diagnostic d in bag1)
            {
                if (d.Code == 4)
                {
                    Assert.False(found4);
                    found4 = true;
                }
                else if (d.Code == 7)
                {
                    Assert.False(found7);
                    found7 = true;
                }
                else
                    Assert.True(false);
            }

            DiagnosticBag bag3 = new DiagnosticBag();
            bag3.Add(CreateDiagnostic(3));
            bag3.Add(CreateDiagnostic(2));
            bag3.Add(CreateDiagnostic(1));
            bag1.AddRange(bag3);
            Assert.False(bag1.IsEmptyWithoutResolution);
            Assert.Equal(5, bag1.Count());
        }

        private static Diagnostic CreateDiagnostic(int code)
        {
            return new CSDiagnostic(new DiagnosticInfo(MessageProvider.Instance, code), Location.None);
        }
    }
}
