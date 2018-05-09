using System;

namespace Message
{
    [Serializable]
    internal class IdleMessage
    {
        public string data;
        public bool isIdle;
        public int packetId;
        public int requestId;

        public override string ToString()
        {
            return string.Format("\"{0}\" \"{3}\" (message ID = {1})", isIdle, packetId, requestId);
        }
    }
}