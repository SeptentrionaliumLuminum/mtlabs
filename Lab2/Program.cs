using System;
using System.Diagnostics;

using Lab2.Data;
using Lab2.Services;

using MPI;

namespace Lab2
{
    class Program
    {
        static void Main(string[] args)
        {
            //EquationGenerator.Generate(args[0], args[1], 2000);

            MPI.Environment.Run(ref args, communicator =>
            {
                if (communicator.Rank == 0)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    Jacoby jacoby = EquationReader.ReadJacoby(args[0], args[1], double.Parse(args[2]));
                    double[] result = jacoby.MPIHead(communicator);
                    //double[] result = jacoby.SolveSingleThreadDebug(2);

                    stopwatch.Stop();
                    Console.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds}");

                    EquationWriter.Write(result, args[3]);
                }
                else
                {
                    Jacoby.MPIWorker(communicator);
                }
            });
        }

        static void TestSendReceive(Communicator communicator, string[] args)
        {
            Console.WriteLine($"I am alive! Rank {communicator.Rank}");

            if (communicator.Rank == 0)
            {
                //Read data
                Jacoby jacoby = EquationReader.ReadJacoby(args[0], args[1], double.Parse(args[2]));

                //Send data
                for (int workerIndex = 1; workerIndex < communicator.Size; workerIndex++)
                    communicator.Send(jacoby, workerIndex, Consts.JacobyMessage);
            }
            else
            {
                var jacoby = communicator.Receive<Jacoby>(0, Consts.JacobyMessage);
                var result = jacoby.SolveSingleThread();
                foreach (var val in result)
                    Console.WriteLine($"I, worker of rank {communicator.Rank} calculate {val}");
            }
        }

        static void TestSingleThread(Communicator communicator, string[] args)
        {
            Console.WriteLine($"Hello process of rank {communicator.Rank} running on {MPI.Environment.ProcessorName}");

            Console.WriteLine($"{args.Length}");
            foreach (var arg in args)
                Console.WriteLine(arg);

            var jacoby = EquationReader.ReadJacoby(args[0], args[1], double.Parse(args[2]));
            var result = jacoby.SolveSingleThread();

            foreach (var val in result)
                Console.WriteLine(val);

            var answ = jacoby.Calculate(result);
            foreach (var val in answ)
                Console.WriteLine(val);

            Console.WriteLine("//////////////");

            jacoby = EquationReader.ReadJacoby(args[0], args[1], double.Parse(args[2]));
            result = jacoby.SolveSingleThreadDebug(2);

            foreach (var val in result)
                Console.WriteLine(val);

            answ = jacoby.Calculate(result);
            foreach (var val in answ)
                Console.WriteLine(val);
        }
    }
}
