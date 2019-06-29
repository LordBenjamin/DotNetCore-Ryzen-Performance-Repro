using System;
using System.IO;
using BenchmarkDotNet.Attributes;

namespace RyzenPerf
{
    [CoreJob]
    [ClrJob]
    public class StreamWriteBenchmark
    {
        private const int SourceDataLength = 3000000;
        private const int CopySize = 1024 * 1024;

        private Stream stream = new MemoryStream();
        private byte[] buffer = new byte[SourceDataLength];

        [Benchmark]
        public void StreamWrite()
        {
            stream.Seek(0, SeekOrigin.Begin);

            int remainder;
            int iterations = Math.DivRem(buffer.Length, CopySize, out remainder);

            stream.Write(buffer, 0, remainder);

            for(int i = 0; i < iterations; i+= CopySize)
            {
                stream.Write(buffer, i, CopySize);
            }
        }
    }

}
