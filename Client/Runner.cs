using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
                IPAddress ipv4Addresses = Array.Find(
                    Dns.GetHostEntry(string.Empty).AddressList,
                    a => a.AddressFamily == AddressFamily.InterNetwork);
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Config.Host);
                IPEndPoint remoteEp = new IPEndPoint(ipv4Addresses, Config.Port);
                Socket client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                client.Connect(remoteEp);

                NetworkStream stream = new NetworkStream(client);
                PacketHandler handler = PacketHandler.Instance;
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                session = new Session(client);

                do
                {
                    ushort size = 0;

                    stream.ReadTimeout = 1000;

                    do
                    {
                        try
                        {
                            size = reader.ReadUInt16();
                            break;
                        }
                        catch (IOException ex)
                        {
                            var innerEx = ex.InnerException as SocketException;

                            if (innerEx == null)
                            {
                                throw ex;
                            }

                            if (innerEx.ErrorCode != (int) SocketError.TimedOut)
                            {
                                throw ex;
                            }
                        }
                    } while (!session.GameStarted || session.Game.Running);

                    stream.ReadTimeout = Timeout.Infinite;

                    if (session.GameStarted && !session.Game.Running)
                    {
                        break;
                    }

                    byte opcodeByte = reader.ReadByte();
                    ushort checkNumber = reader.ReadUInt16();
                    byte[] data = reader.ReadBytes(size);

                    Opcodes opcode = (Opcodes) Enum.ToObject(typeof(Opcodes), opcodeByte);
                    Packet request = new Packet(opcode, data);
                    Packet response = handler.Handle(session, request);

                    if (response == null)
                    {
                        break;
                    }

                    writer.Write((ushort) response.Data.GetLength(0));
                    writer.Write((byte) response.Opcode);
                    writer.Write(checkNumber);
                    writer.Write(response.Data);
                } while (true);

                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (EndOfStreamException)
            {
                session?.Stop();
                MessageBox.Show("Connection closed.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException)
            {
                session?.Stop();
                MessageBox.Show("Connection closed.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                session?.Stop();
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
            finally
            {
                session?.Stop();
            }
        }
    }
}
