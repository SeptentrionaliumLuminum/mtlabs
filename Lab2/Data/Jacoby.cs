using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MPI;
using Newtonsoft.Json;

namespace Lab2.Data
{
    [Serializable]
    public class Jacoby
    {
        public Matrix coefficients;
        public Matrix freeMembers;

        public Matrix initialStep;

        public double precision;

        public Jacoby(Matrix matrix, Matrix initialStep, double precision)
        {
            this.coefficients = Matrix.Cut(matrix, matrix.Height, matrix.Width - 1);
            this.freeMembers = Matrix.TakeColumn(matrix, matrix.Width - 1);

            this.initialStep = initialStep;

            this.precision = precision;
        }

        public static void MPIWorker(Intracommunicator communicator)
        {
            Jacoby jacoby = null;
            ReceiveRequest request;
            using (var perf = new PerformanceCounter($"Comm {communicator.Rank} waiting for Jacoby duration: "))
            {
                communicator.Broadcast<Jacoby>(ref jacoby, 0);
            }

            bool inProcess = true;

            using (var totalPerformance = new PerformanceCounter($"Comm {communicator.Rank} total work duration: "))
            {
                int cycleIndex = 0;
                while (inProcess)
                {
                    using (var perf = new PerformanceCounter($"Comm {communicator.Rank} cycle {cycleIndex} duration: "))
                    {
                        JobToDo jobToDo = null;

                        using (var perfRecv = new PerformanceCounter($"Comm {communicator.Rank} waiting for job-to-do (cycle {cycleIndex}): "))
                        {
                            request = communicator.ImmediateReceive<JobToDo>(0, (int)JacobyMessageType.CalculatePart);
                            request.Wait();
                            jobToDo = request.GetValue() as JobToDo;
                            if (jobToDo.finish)
                            {
                                Console.WriteLine($"Comm {communicator.Rank} receive FINISH signal");
                                return;
                            }
                        }

                        using (var perfWork = new PerformanceCounter($"Comm {communicator.Rank} work duration (cycle {cycleIndex}): "))
                        {
                            Console.WriteLine($"Comm {communicator.Rank} start={jobToDo.start} end={jobToDo.end}");
                            var result = jacoby.PartialCalculation(jobToDo.start, jobToDo.end, jobToDo.initial);
                            var srequest = communicator.ImmediateSend<JobDone>(new JobDone(result), 0, (int)JacobyMessageType.JobDone);
                        }
                    }
                    cycleIndex++;
                }
            }
        }

        private void SendMatrixToWorkers(Intracommunicator communicator)
        {
            using (var perfCounter = new PerformanceCounter("Master. Broadcasting Jacoby: "))
            {
                var jacoby = this;
                communicator.Broadcast<Jacoby>(ref jacoby, 0);
            }
        }

        public double[] MPIHead(Intracommunicator communicator)
        {
            SendMatrixToWorkers(communicator);

            int parts = communicator.Size - 1;

            int matrixSize = coefficients.Height;

            double[] initial = initialStep.GetColumn(0);

            double currentAccuracy;

            var portion = matrixSize / parts;

            int cycleIndex = 0;
            do
            {
                using (var perfCycle = new PerformanceCounter($"Master. Cycle {cycleIndex} work duration: "))
                {
                    List<double[]> calculations = new List<double[]>();

                    for (int part = 0; part < parts; part++)
                    {
                        var start = part * portion;
                        var end = (part + 1) * portion;

                        if (start >= matrixSize)
                            break;

                        if (part == parts - 1)
                            end = matrixSize;

                        Console.WriteLine($"Part {part} size: {end - start}");

                        var jobToDo = new JobToDo(start, end, initial);
                        var request = communicator.ImmediateSend(jobToDo, part + 1, (int)JacobyMessageType.CalculatePart);
                    }

                    ReceiveRequest[] requests = new ReceiveRequest[parts];

                    for (int part = 0; part < parts; part++)
                    {
                        requests[part] = communicator.ImmediateReceive<JobDone>(part + 1, (int)JacobyMessageType.JobDone);
                    }

                    using (var perf = new PerformanceCounter($"Master. Wait for sync (cycle {cycleIndex}): "))
                    {
                        for (int part = 0; part < parts; part++)
                        {
                            requests[part].Wait();
                            calculations.Add((requests[part].GetValue() as JobDone).array);
                        }
                    }

                    double[] currentStepXs = Merge(calculations);

                    currentAccuracy = Math.Abs(initial[0] - currentStepXs[0]);
                    for (int row = 0; row < matrixSize; row++)
                    {
                        if (Math.Abs(initialStep[row, 0] - currentStepXs[row]) > currentAccuracy)
                            currentAccuracy = Math.Abs(initial[row] - currentStepXs[row]);
                        initial[row] = currentStepXs[row];
                    }
                }
            }
            while (currentAccuracy > precision);

            Request[] srequests = new Request[parts];

            for (int i = 0; i < parts; i++)
                 srequests[i] = communicator.ImmediateSend<JobToDo>(new JobToDo(true), i + 1, (int)JacobyMessageType.CalculatePart);

            return initial;
        }

        public double[] SolveSingleThreadDebug(int parts)
        {
            int matrixSize = coefficients.Height;

            double[] initial = initialStep.GetColumn(0);

            double currentAccuracy;

            var portion = matrixSize / parts;

            do
            {
                List<double[]> calculations = new List<double[]>();

                for (int part = 0; part < parts; part++)
                {
                    var start = part * portion;
                    var end = (part + 1) * portion;

                    if (start >= matrixSize)
                        break;

                    if (part == parts - 1)
                        end = matrixSize;

                    var calc = PartialCalculation(start, end, initial);
                    calculations.Add(calc);
                }

                double[] currentStepXs = Merge(calculations);

                currentAccuracy = Math.Abs(initial[0] - currentStepXs[0]);
                for (int row = 0; row < matrixSize; row++)
                {
                    if (Math.Abs(initialStep[row, 0] - currentStepXs[row]) > currentAccuracy)
                        currentAccuracy = Math.Abs(initial[row] - currentStepXs[row]);
                    initial[row] = currentStepXs[row];
                }
            }
            while (currentAccuracy > precision);

            return initial;
        }

        public double[] Merge(List<double[]> parts)
        {
            var length = parts.Sum(part => part.Length);
            var currentStepsX = new double[length];

            int index = 0;
            foreach (var part in parts)
            {
                for (int i = 0; i < part.Length; i++)
                {
                    currentStepsX[index] = part[i];
                    index++;
                }
            }

            return currentStepsX;
        }

        public double[] PartialCalculation(int start, int end, double[] initial)
        {
            int matrixSize = coefficients.Height;

            double[] currentStepXs = new double[end - start];

            for (int row = start; row < end; row++)
            {
                int index = row - start;
                currentStepXs[index] = freeMembers[row, 0];
                for (int column = 0; column < matrixSize; column++)
                {
                    if (row != column)
                    {
                        currentStepXs[index] -= coefficients[row, column] * initial[column];
                    }
                }

                currentStepXs[index] /= coefficients[row, row];
            }

            return currentStepXs;
        }

        public double[] SolveSingleThread()
        {
            //From Wikipedia
            int matrixSize = coefficients.Height;

            double[] currentStepXs = new double[coefficients.Width];
            double[] initial = initialStep.GetColumn(0);

            double currentAccuracy;

            do
            {
                for (int row = 0; row < matrixSize; row++)
                {
                    currentStepXs[row] = freeMembers[row, 0];
                    for (int column = 0; column < matrixSize; column++)
                    {
                        if (row != column)
                            currentStepXs[row] -= coefficients[row, column] * initial[column];
                    }

                    currentStepXs[row] /= coefficients[row, row];
                }
                currentAccuracy = Math.Abs(initial[0] - currentStepXs[0]);
                for (int row = 0; row < matrixSize; row++)
                {
                    if (Math.Abs(initialStep[row, 0] - currentStepXs[row]) > currentAccuracy)
                        currentAccuracy = Math.Abs(initial[row] - currentStepXs[row]);
                    initial[row] = currentStepXs[row];
                }
            } 
            while (currentAccuracy > precision);

            return initial;
        }

        public double[] Calculate(double[] xs)
        {
            if (xs.Length != coefficients.Width)
                throw new ArgumentException(nameof(xs));

            var answers = new double[coefficients.Height];

            for (int row = 0; row < coefficients.Height; row++)
            {
                answers[row] = GetRowValue(row, xs);
            }

            return answers;
        }

        private double GetRowValue(int row, double[] xs)
        {
            double result = 0;

            for (int column = 0; column < coefficients.Width; column++)
            {
                result += coefficients[row, column] * xs[column];
            }

            return result;
        }
    }
}
