using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using MMORPG_Server.Config;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Npgsql;
using System.Text.Json;

namespace MMORPG_Server
{
    [Serializable]
    public class ConnectionManager
    {
        private NpgsqlConnection connection;
        private List<Tuple<string, UdpClient>> udp;
        private TcpListener listener;
        public bool enabled;
        Thread th;
        public Dictionary<string, Player> players;
        public MainWindow window;
        public List<string> logged = new List<string>();

        private readonly object key = new object();
        
        public ConnectionManager()
        {
            enabled = true;
            udp = new List<Tuple<string, UdpClient>>();
            listener = new TcpListener(IPAddress.Parse(Configuration.TCP_SERVER_ADDR), Configuration.TCP_SERVER_PORT);
            listener.Start();
            th = new Thread(StartConnect);
            th.Start();
            connection = new NpgsqlConnection(Configuration.GetDBConnectionString());
            players = new Dictionary<string, Player>();
        }
        public void SetWindow(MainWindow mainWindow)
        {
            lock (key)
            {
                window = mainWindow;
            }
        }
        public void Stop()
        {
            lock (key)
            {
                enabled = false;
            }            
        }
        
        private void AddPlayer(string token, Player p, int i = -1)
        {
            lock (key)
            {
                if (i < 0)
                {
                    players.Add(token, p);
                    logged.Add(p.username);
                }
                else
                {
                    logged.Remove(players[token].username);
                    players.Remove(token);
                }
              //  window.UpdatePlayerList(players);
        //        Delegate c = Delegate.CreateDelegate(typeof(void), window, "UpdateList");
                window.UpdateL(players.Select(pl => pl.Value).ToArray());
            }
        }

        private void SetPlayer(string token, Player p)
        {
            lock (key)
            {
                players[token] = p;
            //    Delegate c = Delegate.CreateDelegate(typeof(void), window, "UpdateList");
                window.UpdateL(players.Select(pl => pl.Value).ToArray());
            }
        }

        public async void UDPSendReceive()
        {
            List<byte[]> Data = new List<byte[]>();
            while (enabled)
            {
                while (Data.Count < udp.Count)
                    Data.Add(new byte[10]);
                if (!enabled)
                    break;
                else
                {
                    for(int i = 0; i < udp.Count; i++)
                    {
                        var data = await udp[i].Item2.ReceiveAsync();
                        int count = BitConverter.ToInt32(data.Buffer.Take(sizeof(int)).ToArray(), 0);
                        byte[] buffer = data.Buffer.Skip(sizeof(int)).ToArray();
                        string token = Encoding.ASCII.GetString(buffer.Take(count).ToArray());
                        buffer = buffer.Skip(count).ToArray();
                        Player player = Player.NewPlayer(Encoding.ASCII.GetString(buffer));

                   //     Predicate<Player> predicate = new Predicate<Player>((p) => { return (p.username.Equals(player.username)) ? true : false; });

                        // player.token

                        if (players.ContainsKey(token))
                        {
                            if (!players[token].Equals(player))
                            {
                                SetPlayer(token, player);
                            }
                            Data[i] = Encoding.ASCII.GetBytes(player.ToSendable());
                        }
                    }
                    for (int i = 0; i < udp.Count; i++)
                    {
                        byte[] buff = Data.SelectMany(d => d).ToArray();
                        var data = await udp[i].Item2.SendAsync(buff, buff.Length);
                    }
                }
            }
        }

        private static bool FindUser(Player p1, Player p2)
        {
            if (p1.username.Equals(p2.username))
                return true;
            return false;
        }

        private async Task Connecting()
        {
            TcpClient client = await listener.AcceptTcpClientAsync();

            DateTime now = DateTime.Now;
            while (client.Available <= 0)
            {
                if ((now - DateTime.Now).TotalSeconds > 5)
                    break;
            }
            if (client.Available > 0)
            {
                NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[sizeof(int)];
                await stream.ReadAsync(buffer, 0, buffer.Length);
                int mode = BitConverter.ToInt32(buffer, 0);
                if (mode == 2)
                {
                    buffer = new byte[sizeof(int)];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                    int count = BitConverter.ToInt32(buffer, 0);
                    buffer = new byte[count];
                    if (count > 0)
                        await stream.ReadAsync(buffer, 0, count);

                    //    await stream.WriteAsync(new byte[] { 1 }, 0, 1);


                    string user = Encoding.ASCII.GetString(buffer);
                    Console.WriteLine(user);


                    connection.Open();
                    NpgsqlCommand t = connection.CreateCommand();
                    t.CommandText = "SELECT * FROM player WHERE username = '" + user + "';";

                    var reader = t.ExecuteReader();

                    Player player = null;
                    reader.Read();
                    if (reader.IsOnRow)
                    {
                        object[] o = new object[reader.FieldCount];
                        reader.GetValues(o);
                        player = new Player(o[1].ToString(), o[2].ToString());
                        player.position = o[3] as float[];
                        player.rotation = o[4] as float[];
                        player.inventory = JsonSerializer.Deserialize<Dictionary<string, int>>((string)o[5]);
                        player.skills = JsonSerializer.Deserialize<Dictionary<string, int>>((string)o[6]);
                    }
                    connection.Close();

                    buffer = new byte[sizeof(int)];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                    count = BitConverter.ToInt32(buffer, 0);

                    buffer = new byte[count];
                    if (count > 0)
                        await stream.ReadAsync(buffer, 0, count);

                    //    byte[] pass = buffer;
                    //-----------------------------
                    Console.WriteLine(Encoding.ASCII.GetString(buffer));
                    Aes aes = Aes.Create();
                    aes.Key = Encoding.ASCII.GetBytes("?^\f?\u0016G?\fg\u0002????^8?2?f\a??\u001d?\u0001??Q???");
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] pass = aes.CreateDecryptor().TransformFinalBlock(buffer, 0, buffer.Length);


                    //-----------------------------
                    Console.WriteLine(Encoding.ASCII.GetString(pass));

                    /**-1 -- wrong username
                     * -2 -- no password
                     * -3 -- wrong password
                     * -4 -- alredy logged in
                     * */

                    if (player == null)
                    {
                        buffer = BitConverter.GetBytes((int)-1);
                        await stream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        string pasas = Encoding.ASCII.GetString(pass);
                        if (player.password.Equals(pasas))
                        {
                            var client_ip = (client.Client.LocalEndPoint as IPEndPoint);
                            client_ip.Port = Configuration.UDP_SERVER_PORT;
                            udp.ForEach((u) => { if ((u.Item2.Client.LocalEndPoint as IPEndPoint).Address.Equals(client_ip.Address)) if ((u.Item2.Client.LocalEndPoint as IPEndPoint).Port.Equals(client_ip.Port)) client_ip.Port += 1; });
                            if (!logged.Contains(user))
                            {
                                string token = GenerateToken(); 

                                byte[] send = BitConverter.GetBytes(client_ip.Port);
                                byte[] send2 = Encoding.ASCII.GetBytes(player.ToSendable());
                                byte[] tokenBytes = Encoding.ASCII.GetBytes(token);
                                buffer = BitConverter.GetBytes(send.Length).Concat(send).Concat(BitConverter.GetBytes(send2.Length)).Concat(send2).Concat(BitConverter.GetBytes(tokenBytes.Length)).Concat(tokenBytes).ToArray();
                                await stream.WriteAsync(buffer, 0, buffer.Length);

                                udp.Add(new Tuple<string, UdpClient>(token, new UdpClient(client_ip)));
                                AddPlayer(token, player);
                                Console.WriteLine("List size = " + udp.Count.ToString());
                            }
                            else
                            {
                                buffer = BitConverter.GetBytes((int)-4);
                                await stream.WriteAsync(buffer, 0, buffer.Length);
                            }
                        }
                        else
                        {
                            buffer = BitConverter.GetBytes((int)-3);
                            await stream.WriteAsync(buffer, 0, buffer.Length);
                        }

                    }
                }
                else if (mode == 1)
                { 
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                    int count = BitConverter.ToInt32(buffer, 0);
                    buffer = new byte[count];
                    if (count > 0)
                        await stream.ReadAsync(buffer, 0, count);


                    string user = Encoding.ASCII.GetString(buffer);
                    buffer = new byte[sizeof(int)];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                    count = BitConverter.ToInt32(buffer, 0);

                    buffer = new byte[count];
                    if (count > 0)
                        await stream.ReadAsync(buffer, 0, count);

                    connection.Open();
                    NpgsqlCommand t = connection.CreateCommand();
                    t.CommandText = "SELECT * FROM player WHERE username = '" + user + "';";
                    bool exists = t.ExecuteReader().HasRows;
                    connection.Close();

                    if (!exists)
                    {
                        //    pass decryption
                        //-----------------------------
                        Aes aes = Aes.Create();
                        aes.Key = Encoding.ASCII.GetBytes("?^\f?\u0016G?\fg\u0002????^8?2?f\a??\u001d?\u0001??Q???");
                        aes.Mode = CipherMode.ECB;
                        aes.Padding = PaddingMode.PKCS7;
                        byte[] pass = aes.CreateDecryptor().TransformFinalBlock(buffer, 0, buffer.Length);
                        //-----------------------------                       

                        Player player = new Player(user, Encoding.ASCII.GetString(pass));

                        connection.Open();
                        t = connection.CreateCommand();
                        t.CommandText = player.GetSql();
                        count = 0;
                        count = await t.ExecuteNonQueryAsync();
                        connection.Close();
                        if (count == 1)
                        {
                            buffer = BitConverter.GetBytes(1);
                            await stream.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        buffer = BitConverter.GetBytes(-1);
                        await stream.WriteAsync(buffer, 0, buffer.Length);
                    }
                }
                else if (mode == 0)
                {
                    buffer = new byte[sizeof(int)];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                    int count = BitConverter.ToInt32(buffer, 0);

                    buffer = new byte[count];
                    await stream.ReadAsync(buffer, 0, buffer.Length);

                    string token = Encoding.ASCII.GetString(buffer);
                    // save to db
                    connection.Open();
                    NpgsqlCommand t = connection.CreateCommand();
                    t.CommandText = players[token].UpdateSql();
                    count = await t.ExecuteNonQueryAsync();
                    connection.Close();
                    // remove from udp
                    players.Remove(token);
                    int index = udp.FindIndex(u => u.Item1 == token);
                    udp[index].Item2.Close();
                    udp[index].Item2.Dispose();
                    udp.RemoveAt(index);
                }
            }
            client.Close();
        }
        private string GenerateToken()
        {
            Random random = new Random();
            string[] symbols = new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "p", "q", "r", "o",
                                              "s", "t", "u", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            string result = "";
            for (int i = 0; i < 15; i++)
                result += symbols[random.Next(0, 15)];
            if (players.ContainsKey(result))
                return GenerateToken();
            else
                return result;
        }
        public async void StartConnect()
        {
            while (enabled)
            {
                if (!enabled)
                    break;
                else
                {
                    /// listen for connections and check password then if correct add to list
                    
                    if (listener.Pending())
                    {
                        await Task.Run(Connecting);
                    }
                }
            }
            th.Join();
        }        
    }
}
