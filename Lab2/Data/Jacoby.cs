using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MPI;

namespace Lab2.Data
{
    [Serializable]
    public class Jacoby
    {
        private Matrix coefficients;
        private Matrix freeMembers;

        private Matrix initialStep;

        private double precision;

        public Jacoby(Matrix matrix, Matrix initialStep, double precision)
        {
            this.coefficients = Matrix.Cut(matrix, matrix.Height, matrix.Width - 1);
            this.freeMembers = Matrix.TakeColumn(matrix, matrix.Width - 1);

            this.initialStep = initialStep;

            this.precision = precision;
        }

        public static void MPIWorker(Communicator communicator)
        {
            Jacoby jacoby;
            ReceiveRequest request;
            using (var perf = new Performance($"{communicator.Rank} wait for Jacoby "))
            {
                request = communicator.ImmediateReceive<Jacoby>(0, Consts.JacobyMessage);
                request.Wait();
                jacoby = request.GetValue() as Jacoby;
            }

            bool inProcess = true;

            while (inProcess)
            {
                using (var perf = new Performance($"{communicator.Rank} cycle in "))
                {
                    request = communicator.ImmediateReceive<JobToDo>(0, Consts.CalculatePart);
                    request.Wait();
                    JobToDo jobToDo = request.GetValue() as JobToDo;
                    if (jobToDo.finish)
                        return;

                    var result = jacoby.PartialCalculation(jobToDo.start, jobToDo.end, jobToDo.initial);
                    communicator.ImmediateSend<JobDone>(new JobDone(result), 0, Consts.JobDone);
                }
            }
        }

        public double[] MPIHead(Communicator communicator)
        {
            for (int r = 1; r < communicator.Size; r++)
            {
                communicator.ImmediateSend<Jacoby>(this, r, Consts.JacobyMessage);
            }

            int parts = communicator.Size - 1;

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

                    var jobToDo = new JobToDo(start, end, initial);
                    communicator.ImmediateSend(jobToDo, part + 1, Consts.CalculatePart);
                }

                ReceiveRequest[] requests = new ReceiveRequest[parts];

                for (int part = 0; part < parts; part++)
                {
                    requests[part] = communicator.ImmediateReceive<JobDone>(part + 1, Consts.JobDone);
                }

                using (var perf = new Performance($"Wait for sync in "))
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
            while (currentAccuracy > precision);

            for (int i = 0; i < parts; i++)
                communicator.ImmediateSend<JobToDo>(new JobToDo(true), i + 1, Consts.CalculatePart);

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
