using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    [Serializable]
    class IdleMessage
    {
        //This class is used to pass messages through the NamedPipe between the Service and the IdleMon program running on the active Windows session
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
