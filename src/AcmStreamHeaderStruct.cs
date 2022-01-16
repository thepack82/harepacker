using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 128)] // explicit size to make it work for x64
class AcmStreamHeaderStruct
{
    public int cbStruct;
    public AcmStreamHeaderStatusFlags fdwStatus = 0;
    public IntPtr userData;
    public IntPtr sourceBufferPointer;
    public int sourceBufferLength;
    public int sourceBufferLengthUsed;
    public IntPtr sourceUserData;
    public IntPtr destBufferPointer;
    public int destBufferLength;
    public int destBufferLengthUsed = 0;
    public IntPtr destUserData;

    // 10 reserved values follow this, we don't need to declare them
    // since we have set the struct size explicitly and don't
    // need to access them in client code (thanks Brian)
    /*public int reserved0;
    public int reserved1;
    public int reserved2;
    public int reserved3;
    public int reserved4;
    public int reserved5;
    public int reserved6;
    public int reserved7;
    public int reserved8;
    public int reserved9;*/
}
