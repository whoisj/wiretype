using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sample;

namespace PerfCli
{
    class Program
    {
        const int inner_loop = 1024;
        const int outer_loop = 1024;

        unsafe static void Main(string[ ] args)
        {
            var rand = new Random(19770407);
            var varint = stackalloc byte[sizeof(ulong) + 2];

            int bytes_saved = 0;
            int bytes_wasted = 0;
            int encoded_count = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();

            for (int x = 1; x < outer_loop; x += 1)
            {
                for (int i = 1; i < inner_loop; i += 1)
                {
                    int number = x * i;

                    if (i % 2 == 0)
                    {
                        number *= -1;
                    }

                    int value = 0;

                    int written = WireType.Varint.Write(&number, varint);
                    int read = WireType.Varint.Read(varint, &value);

                    if (written != read)
                        throw new InvalidCastException($"written != read | {written} != {read}");

                    if (number != value)
                        throw new InvalidCastException($"number != value | {number} != {value}");

                    if (written < sizeof(int))
                    {
                        bytes_saved += 1;
                    }
                    else if (written > sizeof(int))
                    {
                        bytes_wasted += 1;
                    }

                    encoded_count += 1;
                }

                for (int i = 1; i < inner_loop; i += 1)
                {
                    long number = rand.Next();

                    if (i % 3 == 0)
                    {
                        number <<= 32;
                        number += x * i;
                    }

                    if (i % 2 == 0)
                    {
                        number *= -1;
                    }

                    long value = 0;

                    int written = WireType.Varint.Write(&number, varint);
                    int read = WireType.Varint.Read(varint, &value);

                    if (written != read)
                        throw new InvalidCastException($"written != read | {written} != {read}");

                    if (number != value)
                        throw new InvalidCastException($"number != value | {number} != {value}");

                    if (written < sizeof(long))
                    {
                        bytes_saved += 1;
                    }
                    else if (written > sizeof(long))
                    {
                        bytes_wasted += 1;
                    }

                    encoded_count += 1;
                }

                for (int i = 1; i < inner_loop; i += 1)
                {
                    uint number = (uint)(x * i);

                    uint value = 0;

                    int written = WireType.Varint.Write(&number, varint);
                    int read = WireType.Varint.Read(varint, &value);

                    if (written != read)
                        throw new InvalidCastException($"written != read | {written} != {read}");

                    if (number != value)
                        throw new InvalidCastException($"number != value | {number} != {value}");

                    if (written < sizeof(uint))
                    {
                        bytes_saved += 1;
                    }
                    else if (written > sizeof(uint))
                    {
                        bytes_wasted += 1;
                    }

                    encoded_count += 1;
                }

                for (int i = 1; i < inner_loop; i += 1)
                {
                    ulong number = (ulong)(x * i);

                    if (i % 2 == 0)
                    {
                        number <<= 32;
                        number += (ulong)rand.Next();
                    }

                    ulong value = 0;

                    int written = WireType.Varint.Write(&number, varint);
                    int read = WireType.Varint.Read(varint, &value);

                    if (written != read)
                        throw new InvalidCastException($"written != read | {written} != {read}");

                    if (number != value)
                        throw new InvalidCastException($"number != value | {number} != {value}");

                    if (written < sizeof(ulong))
                    {
                        bytes_saved += 1;
                    }
                    else if (written > sizeof(ulong))
                    {
                        bytes_wasted += 1;
                    }

                    encoded_count += 1;
                }
            }

            timer.Stop();

            Console.WriteLine($"{encoded_count:###,###,###,###} integers encoded in {timer.ElapsedMilliseconds} milliseconds.");
            Console.WriteLine($"{bytes_saved:###,###,###,###} bytes saved, {bytes_wasted:###,###,###,###} bytes wasted.");

            var complex1 = new Complex();
            var leaves = new List<Basic>();

            for (int i = 0; i < 100; i += 1)
            {
                var leaf = new Basic();
                leaf.Enum(Values.Baz);
                leaf.Text("this is a string, persist me!");

                var floats = new float[32];

                unchecked
                {
                    for (int j = 0; j < floats.Length; j += 1)
                    {
                        floats[j] = (float)rand.NextDouble();
                    }
                }

                leaf.Values(floats);
                leaves.Add(leaf);
            }

            complex1.Leaves(leaves);

            timer.Restart();

            using (var stream = File.Open("complex.bin", FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new WireType.WireTypeWriter(stream))
            {
                complex1.WriteTo(writer);
            }

            timer.Stop();

            Console.WriteLine($"{timer.ElapsedMilliseconds} milliseconds to serialize 101 messages.");

            var complex2 = new Complex();

            timer.Restart();

            using (var stream = File.Open("complex.bin", FileMode.Open, FileAccess.Read, FileShare.None))
            using (var reader = new WireType.WireTypeReader(stream))
            {
                complex1.ReadFrom(reader);
            }

            timer.Stop();

            Console.WriteLine($"{timer.ElapsedMilliseconds} milliseconds to deserialize 101 messages.");

            Console.ReadKey();
        }
    }
}
