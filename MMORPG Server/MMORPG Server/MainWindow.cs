using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MMORPG_Server.Config;
using System.Text.Json;
using System.IO;

namespace MMORPG_Server
{
    public partial class MainWindow : Form
    {
        public delegate void Window(Player[] p);
        ConnectionManager cm = new ConnectionManager();
        private readonly object key = new object();

        public MainWindow()
        {            
            InitializeComponent();
            players.Items.Clear();
            cm.SetWindow(this);
        }

        public void UpdateL(Player[] p)
        {
            Window window = UpdateList;
            players.Invoke(window, new object[] { p as Player[] });
        }
       
        public void UpdateList(Player[] p)
        {
            players.Items.Clear();
            players.BeginUpdate();
         //   ListBox.ObjectCollection collection = new ListBox.ObjectCollection(players);
            p.Select(pp => pp.username).ToList().ForEach(u => players.Items.Add(u));
        //    collection.AddRange(users);
       //     players.Items.Add(collection);
            players.EndUpdate();
        //    players.Update();
        }
        private void MainWindow_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Maximised)
            {
                WindowState = FormWindowState.Maximized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else if (Properties.Settings.Default.Minimised)
            {
                WindowState = FormWindowState.Minimized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else
            {
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
        }
        private void MainWindow_Close(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximised = true;
                Properties.Settings.Default.Minimised = false;
            }
            else if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Size = Size;
                Properties.Settings.Default.Maximised = false;
                Properties.Settings.Default.Minimised = false;
            }
            else
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximised = false;
                Properties.Settings.Default.Minimised = true;
            }
            Properties.Settings.Default.Save();
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            //cm.Stop();


            
        }

        private void Players_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (players.SelectedIndex >= 0)
            {
                string user = players.SelectedItem.ToString();
                PlayerInfo playerInfo = new PlayerInfo(cm.players.First(p => p.username.Equals(user)));
                playerInfo.ShowDialog();
            }
        }
        
    }
    public class Player
    {
        public string username { get; set; }
        public readonly string password;
        public float[] position { get; set; }
        public float[] rotation { get; set; }
        public Dictionary<string, int> inventory { get; set; }
        public Dictionary<string, int> skills { get; set; }
        public string token { get; set; }
        public Player()
        {
            password = "";
            position = new float[3];
            rotation = new float[3];
            inventory = new Dictionary<string, int>();
            skills = new Dictionary<string, int>();
        }
        public Player(string user, string pass)
        {
            username = user; password = pass;
            position = new float[3];
            rotation = new float[3];
            inventory = new Dictionary<string, int>();
            inventory.Add("item1", 1);
            inventory.Add("item2", 2);
            skills = new Dictionary<string, int>();
            skills.Add("str", 1);
            skills.Add("def", 1);
        }
        
        public override string ToString()
        {
            string str = "Username: " + username + "; Password: " + password + "; Position: { x: " + position[0] + "   y: " + position[1] + "   z: " + position[2] + " }; Rotation: { x: " + rotation[0] + "   y: " + rotation[1] + "   z: " + rotation[2] + " }; Inventory: ";
            if (inventory.Count > 0)
                inventory.ToList().ForEach(i => { str += i.Key + " " + i.Value.ToString() + ", "; });
            str += "; Skills: ";
            if (skills.Count > 0)
                skills.ToList().ForEach(i => { str += i.Key + " " + i.Value.ToString() + ", "; });
            return str;
        }
        
        public string ToSendable()
        {
            string space = "\u001f";
            string positionStr = position[0] + ";" + position[1] + ";" + position[2];
            string rotationStr = rotation[0] + ";" + rotation[1] + ";" + rotation[2];
            string inventoryStr = "";
            inventory.ToList().ForEach(i => inventoryStr += i.Key + "=" + i.Value.ToString() + ";");
            if (inventoryStr.Length > 1)
                inventoryStr = inventoryStr.Substring(0, inventoryStr.Length - 1);
            string skillsStr = "";
            skills.ToList().ForEach(i => skillsStr += i.Key + "=" + i.Value.ToString() + ";");
            if (skillsStr.Length > 1)
                skillsStr = skillsStr.Substring(0, skillsStr.Length - 1);
            return username + space + positionStr + space + rotationStr + space + inventoryStr + space + skillsStr + "\n";
        }
        public void FromSendable(string str)
        {
            string[] fields = str.Replace("\n", "").Split(new string[] { "\u001f" }, StringSplitOptions.None);
            this.username = fields[0];
            this.position = new float[] { float.Parse(fields[1].Split(';')[0]), float.Parse(fields[1].Split(';')[1]), float.Parse(fields[1].Split(';')[2]) };
            this.rotation = new float[] { float.Parse(fields[2].Split(';')[0]), float.Parse(fields[2].Split(';')[1]), float.Parse(fields[2].Split(';')[2]) };
            inventory = new Dictionary<string, int>();
            fields[3].Split(';').ToList().ForEach(f => inventory.Add(f.Split('=')[0], int.Parse(f.Split('=')[1])));
            skills = new Dictionary<string, int>();
            fields[4].Split(';').ToList().ForEach(f => skills.Add(f.Split('=')[0], int.Parse(f.Split('=')[1])));
        }
        public static Player NewPlayer(string str)
        {
            string[] fields = str.Replace("\n", "").Split(new string[] { "\u001f" }, StringSplitOptions.None);
            Player p = new Player(fields[0], "");
            p.position = new float[] { float.Parse(fields[1].Split(';')[0]), float.Parse(fields[1].Split(';')[1]), float.Parse(fields[1].Split(';')[2]) };

            p.rotation = new float[] { float.Parse(fields[2].Split(';')[0]), float.Parse(fields[2].Split(';')[1]), float.Parse(fields[2].Split(';')[2]) };
            p.inventory = new Dictionary<string, int>();
            fields[3].Split(';').ToList().ForEach(f => p.inventory.Add(f.Split('=')[0], int.Parse(f.Split('=')[1])));
            p.skills = new Dictionary<string, int>();
            fields[4].Split(';').ToList().ForEach(f => p.skills.Add(f.Split('=')[0], int.Parse(f.Split('=')[1])));
            return p;
        }
        public string UpdateSql()
        {
            string itemsJson = JsonSerializer.Serialize(inventory);
            string skillsJson = JsonSerializer.Serialize(skills);
            return "update player (position, rotation, inventory, skills) values('{" + position[0].ToString() + "," + position[1].ToString() + "," + position[2].ToString() +
                           "}', '{" + rotation[0].ToString() + "," + rotation[1].ToString() + "," + rotation[2].ToString() + "}', '" + itemsJson + "', '" + skillsJson + "') " +
                           "where username = '"+ username +"';"; 
        }
        public string GetSql()
        {
            string itemsJson = JsonSerializer.Serialize(inventory);
            string skillsJson = JsonSerializer.Serialize(skills);
            return "insert into player (username, password, position, rotation, inventory, skills) values('"
                           +username + "', '"+password+"', '{"+position[0].ToString()+","+ position[1].ToString() + ","+ position[2].ToString() +
                           "}', '{" + rotation[0].ToString() + ","+ rotation[1].ToString() + ","+ rotation[2].ToString() + "}', '" + itemsJson + "', '"+skillsJson+"');";
        }
        public byte[] GetBytes()
        {            
            return Encoding.ASCII.GetBytes(JsonSerializer.Serialize<Player>(this, new JsonSerializerOptions{ IgnoreReadOnlyProperties = true }));
        }
    }
}
