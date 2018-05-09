using System;

namespace Message
{
    [Serializable]
    internal class IdleMessage
    {
        public string data;

        public bool isIdle;

        //This class is used to pass messages through the NamedPipe between the Service and the IdleMon program running on the active Windows session
        public int packetId;

        public int requestId;

        public override string ToString()
        {
            return string.Format("\"{0}\" \"{3}\" (message ID = {1})", isIdle, packetId, requestId);
        }
    }
}