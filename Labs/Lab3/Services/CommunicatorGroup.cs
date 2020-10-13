using MPI;

namespace Lab3.Services
{
    internal class CommunicatorGroup
    {
        public CommunicatorGroup(Intracommunicator communicator)
        {
            this.Communicator = communicator;
        }

        public int Current => Communicator.Rank;

        public int Total => Communicator.Size;

        public bool IsLeftHalf => Current < Total / 2;

        public Intracommunicator Communicator { get; }
    }
}
