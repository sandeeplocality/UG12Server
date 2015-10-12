using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace UniGuard12Server
{
    /// </summary>
    class GPRSCommandReader : CommandReaderBase<BinaryCommandInfo>
    {

        public GPRSCommandReader(IAppServer appServer)
            : base(appServer)
        {

        }

        public override BinaryCommandInfo FindCommandInfo(IAppSession session, byte[] readBuffer, int offset, int length, bool isReusableBuffer, out int left)
        {
            left = 0;


            this.AddArraySegment(readBuffer, offset, length, isReusableBuffer);

            if (length < 14)
            {

                BufferSegments.ClearSegements();
                return null;
            }

            var commandData = BufferSegments.ToArrayData(0, length);
            BufferSegments.ClearSegements();

            return new BinaryCommandInfo("WMDEVICE", commandData);

        }
    }
}
