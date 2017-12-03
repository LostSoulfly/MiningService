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
        public int Id;
        public bool isIdle;
        public int request;
        public string data;

        public override string ToString()
        {
            return string.Format("\"{0}\" \"{3}\" (message ID = {1})", isIdle, Id, request);
        }
    }
}
