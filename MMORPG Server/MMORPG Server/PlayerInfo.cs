using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MMORPG_Server
{
    public partial class PlayerInfo : Form
    {
        private Player player;

        public PlayerInfo()
        {
            player = new Player();
            InitializeComponent();
        }

        public PlayerInfo(Player p)
        {
            player = p;
            InitializeComponent();
        }

        private void PlayerInfo_Load(object sender, EventArgs e)
        {
            username.Text = player.username;
            posx.Text = player.position[0].ToString();
            posy.Text = player.position[1].ToString();
            posz.Text = player.position[2].ToString();
            rotx.Text = player.rotation[0].ToString();
            roty.Text = player.rotation[1].ToString();
            rotz.Text = player.rotation[2].ToString();
            inventory.BeginUpdate();
            inventory.View = View.Details;
            inventory.Columns.Add("Item", 200, HorizontalAlignment.Left);
            inventory.Columns.Add("Count", 90, HorizontalAlignment.Right);
            player.inventory.ToList().ForEach(p => {
                ListViewItem item = new ListViewItem(p.Key);
                item.SubItems.Add(p.Value.ToString());
                inventory.Items.Add(item);
            });
            inventory.EndUpdate();
            skills.BeginUpdate();
            skills.View = View.Details;
            skills.Columns.Add("Skill", 200, HorizontalAlignment.Left);
            skills.Columns.Add("Value", 90, HorizontalAlignment.Right);
            player.skills.ToList().ForEach(p => {
                ListViewItem item = new ListViewItem(p.Key);
                item.SubItems.Add(p.Value.ToString());
                skills.Items.Add(item);
            });
            skills.EndUpdate();
        }        
    }
}
