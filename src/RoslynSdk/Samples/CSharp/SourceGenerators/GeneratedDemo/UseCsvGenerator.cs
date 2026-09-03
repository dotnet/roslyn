// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSV;
using static System.Console;

namespace GeneratedDemo
{
    class UseCsvGenerator
    {
        public static void Run()
        {
            WriteLine("## CARS");
            Cars.All.ToList().ForEach(c => WriteLine($"{c.Brand}\t{c.Model}\t{c.Year}\t{c.Cc}"));
            WriteLine("\n## PEOPLE");
            People.All.ToList().ForEach(p => WriteLine($"{p.Name}\t{p.Address}\t{p._11Age}"));
        }
    }
}
