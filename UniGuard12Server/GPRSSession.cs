using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;

namespace UniGuard12Server
{
    public class GPRSSession : AppSession<GPRSSession, BinaryCommandInfo>
    {
        public new GPRSServer AppServer
        {
            get
            {
                return (GPRSServer)base.AppServer;
            }
        }
    }
}
