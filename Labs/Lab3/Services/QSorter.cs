using System;

using MPI;

namespace Lab3.Services
{
    internal static class Tags
    {
        public const int ArrayPart = 0;
        public const int Pivot = 1;
        public const int Exchange = 2;
        public const int Collect = 3;
    }

    internal class QSorter
    {
        private CommunicatorGroup globalGroup;
        private CommunicatorGroup group;

        private QSArray currentArray;
        private int currentPivot;

        public QSorter(Intracommunicator communicator)
        {
            this.globalGroup = new CommunicatorGroup(communicator);
            this.group = globalGroup;
        }

        public bool LastInGroup => group.Total == 1;

        private bool IsGroupManager => group.Current == 0;

        private bool IsGlobalManager => globalGroup.Current == 0;

        internal void InitializeWithData(QSArray qsArray)
        {
            if (IsGroupManager)
                SendSlicedArrayToAllSubProcesses(qsArray);

            currentArray = ReceiveArrayPartFromManager(Tags.ArrayPart);
        }

        internal void PivotBroadcast()
        {
            if (IsGroupManager)
                currentPivot = currentArray.GetPivot();

            group.Communicator.Broadcast(ref currentPivot, 0);
        }

        internal void PartitionAndPartsExchange()
        {
            currentArray.Partition(currentPivot, out QSArray low, out QSArray high);

            int partner = group.IsLeftHalf ?
                group.Current + group.Total / 2 :
                group.Current - group.Total / 2;

            if (group.IsLeftHalf)
            {
                group.Communicator.ImmediateSend<int[]>(high.GetContent(), partner, Tags.Exchange);
                high = ReceiveArrayFrom(partner, Tags.Exchange);
            }
            else
            {
                group.Communicator.ImmediateSend<int[]>(low.GetContent(), partner, Tags.Exchange);
                low = ReceiveArrayFrom(partner, Tags.Exchange);
            }

            currentArray = QSArray.Merge(low, high);
        }

        internal void GroupHalfToSubGroup()
        {
            var newGroup = group.IsLeftHalf ? 0 : 1;

            var newCommunicator = (Intracommunicator)group.Communicator.Split(newGroup, 0); 
            group = new CommunicatorGroup(newCommunicator);
        }

        internal void Sort()
        {
            QuickSort.Sort(currentArray.List, 0, currentArray.List.Count - 1);
        }

        internal void SendWorkResult()
        {
            var request = globalGroup.Communicator.ImmediateSend<int[]>(currentArray.GetContent(), 0, Tags.Collect);
        }

        internal int[] MergeDataFromWorkers()
        {
            if (IsGlobalManager)
            {
                QSArray result = null;
                for (int process = 0; process < globalGroup.Total; process++)
                {
                    var array = ReceiveGlobalArrayFrom(process, Tags.Collect);

                    if (result == null)
                        result = array;
                    else
                        result = QSArray.Merge(result, array);
                }

                return result.GetContent();
            }
            else throw new InvalidOperationException($"Not Global Manager");
        }

        private QSArray ReceiveGlobalArrayFrom(int partner, int tag)
        {
            var content = globalGroup.Communicator.Receive<int[]>(partner, tag);
            return new QSArray(content);
        }

        private QSArray ReceiveArrayFrom(int partner, int tag)
        {
            var content = group.Communicator.Receive<int[]>(partner, tag);
            return new QSArray(content);
        }

        private QSArray ReceiveArrayPartFromManager(int tag)
        {
            var content = group.Communicator.Receive<int[]>(0, tag);
            return new QSArray(content);
        }

        private void SendSlicedArrayToAllSubProcesses(QSArray qsArray)
        {
            for (int process = 0; process < globalGroup.Communicator.Size; process++)
            {
                var part = qsArray.GetPart(process, globalGroup.Communicator.Size);
                var request = globalGroup.Communicator.ImmediateSend<int[]>(part.GetContent(), process, Tags.ArrayPart);
            }
        }
    }
}
