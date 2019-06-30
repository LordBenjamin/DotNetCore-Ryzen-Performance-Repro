using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace RyzenPerf
{
    [CoreJob]
    [ClrJob]
    public class OriginalBenchmark
    {
        private const int SharedArrayPoolMaxBufferSize = 1024 * 1024;
        private const int SourceDataLength = 3000000;

        private Stream stream = new MemoryStream();

        private IntPtr unmanagedBuffer;

        [GlobalSetup]
        public void Setup()
        {
            unmanagedBuffer = Marshal.AllocHGlobal(SourceDataLength);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Marshal.FreeHGlobal(unmanagedBuffer);
        }

        [Benchmark]
        public void Original()
        {
            stream.Seek(0, SeekOrigin.Begin);
            StreamWrite(unmanagedBuffer, SourceDataLength);
        }

        // This method runs faster under .NET Framework than .NET Core on an AMD Ryzen 1200.
        // The expectation is that .NET Core should be faster, given the focus on performance,
        // runtime support for Span<T>, etc. This is true on an Intel i7 6700.
        public unsafe uint StreamWrite(IntPtr buffer, uint size)
        {
            var arrayPool = ArrayPool<byte>.Shared;
            byte[] managedBuffer = arrayPool.Rent(
                size < SharedArrayPoolMaxBufferSize ? (int)size : SharedArrayPoolMaxBufferSize);

            int writeSize = (int)Math.Min(managedBuffer.Length, size);

            byte* ptr = (byte*)buffer.ToPointer();
            uint writeCount = 0;

            int bytesWritten = 0;

            int remainder;
            int iterations = Math.DivRem(checked((int)size), writeSize, out remainder);

            try
            {
                // Copy bytes that don't divide exactly into the buffer size
                ReadOnlySpan<byte> source = new ReadOnlySpan<byte>(ptr, remainder);
                source.CopyTo(managedBuffer);
                stream.Write(managedBuffer, 0, remainder);

                // Repeated full-buffer copies
                while (writeCount < iterations)
                {
                    source = new ReadOnlySpan<byte>(ptr, writeSize);
                    ptr += writeSize;

                    source.CopyTo(managedBuffer);

                    // Commenting this line out makes the .NET Core benchmark run faster than Framework
                    // on a Ryzen 1200 CPU
                    stream.Write(managedBuffer, 0, writeSize);

                    writeCount++;
                    bytesWritten += writeSize;
                }
            }
            finally
            {
                arrayPool.Return(managedBuffer);
            }

            return writeCount + 1;
        }
    }

}
