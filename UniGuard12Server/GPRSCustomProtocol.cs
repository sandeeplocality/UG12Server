using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketBase;

namespace UniGuard12Server
{
    class GPRSCustomProtocol : ICustomProtocol<BinaryCommandInfo>
    {
        public ICommandReader<BinaryCommandInfo> CreateCommandReader(IAppServer appServer)
        {
            return new GPRSCommandReader(appServer);
        }

    }
}
