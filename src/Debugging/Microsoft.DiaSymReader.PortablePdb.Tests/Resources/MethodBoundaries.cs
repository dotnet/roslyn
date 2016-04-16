#line 1 "C:\MethodBoundaries1.cs"
#pragma checksum "C:\MethodBoundaries1.cs" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DBEB2A067B2F0E0D678A002C587A2806056C3DCE"

partial class C
{
    int i = F()                      // 5
            +                        // (6)
            F();                     // (7)
                               
    public C()                       // 9
    {                                // 10
        F();                         // 11
    }                                // 12
                                     
    int j = F();                     // 14
                                     
    public static int F()            
    {                                // 17
#line 10 "C:\MethodBoundaries1.cs"  
        F();                         // 10 
#line 5 "C:\MethodBoundaries1.cs"    
        F();                         // 5
                                    
        F();                         // 7
        F();                         // 8
                                     
#line 5 "C:\MethodBoundaries1.cs"    
        F();                         // 5
                                    
#line 1 "C:\MethodBoundaries2.cs"   
        F();                         // 2-1
                                     
#line 20 "C:\MethodBoundaries1.cs"   
        F();                         // 20
                                     
        return 1;                    // 22
    }                                // 23

    public static int G()
#line 4 "C:\MethodBoundaries1.cs"
    {                                // 4
        F(                           // 5
                                     // (6)
        );                           // (7)
        return 1;                    // 8
    }                                // 9

#line 5 "C:\MethodBoundaries2.cs"
    public static int E0() => F();    // 5

#line 7 "C:\MethodBoundaries2.cs"
    public static int E1() => F();    // 7

    public static int H()
#line 4 "C:\MethodBoundaries2.cs"
    {                                // 4
        F(                           // 5
                                     // (6)
                                     // (7)
        );                           // (8)
        return 1;                    // 9
    }                                // 10

#line 6 "C:\MethodBoundaries2.cs"
    public static int E2() => F();   // 6

    public static int E3() =>
#line 8 "C:\MethodBoundaries2.cs"
    F() +                            // 8
    F();                             // (9)

    public static int E4() =>
#line 9 "C:\MethodBoundaries2.cs"
    F();                             // 9   

    // Overlapping sequence point spans from different methods.

    public static void J1()
#line 13 "C:\MethodBoundaries2.cs"
    {                                // 13
        F();                         // 14
    }                                // 15

    public static int I()
#line 11 "C:\MethodBoundaries2.cs"
    {                                // 11
        F(                           // 12
                                     // (13) overlaps with J1
                                     // (14) overlaps with J1
                                     // (15) overlaps with J1
                                     // (16) overlaps with J2
                                     // (17) overlaps with J2
                                     // (18)
                                     // (19)
                                     // (20)
        );                           // (21)
        return 1;                    // 22
    }                                // 23  

    public static void J2()
#line 16 "C:\MethodBoundaries2.cs"
    {                                // 16
        F();                         // 17
#line 28 "C:\MethodBoundaries2.cs"
    }                                // 28

    public static void K1()
#line 1 "C:\MethodBoundaries3.cs"
    {                                // 1     K1
        F(                           // 2     K1
                                     // (3)   K1, K2
                                     // (4)   K1, K2
                                     // (5)   K1, K2, K3
                                     // (6)   K1, K2, K3
                                     // (7)   K1, K2, K3, K4
                                     // (8)   K1, K2, K3, K4
                                     // (9)   K1, K2, K3
                                     // (10)  K1, K2, K3
        );                           // (11)  K1, K2
    }                                // 12    K1

    public static void K2()
#line 3 "C:\MethodBoundaries3.cs"
    {                                // 3    
        F(                           // 4     
                                     // (5)   
                                     // (6)   
                                     // (7)   
                                     // (8)   
                                     // (9)  
        );                           // (10)  
    }                                // 11   

    public static void K3()
#line 5 "C:\MethodBoundaries3.cs"
    {                                // 5     
        F(                           // 6     
                                     // (7)   
                                     // (8)   
        );                           // (9)  
    }                                // 10    

    public static void K4() =>
#line 7 "C:\MethodBoundaries3.cs"
        F(                           // 7
        );                           // (8)
}