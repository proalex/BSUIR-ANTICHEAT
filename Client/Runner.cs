using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Client
{
    public class Runner
    {
        public static void Main(string[] args)
        {
            Session session = null;

            try
            {
                var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                var ipAddress = ipHostInfo.AddressList[0];
                var remoteEP = new IPEndPoint(ipAddress, 43555);
                var client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                client.Connect(remoteEP);

                var stream = new NetworkStream(client);
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                session = new Session(client);

                var handler = new PacketHandler(session);

                do
                {
                    var size = reader.ReadUInt16();
                    var opcodeByte = reader.ReadByte();
                    var checkNumber = reader.ReadUInt16();
                    var data = new byte[size];

                    data = reader.ReadBytes(size);

                    var opcode = (Opcodes)Enum.ToObject(typeof(Opcodes), opcodeByte);
                    var request = new Packet(opcode, data);
                    var response = handler.Handle(request);

                    if (response == null)
                    {
                        break;
                    }

                    writer.Write((ushort)response.Data.GetLength(0));
                    writer.Write((ushort)response.Opcode);
                    writer.Write(checkNumber);
                    writer.Write(response.Data);
                } while (true);

                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                session?.Stop();
                MessageBox.Show(e.Message, "Error");
            }
            finally
            {
                session?.Stop();
            }
        }
    }
}
