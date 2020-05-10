//
// Copyright (c) 2020 Chris Gunn
//

using BenchmarkDotNet.Running;

namespace BitSerialization.PerfTests
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
