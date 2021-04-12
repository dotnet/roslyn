' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On

Imports System

Module Module1

    Sub Main()

        Dim Ob1 As Object
        Dim Ob2 As Object
        Dim Ob3 As Object

        Ob1 = "a"
        Ob2 = "a"

        Ob3 = (Ob1 + Ob2)
        Ob3 = (Ob1 - Ob2)
        Ob3 = (Ob1 * Ob2)
        Ob3 = (Ob1 / Ob2)
        Ob3 = (Ob1 \ Ob2)
        Ob3 = (Ob1 Mod Ob2)
        Ob3 = (Ob1 ^ Ob2)
        Ob3 = (Ob1 << Ob2)
        Ob3 = (Ob1 >> Ob2)
        Ob3 = (Ob1 OrElse Ob2)
        Ob3 = (Ob1 AndAlso Ob2)
        Ob3 = (Ob1 & Ob2)
        Ob3 = (Ob1 Like Ob2)
        Ob3 = (Ob1 = Ob2)
        Ob3 = (Ob1 <> Ob2)
        Ob3 = (Ob1 <= Ob2)
        Ob3 = (Ob1 >= Ob2)
        Ob3 = (Ob1 < Ob2)
        Ob3 = (Ob1 > Ob2)
        Ob3 = (Ob1 Xor Ob2)
        Ob3 = (Ob1 And Ob2)
        Ob3 = (Ob1 Or Ob2)

        Dim i1 As IComparable
        Dim i2 As IComparable

        i1 = Nothing
        i2 = Nothing

        Ob3 = (i1 = i2)
        Ob3 = (i1 <> i2)
        Ob3 = (i1 > i2)
    End Sub


End Module
