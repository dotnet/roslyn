' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Module Module1

    Sub Main()

        Dim i8 As SByte
        Dim i16 As Short
        Dim i32 As Integer
        Dim i64 As Long
        Dim ui8 As Byte
        Dim ui16 As UShort
        Dim ui32 As UInteger
        Dim ui64 As ULong

        i8 = 1
        i16 = 1
        i32 = 1
        i64 = 1
        ui8 = 1
        ui16 = 1
        ui32 = 1
        ui64 = 1

        Console.WriteLine(" << 0")
        Console.WriteLine(i8 << 0)
        Console.WriteLine(i16 << 0)
        Console.WriteLine(i32 << 0)
        Console.WriteLine(i64 << 0)
        Console.WriteLine(ui8 << 0)
        Console.WriteLine(ui16 << 0)
        Console.WriteLine(ui32 << 0)
        Console.WriteLine(ui64 << 0)

        Console.WriteLine(" << 7")
        Console.WriteLine(i8 << 7)
        Console.WriteLine(i16 << 7)
        Console.WriteLine(i32 << 7)
        Console.WriteLine(i64 << 7)
        Console.WriteLine(ui8 << 7)
        Console.WriteLine(ui16 << 7)
        Console.WriteLine(ui32 << 7)
        Console.WriteLine(ui64 << 7)

        Console.WriteLine(" << 15")
        Console.WriteLine(i8 << 15)
        Console.WriteLine(i16 << 15)
        Console.WriteLine(i32 << 15)
        Console.WriteLine(i64 << 15)
        Console.WriteLine(ui8 << 15)
        Console.WriteLine(ui16 << 15)
        Console.WriteLine(ui32 << 15)
        Console.WriteLine(ui64 << 15)

        Console.WriteLine(" << 31")
        Console.WriteLine(i8 << 31)
        Console.WriteLine(i16 << 31)
        Console.WriteLine(i32 << 31)
        Console.WriteLine(i64 << 31)
        Console.WriteLine(ui8 << 31)
        Console.WriteLine(ui16 << 31)
        Console.WriteLine(ui32 << 31)
        Console.WriteLine(ui64 << 31)

        Console.WriteLine(" << 63")
        Console.WriteLine(i8 << 63)
        Console.WriteLine(i16 << 63)
        Console.WriteLine(i32 << 63)
        Console.WriteLine(i64 << 63)
        Console.WriteLine(ui8 << 63)
        Console.WriteLine(ui16 << 63)
        Console.WriteLine(ui32 << 63)
        Console.WriteLine(ui64 << 63)

        Console.WriteLine(" << 64")
        Console.WriteLine(i8 << 64)
        Console.WriteLine(i16 << 64)
        Console.WriteLine(i32 << 64)
        Console.WriteLine(i64 << 64)
        Console.WriteLine(ui8 << 64)
        Console.WriteLine(ui16 << 64)
        Console.WriteLine(ui32 << 64)
        Console.WriteLine(ui64 << 64)

        i8 = System.SByte.MaxValue
        i16 = System.Int16.MaxValue
        i32 = System.Int32.MaxValue
        i64 = System.Int64.MaxValue
        ui8 = System.Byte.MaxValue
        ui16 = System.UInt16.MaxValue
        ui32 = System.UInt32.MaxValue
        ui64 = System.UInt64.MaxValue

        Console.WriteLine(" >> 0")
        Console.WriteLine(i8 >> 0)
        Console.WriteLine(i16 >> 0)
        Console.WriteLine(i32 >> 0)
        Console.WriteLine(i64 >> 0)
        Console.WriteLine(ui8 >> 0)
        Console.WriteLine(ui16 >> 0)
        Console.WriteLine(ui32 >> 0)
        Console.WriteLine(ui64 >> 0)

        Console.WriteLine(" >> 6")
        Console.WriteLine(i8 >> 6)
        Console.WriteLine(i16 >> 6)
        Console.WriteLine(i32 >> 6)
        Console.WriteLine(i64 >> 6)
        Console.WriteLine(ui8 >> 6)
        Console.WriteLine(ui16 >> 6)
        Console.WriteLine(ui32 >> 6)
        Console.WriteLine(ui64 >> 6)

        Console.WriteLine(" >> 14")
        Console.WriteLine(i8 >> 14)
        Console.WriteLine(i16 >> 14)
        Console.WriteLine(i32 >> 14)
        Console.WriteLine(i64 >> 14)
        Console.WriteLine(ui8 >> 14)
        Console.WriteLine(ui16 >> 14)
        Console.WriteLine(ui32 >> 14)
        Console.WriteLine(ui64 >> 14)

        Console.WriteLine(" >> 30")
        Console.WriteLine(i8 >> 30)
        Console.WriteLine(i16 >> 30)
        Console.WriteLine(i32 >> 30)
        Console.WriteLine(i64 >> 30)
        Console.WriteLine(ui8 >> 30)
        Console.WriteLine(ui16 >> 30)
        Console.WriteLine(ui32 >> 30)
        Console.WriteLine(ui64 >> 30)

        Console.WriteLine(" >> 62")
        Console.WriteLine(i8 >> 62)
        Console.WriteLine(i16 >> 62)
        Console.WriteLine(i32 >> 62)
        Console.WriteLine(i64 >> 62)
        Console.WriteLine(ui8 >> 62)
        Console.WriteLine(ui16 >> 62)
        Console.WriteLine(ui32 >> 62)
        Console.WriteLine(ui64 >> 62)

        Console.WriteLine(" >> 64")
        Console.WriteLine(i8 >> 64)
        Console.WriteLine(i16 >> 64)
        Console.WriteLine(i32 >> 64)
        Console.WriteLine(i64 >> 64)
        Console.WriteLine(ui8 >> 64)
        Console.WriteLine(ui16 >> 64)
        Console.WriteLine(ui32 >> 64)
        Console.WriteLine(ui64 >> 64)

    End Sub


End Module
