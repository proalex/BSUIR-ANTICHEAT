using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Server;

public static class Runner
{
    public static void ClientHandler(Object obj)
    {
        if (obj == null)
        {
            throw new NullReferenceException("obj is null");
        }

        Socket socket = obj as Socket;

        if (socket == null)
        {
            throw new InvalidDataException("obj is not instance of Socket type.");
        }

        try
        {
            NetworkStream stream = new NetworkStream(socket);
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);
            ChecksGenerator generator = ChecksGenerator.Instance;
            PacketHandler handler = PacketHandler.Instance;
            Session session = new Session(socket.RemoteEndPoint as IPEndPoint);

            stream.ReadTimeout = 30000;

            do
            {
                if (session.RequestedChecks.Any())
                {
                    ushort size;
                    byte opcodeByte;
                    ushort checkNumber;
                    byte[] data;

                    if (session.Timeout.ElapsedMilliseconds * 1000 > 30)
                    {
                        break;
                    }

                    try
                    {
                        size = reader.ReadUInt16();
                        opcodeByte = reader.ReadByte();
                        checkNumber = reader.ReadUInt16();
                        data = reader.ReadBytes(size);
                    }
                    catch (IOException ex)
                    {
                        var innerEx = ex.InnerException as SocketException;

                        if (innerEx == null)
                        {
                            throw ex;
                        }

                        if (innerEx.ErrorCode != (int)SocketError.TimedOut)
                        {
                            throw ex;
                        }

                        IPEndPoint endPoint = socket.RemoteEndPoint as IPEndPoint;
                        Console.WriteLine("IP: {0}:{1} Response timeout.",
                            endPoint.Address, endPoint.Port);
                        break;
                    }
                    
                    Opcodes opcode = (Opcodes)Enum.ToObject(typeof(Opcodes), opcodeByte);
                    Packet response = new Packet(opcode, data, checkNumber);

                    if (!handler.Handle(session, response))
                    {
                        break;
                    }
                }
                else
                {
                    Packet[] checks = generator.Generate(session);

                    if (checks.Length == 0)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    foreach (var check in checks)
                    {
                        writer.Write((ushort) check.Data.Length);
                        writer.Write((byte) check.Opcode);
                        writer.Write(check.Number);
                        writer.Write(check.Data);
                    }

                    session.Timeout.Reset();
                    session.Timeout.Start();
                }
            } while (true);
        }
        catch (Exception ex)
        {
            IPEndPoint endPoint = socket.RemoteEndPoint as IPEndPoint;
            Console.WriteLine("IP: {0}:{1} Exception caught: {2}", 
                endPoint.Address, endPoint.Port, ex.Message);
        }
        finally
        {
            IPEndPoint endPoint = socket.RemoteEndPoint as IPEndPoint;
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            Console.WriteLine("IP: {0}:{1} Connection closed.", 
                endPoint.Address, endPoint.Port);
        }
    }

    public static void Main(string[] args)
    {
        IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 43555);
        Socket listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(43555);
            Console.WriteLine("Waiting for a connection...");

            while (true)
            {
                Thread clientHandler = new Thread(ClientHandler);
                Socket socket = listener.Accept();
                IPEndPoint endPoint = socket.RemoteEndPoint as IPEndPoint;

                Console.WriteLine("Incoming connection from {0}:{1}.",
                    endPoint.Address, endPoint.Port);
                clientHandler.Start(socket);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            Console.ReadLine();
        }
        finally
        {
            listener?.Close();
        }
    }
}