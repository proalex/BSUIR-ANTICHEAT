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
                IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEp = new IPEndPoint(ipAddress, 43555);
                Socket client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                client.Connect(remoteEp);

                NetworkStream stream = new NetworkStream(client);
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);
                PacketHandler handler = PacketHandler.Instance;

                session = new Session(client);

                do
                {
                    ushort size = reader.ReadUInt16();
                    byte opcodeByte = reader.ReadByte();
                    ushort checkNumber = reader.ReadUInt16();
                    byte[] data = reader.ReadBytes(size);

                    Opcodes opcode = (Opcodes)Enum.ToObject(typeof(Opcodes), opcodeByte);
                    Packet request = new Packet(opcode, data);
                    Packet response = handler.Handle(session, request);

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
