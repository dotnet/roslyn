// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

using Roslyn.Compilers;
using Roslyn.Compilers.Common;

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
            Assert.True(bag1.IsEmpty);
            Assert.False(bag1.IsSealed);
            Assert.Equal(0, bag1.Count());
            Assert.Equal("<no errors>", bag1.ToString());
        }

        [Fact]
        public void DiagnosticBag()
        {
            DiagnosticBag bag1 = new DiagnosticBag();
            bag1.Add(CreateDiagnostic(4));
            bag1.Add(CreateDiagnostic(7));
            Assert.False(bag1.IsEmpty);
            Assert.Equal(2, bag1.Count());
            Assert.NotNull(bag1.ToString());

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
            bag1.Add(bag2);
            Assert.False(bag1.IsEmpty);
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
            bag1.Add(bag3);
            Assert.False(bag1.IsEmpty);
            Assert.Equal(5, bag1.Count());
        }

        [Fact]
        public void SealBag()
        {
            DiagnosticBag bag1 = new DiagnosticBag();
            Assert.False(bag1.IsSealed);
            bag1.Add(CreateDiagnostic(4));
            bag1.Seal();
            Assert.True(bag1.IsSealed);

            Assert.Throws<InvalidOperationException>(delegate()
            {
                bag1.Add(CreateDiagnostic(7));
            });

            Assert.Equal(1, bag1.Count());
        }


        private Diagnostic CreateDiagnostic(int code)
        {
            MockMessageProvider provider = new MockMessageProvider();
            DiagnosticInfo di = new DiagnosticInfo(provider, code);
            return new Diagnostic(di, new EmptyMockLocation());
        }
    }

    class EmptyMockLocation : Location
    {
    }
}
#endif
