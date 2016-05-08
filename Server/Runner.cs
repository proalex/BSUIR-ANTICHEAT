using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.Linq;
using Database;
using Server;

public static class Runner
{
    public static DataContext DB;

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
            ChecksGenerator generator = ChecksGenerator.Instance;
            PacketHandler handler = PacketHandler.Instance;
            Session session = new Session(socket.RemoteEndPoint as IPEndPoint);
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);

            do
            {
                if (session.RequestedChecks.Any())
                {
                    ushort size;
                    byte opcodeByte;
                    ushort checkNumber;
                    byte[] data;

                    if (session.Timeout.ElapsedMilliseconds > Config.ResponseTimeout*1000)
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

                        if (innerEx.ErrorCode != (int) SocketError.TimedOut)
                        {
                            throw ex;
                        }

                        IPEndPoint endPoint = socket.RemoteEndPoint as IPEndPoint;
                        Console.WriteLine("IP: {0}:{1} Response timeout.",
                            endPoint.Address, endPoint.Port);
                        break;
                    }

                    Opcodes opcode = (Opcodes) Enum.ToObject(typeof(Opcodes), opcodeByte);
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
        catch (IOException)
        {
            
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

    public static void ConnectToDB()
    {
        DB = new DataContext(
            "Server=" + Config.DBAddress +
            ";Database=" + Config.DBName + 
            ";Trusted_Connection=True;");
    }

    public static int LoadChecks()
    {
        if (DB == null)
        {
            throw new NullReferenceException("DB is null");
        }

        List<Object> checks = new List<Object>();
        Table<ModuleChecks> moduleChecksTbl = DB.GetTable<ModuleChecks>();
        Table<WindowChecks> windowChecksTbl = DB.GetTable<WindowChecks>();
        Table<FileChecks> fileChecksTbl = DB.GetTable<FileChecks>();
        Table<MemoryPatterns> memoryPatternsTbl = DB.GetTable<MemoryPatterns>();
        Table<MemoryChecks> memoryChecksTbl = DB.GetTable<MemoryChecks>();

        var queryWindowChecks =
                from check in windowChecksTbl
                select check;

        var queryModuleChecks =
                from check in moduleChecksTbl
                select check;

        var queryFileChecks =
                from check in fileChecksTbl
                select check;

        var queryMemoryPatterns =
                from check in memoryPatternsTbl
                select check;

        var queryMemoryChecks =
                from check in memoryChecksTbl
                select check;

        checks.AddRange(queryWindowChecks);
        checks.AddRange(queryModuleChecks);
        checks.AddRange(queryFileChecks);
        checks.AddRange(queryMemoryPatterns);
        checks.AddRange(queryMemoryChecks);
        ChecksGenerator.Instance.LoadChecks(checks);
        return checks.Count;
    }

    public static void Main(string[] args)
    {
        IPAddress ipv4Addresses = Array.Find(
                    Dns.GetHostEntry(string.Empty).AddressList,
                    a => a.AddressFamily == AddressFamily.InterNetwork);
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Config.Host);
        IPEndPoint localEndPoint = new IPEndPoint(ipv4Addresses, Config.Port);
        Socket listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        Console.WriteLine("Loading checks from database...");
        ConnectToDB();
        Console.WriteLine("{0} checks loaded.", LoadChecks());

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(Config.Port);
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