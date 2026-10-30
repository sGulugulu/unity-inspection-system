using System;

namespace Inspection.Client
{
    [Serializable]
    public sealed class InspectionTask
    {
        public string id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public Position position;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public struct Position
    {
        public float x;
        public float y;
        public float z;
    }
}
