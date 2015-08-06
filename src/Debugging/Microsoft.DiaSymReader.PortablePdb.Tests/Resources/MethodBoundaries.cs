#line 1 "C:\MethodBoundaries1.cs"
#pragma checksum "C:\MethodBoundaries1.cs" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DBEB2A067B2F0E0D678A002C587A2806056C3DCE"

partial class C
{
    int i = F()                      // 6
            +                        // (7)
            F();                     // (8)
                               
    public C()                       // 10
    {                                // 11
        F();                         // 12
    }                                // 13
                                     
    int j = F();                     // 15
                                     
    public static int F()            
    {                                // 18
#line 10 "C:\MethodBoundaries1.cs"  
        F();                         // 11 
#line 5 "C:\MethodBoundaries1.cs"    
        F();                         // 6
                                    
        F();                         // 8
        F();                         // 9
                                     
#line 5 "C:\MethodBoundaries1.cs"    
        F();                         // 6
                                    
#line 1 "C:\MethodBoundaries2.cs"   
        F();                         // 2-2
                                     
#line 20 "C:\MethodBoundaries1.cs"   
        F();                         // 21
                                     
        return 1;                    // 23
    }                                // 24

    public static int G()
#line 4 "C:\MethodBoundaries1.cs"
    {                                // 5
        F(                           // 6
                                     // (7)
        );                           // (8)
        return 1;                    // 9
    }                                // 10

#line 5 "C:\MethodBoundaries2.cs"
    public static int E0() => F();    // 6

#line 7 "C:\MethodBoundaries2.cs"
    public static int E1() => F();    // 8

    public static int H()
#line 4 "C:\MethodBoundaries2.cs"
    {                                // 5
        F(                           // 6
                                     // (7)
                                     // (8)
        );                           // (9)
        return 1;                    // 10
    }                                // 11

#line 6 "C:\MethodBoundaries2.cs"
    public static int E2() => F();   // 7

    public static int E3() =>
#line 8 "C:\MethodBoundaries2.cs"
    F() +                            // 9
    F();                             // (10)

    public static int E4() =>
#line 9 "C:\MethodBoundaries2.cs"
    F();                             // 10   

    // Overlapping sequence point spans from different methods.

    public static void J1()
#line 13 "C:\MethodBoundaries2.cs"
    {                                // 14
        F();                         // 15
    }                                // 16

    public static int I()
#line 11 "C:\MethodBoundaries2.cs"
    {                                // 12
        F(                           // 13
                                     // (14) overlaps with J1
                                     // (15) overlaps with J1
                                     // (16) overlaps with J1
                                     // (17) overlaps with J2
                                     // (18) overlaps with J2
                                     // (19)
                                     // (20)
                                     // (21)
        );                           // (22)
        return 1;                    // 23
    }                                // 24  

    public static void J2()
#line 16 "C:\MethodBoundaries2.cs"
    {                                // 17
        F();                         // 18
#line 28 "C:\MethodBoundaries2.cs"
    }                                // 29

    public static void K1()
#line 1 "C:\MethodBoundaries3.cs"
    {                                // 2     K1
        F(                           // 3     K1
                                     // (4)   K1, K2
                                     // (5)   K1, K2
                                     // (6)   K1, K2, K3
                                     // (7)   K1, K2, K3
                                     // (8)   K1, K2, K3, K4
                                     // (9)   K1, K2, K3, K4
                                     // (10)  K1, K2, K3
                                     // (11)  K1, K2, K3
        );                           // (12)  K1, K2
    }                                // 13    K1

    public static void K2()
#line 3 "C:\MethodBoundaries3.cs"
    {                                // 4     
        F(                           // 5     
                                     // (6)   
                                     // (7)   
                                     // (8)   
                                     // (9)   
                                     // (10)  
        );                           // (11)  
    }                                // 12    

    public static void K3()
#line 5 "C:\MethodBoundaries3.cs"
    {                                // 6     
        F(                           // 7     
                                     // (8)   
                                     // (9)   
        );                           // (10)  
    }                                // 11    

    public static void K4() =>
#line 7 "C:\MethodBoundaries3.cs"
        F(                           // 8
        );                           // (9)
}