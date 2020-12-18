/*
The MIT License (MIT)

Copyright (c) 2020 Dennis Corvers

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
using System.Threading;

namespace UnsafeCollections.Collections.Unsafe
{
    internal class UnsafeLockException : Exception
    {
        public UnsafeLockException()
        { }

        public UnsafeLockException(string message) : base(message)
        { }
    }

    internal struct UnsafeLock
    {
        const int Locked = 1;
        const int Unlocked = 0;

        volatile int _lock;

        public void Lock()
        {
            while (Interlocked.CompareExchange(ref _lock, Locked, Unlocked) != Unlocked)
            {
                Thread.SpinWait(1);
            }
        }

        public void Unlock()
        {
            if (Interlocked.CompareExchange(ref _lock, Unlocked, Locked) != Locked)
            {
                throw new UnsafeLockException();
            }
        }
    }
}
