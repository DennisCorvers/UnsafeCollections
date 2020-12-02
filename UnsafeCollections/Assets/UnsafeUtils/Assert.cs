/*
The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Diagnostics;

public class AssertException : Exception
{
    public AssertException()
    {
    }

    public AssertException(string msg)
      : base(msg)
    {
    }
}

public static class Assert
{
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Fail()
    {
        throw new AssertException();
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Fail(string error)
    {
        throw new AssertException(error);
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Fail(string format, params object[] args)
    {
        throw new AssertException(string.Format(format, args));
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Check(bool condition)
    {
        if (condition == false)
        {
            throw new AssertException();
        }
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Check(bool condition, string error)
    {
        if (condition == false)
        {
            throw new AssertException(error);
        }
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Check(bool condition, string format, params object[] args)
    {
        if (condition == false)
        {
            throw new AssertException(string.Format(format, args));
        }
    }

    [DebuggerStepThrough]
    public static void Always(bool condition)
    {
        if (condition == false)
        {
            throw new AssertException();
        }
    }

    [DebuggerStepThrough]
    public static void Always(bool condition, string error)
    {
        if (condition == false)
        {
            throw new AssertException(error);
        }
    }

    [DebuggerStepThrough]
    public static void Always(bool condition, string format, params object[] args)
    {
        if (condition == false)
        {
            throw new AssertException(string.Format(format, args));
        }
    }
}