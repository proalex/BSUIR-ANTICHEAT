using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    public class Runner
    {
        public static void Main(string[] args)
        {
            Session session = null;

            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 43555);
                Socket client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                client.Connect(remoteEP);

                NetworkStream stream = new NetworkStream(client);
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                session = new Session(client);

                PacketHandler handler = new PacketHandler(session);

                do
                {
                    ushort size = reader.ReadUInt16();
                    byte opcodeByte = reader.ReadByte();
                    byte[] data = new byte[size];

                    data = reader.ReadBytes(size);

                    Opcodes opcode = (Opcodes)Enum.ToObject(typeof(Opcodes), opcodeByte);
                    Packet request = new Packet(opcode, data);

                    Packet response = handler.Handle(request);

                    if (response == null)
                    {
                        break;
                    }

                    writer.Write((ushort)response.Data.GetLength(0));
                    writer.Write((ushort)response.Opcode);
                    writer.Write(response.Data);
                } while (true);

                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
            }
            finally
            {
                if (session != null)
                {
                    session.Stop();
                }
            }
        }
    }
}
