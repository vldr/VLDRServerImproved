using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vldrChatServer
{
    class TcpServer
    {
        private TcpListener _server;
        private Boolean _isRunning;

        private List<TcpClient> _userList = new List<TcpClient>();
        private List<string> _userListNickName = new List<string>();

        public TcpServer(int port)
        {
            Console.Clear();
            Console.WriteLine("[vldrChat Server v1.0.0]");

            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();

            _isRunning = true;
            Console.WriteLine(">> Server has successfully started!");

            LoopClients();
        }

        public void LoopClients()
        {
            while (_isRunning)
            {
                TcpClient newClient = _server.AcceptTcpClient();

                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(newClient);
            }
        }

        public void MessageAll(String msg)
        {
            try
            {
                foreach (var user in _userList)
                {
                    if (user.Connected)
                    {
                        StreamWriter userWriter = new StreamWriter(user.GetStream(), Encoding.ASCII);

                        userWriter.WriteLine(msg);
                        userWriter.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">> Strange error occurred at MessageAll function... [" + ex.Message + "]");
            }
        }

        public void MessageSpecific(String msg, TcpClient c)
        {
            try
            {
                if (c.Connected)
                {
                    StreamWriter userWriter = new StreamWriter(c.GetStream(), Encoding.ASCII);

                    userWriter.WriteLine(msg);
                    userWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">> Strange error occurred at MessageSpecific function... [" + ex.Message + "]");
            }
        }

        public void ProcessCommands(String cmd, TcpClient c)
        {
            try
            {
                if (c.Connected)
                {
                    Socket s = c.Client;
                    IPEndPoint remoteIpEndPoint = s.RemoteEndPoint as IPEndPoint;

                    if (cmd.Equals("!help"))
                    {
                        MessageSpecific("[----------------]\n[!help : shows this menu]\n[!setnick newnick : sets your new nickname]\n[!time : shows the time]\n[!pm nick message : messages the port]\n[!clear : clears messages]\n[----------------]", c);
                        return;
                    }

                    if (cmd.Equals("!time"))
                    {
                        MessageSpecific("[Server time is currently: " + DateTime.Now.ToString("h:mm:ss tt") + "]", c);
                        return;
                    }

                    if (cmd.Equals("!clear") || cmd.Equals("!cls"))
                    {
                        MessageSpecific("{clear}", c);
                        return;
                    }

                    if (cmd.StartsWith("!setnick ")) 
                    {
                        string[] args = cmd.Split(' ');

                        if (args.Length > 1)
                        { 
                            foreach (var item in _userListNickName)
                            {
                                if (item == args[1])
                                {
                                    MessageSpecific("Error! Nickname is already taken...", c);
                                    return;
                                }
                            }

                            if ((string.Concat(args[1].Where(char.IsLetterOrDigit))) != String.Empty && (string.Concat(args[1].Where(char.IsLetterOrDigit))).Length < 10)
                            {
                                _userListNickName[_userList.FindIndex(u => u.Client == c.Client)] = (string.Concat(args[1].Where(char.IsLetterOrDigit)));
                                UpdatePlayerList(c);
                            } 
                            else
                            {
                                MessageSpecific("Error! Invalid characters or too long!", c);
                                return;
                            }

                            return;
                        }
                    }

                    if (cmd.StartsWith("!pm ") )
                    {
                        string[] args = cmd.Split(' ');

                        if (args.Length > 2)
                        {
                            int j = 0;
                            foreach (var user in _userList)
                            {
                                IPEndPoint remoteIpEndPointTemp = user.Client.RemoteEndPoint as IPEndPoint;

                                if (user.Connected && _userListNickName[j] == args[1])
                                {
                                    String finalMessage = "";

                                    int i = 0;
                                    foreach (var arg in args)
                                    {
                                        i++;

                                        if (i > 2)
                                            finalMessage += arg + " ";
                                    }

                                    MessageSpecific(">> Client (" + _userListNickName[_userList.FindIndex(u => u.Client == c.Client)] + ") (PM): " + finalMessage, user);
                                    MessageSpecific("[You messaged (" + args[1] + ") successfully!]", c);
                                }
                                j++;
                            }
                        }
                        else
                        {
                            MessageSpecific("[Invalid arguments...]", c);
                        }

                        return;
                    }

                     
                    Console.WriteLine(">> Client (" + remoteIpEndPoint.Address + ":" + remoteIpEndPoint.Port + ") (" + _userListNickName[_userList.FindIndex(u => u.Client == c.Client)] + "): " + cmd);
                    MessageAll("Client (" + _userListNickName[_userList.FindIndex(u => u.Client == c.Client)] + "): " + cmd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">> Strange error occurred at ProcessCommands function... [" + ex.StackTrace + "]");
            }
        }

        public void UpdatePlayerList(TcpClient thisClient)
        {
            try
            {
                String kirka = "{player_list:(";

                if (_userList.Any())
                {
                    var last = _userList.Last();

                    int i = 0;
                    foreach (var user in _userList)
                    {
                        if (user.Connected)
                        {
                            IPEndPoint remoteIpEndPointTemp = user.Client.RemoteEndPoint as IPEndPoint;

                            if (user == last)
                                kirka += _userListNickName[i];
                            else
                                kirka += _userListNickName[i] + ",";
                        }
                        i++;
                    }

                    kirka += ")}";

                    MessageAll(kirka);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">> Strange error occurred at UpdatePlayerList function... [" + ex.Message + " " + ex.StackTrace + "]");
            }
        }

        public void HandleClient(object obj)
        { 
            TcpClient client = (TcpClient)obj;
            Socket s = client.Client;

            Boolean bClientConnected = true;
            IPEndPoint remoteIpEndPoint = s.RemoteEndPoint as IPEndPoint;
            IPEndPoint localIpEndPoint = s.LocalEndPoint as IPEndPoint;

            _userList.Add(client);
            _userListNickName.Add(remoteIpEndPoint.Port.ToString());

            StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);

            Console.WriteLine("[Client connected: " + remoteIpEndPoint.Address + ":" + remoteIpEndPoint.Port + "]");
            MessageAll("[Client (" + remoteIpEndPoint.Port + ") has connected to the server!]");

            UpdatePlayerList(client);

            String sData;
            while (bClientConnected)
            {
                try
                { 
                    sData = sReader.ReadLine();

                    if ( sData == null )
                    {
                        Console.WriteLine("[Client (" + remoteIpEndPoint.Address + ":" + remoteIpEndPoint.Port + ") has been dropped...]");
                        MessageAll("[Client (" + remoteIpEndPoint.Port + ") has been dropped from the server...]");

                        _userListNickName.Remove(_userListNickName[_userList.FindIndex(u => u.Client == client.Client)]);
                        _userList.Remove(client);

                        bClientConnected = false;

                        UpdatePlayerList(client);
                        return; 
                    }

                    ProcessCommands(sData, client);


                }
                catch (IOException)
                {
                    Console.WriteLine("[Client (" + remoteIpEndPoint.Address + ":" + remoteIpEndPoint.Port + ") has been dropped...]");
                    MessageAll("[Client (" + remoteIpEndPoint.Port + ") has been dropped from the server...]");

                    _userListNickName.Remove(_userListNickName[_userList.FindIndex(u => u.Client == client.Client)]);
                    _userList.Remove(client);
                   

                    bClientConnected = false;

                    UpdatePlayerList(client);
                }
            }
        }
    }
}
