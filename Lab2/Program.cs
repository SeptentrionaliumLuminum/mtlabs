using System;
using System.Threading;
using Lab2.Data;
using Lab2.Services;

using MPI;

namespace Lab2
{
    class Program
    {
        static void Main(string[] args)
        {
            //EquationGenerator.Generate(args[0], args[1], 1681);

            DoParallelJacoby(args);
        }

        static void DoParallelJacoby(string[] args)
        {
            MPI.Environment.Run(ref args, communicator =>
            {

                Console.WriteLine($"{communicator.Rank} birthday!");

                var isMaster = communicator.Rank == 0;

                if (isMaster)
                {
                    double[] result = null;

                    using (var performanceCounter = new PerformanceCounter("Master. Total duration: "))
                    {
                        Jacoby jacoby = null;

                        using (var perfRead = new PerformanceCounter("Master. Jacoby reading time: "))
                        {
                            jacoby = EquationReader.ReadJacoby(args[0], args[1], double.Parse(args[2]));
                        }

                        using (var perfWork = new PerformanceCounter("Master. Work duration: "))
                        {
                            result = jacoby.MPIHead(communicator);
                            //double[] result = jacoby.SolveSingleThreadDebug(2);
                        }
                    }

                    if (result == null)
                        throw new Exception("Result is null");

                    EquationWriter.Write(result, args[3]);
                }
                else
                {
                    using (var perfCounter = new PerformanceCounter($"Worker {communicator.Rank} total lifetime: "))
                    {
                        Jacoby.MPIWorker(communicator);
                    }
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
                    communicator.Send(jacoby, workerIndex, (int)JacobyMessageType.JacobyMessage);
            }
            else
            {
                var jacoby = communicator.Receive<Jacoby>(0, (int)JacobyMessageType.JacobyMessage);
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
