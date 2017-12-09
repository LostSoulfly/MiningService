using System;

namespace Message
{
    [Serializable]
    internal class IdleMessage
    {
        public int packetId;
        public int requestId;
        public bool isIdle;
        public string data;

        public override string ToString()
        {
            return string.Format("\"{0}\" \"{3}\" (message ID = {1})", isIdle, packetId, requestId);
        }
    }
}