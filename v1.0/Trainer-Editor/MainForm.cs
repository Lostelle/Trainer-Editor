﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lost
{
    public partial class MainForm : Form
    {
        ROM rom;
        Settings romInfo;

        Bitmap invisible = new Bitmap(64, 64);
        PictureBox[] partyPictureBoxes;

        bool ignore = false;

        public MainForm()
        {
            InitializeComponent();

            partyPictureBoxes = new PictureBox[6] { p1, p2, p3, p4, p5, p6 };
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            invisible.Dispose();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "GBA ROMs|*.gba";
            openFileDialog1.Title = "Open ROM";

            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            if (!OpenROM(openFileDialog1.FileName))
                return;

            LoadAll();

            // populate form
            listTrainers.Items.Clear();
            for (int i = 0; i < trainerCount; i++)
            {
                var item = new ListViewItem($"{i:X3}");
                item.SubItems.Add(names[i]);
                listTrainers.Items.Add(item);
            }

            cClass.Items.Clear();
            cClass.Items.AddRange(classes);

            cItem1.Items.Clear();
            cItem1.Items.AddRange(items);
            cItem2.Items.Clear();
            cItem2.Items.AddRange(items);
            cItem3.Items.Clear();
            cItem3.Items.AddRange(items);
            cItem4.Items.Clear();
            cItem4.Items.AddRange(items);

            cHeld.Items.Clear();
            cHeld.Items.AddRange(items);

            cSpecies.Items.Clear();
            cSpecies.Items.AddRange(pokemon);

            cAttack1.Items.Clear();
            cAttack1.Items.AddRange(attacks);
            cAttack2.Items.Clear();
            cAttack2.Items.AddRange(attacks);
            cAttack3.Items.Clear();
            cAttack3.Items.AddRange(attacks);
            cAttack4.Items.Clear();
            cAttack4.Items.AddRange(attacks);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void listTrainers_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = -1;
            foreach (int x in listTrainers.SelectedIndices)
                index = x;

            if (index == -1)
                return;
            ignore = true;

            // ------------------------------
            LoadTrainer(index);
            for (int i = 0; i < 6; i++)
            {
                var sprite = invisible;
                if (i < trainer.Party.Count)
                {
                    sprite = LoadFrontSprite(trainer.Party[i].Species);
                }
                partyPictureBoxes[i].Image = sprite;
            }
            pSprite.Image = LoadTrainerSprite(trainer.Sprite);

            // ------------------------------
            txtName.Text = trainer.Name;
            rMale.Checked = trainer.Gender == 0;
            rFemale.Checked = trainer.Gender == 1;

            nSprite.Value = trainer.Sprite;

            cClass.SelectedIndex = trainer.Class;
            txtClassID.Value = trainer.Class;
            txtClass.Text = classes[trainer.Class];

            cItem1.SelectedIndex = trainer.Items[0];
            cItem2.SelectedIndex = trainer.Items[1];
            cItem3.SelectedIndex = trainer.Items[2];
            cItem4.SelectedIndex = trainer.Items[3];

            txtMusic.Value = trainer.Music;
            txtAI.Value = (int)(trainer.AI & 0x1FF); // AI is the first 9 bits

            chkDoubleBattle.Checked = trainer.DoubleBattle;
            chkHeldItems.Checked = trainer.HasHeldItems;
            chkMovesets.Checked = trainer.HasCustomAttacks;

            listParty.Items.Clear();
            var p = 0;
            foreach (var pk in trainer.Party)
            {
                var i = new ListViewItem((++p).ToString());
                i.SubItems.Add(pokemon[pk.Species]);
                i.SubItems.Add($"{pk.Level}");

                listParty.Items.Add(i);
            }

            txtSpecies.Value = 0;
            txtLevel.Value = 0;
            txtEVs.Value = 0;
            cSpecies.SelectedIndex = 0;
            cHeld.SelectedIndex = 0;
            cAttack1.SelectedIndex = 0;
            cAttack2.SelectedIndex = 0;
            cAttack3.SelectedIndex = 0;
            cAttack4.SelectedIndex = 0;

            if (trainer.HasHeldItems)
            {
                cHeld.Enabled = true;
            }
            else
            {
                cHeld.Enabled = false;
            }

            if (trainer.HasCustomAttacks)
            {
                cAttack1.Enabled = true;
                cAttack2.Enabled = true;
                cAttack3.Enabled = true;
                cAttack4.Enabled = true;
            }
            else
            {
                cAttack1.Enabled = false;
                cAttack2.Enabled = false;
                cAttack3.Enabled = false;
                cAttack4.Enabled = false;
            }

            ignore = false;
        }

        private void listParty_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = -1;
            foreach (int x in listParty.SelectedIndices)
                index = x;

            if (index == -1)
                return;
            ignore = true;

            // ------------------------------
            var pk = trainer.Party[index];

            txtSpecies.Value = pk.Species;
            cSpecies.SelectedIndex = pk.Species;
            txtLevel.Value = pk.Level;
            txtEVs.Value = pk.EVs;

            if (trainer.HasHeldItems)
                cHeld.SelectedIndex = pk.HeldItem;
            

            if (trainer.HasCustomAttacks)
            {
                cAttack1.SelectedIndex = pk.Attacks[0];
                cAttack2.SelectedIndex = pk.Attacks[1];
                cAttack3.SelectedIndex = pk.Attacks[2];
                cAttack4.SelectedIndex = pk.Attacks[3];
            }

            ignore = false;
        }

        bool OpenROM(string filename)
        {
            bool success = true;
            ROM temp = null;

            try
            {
                // create a new ROM
                temp = new ROM(filename);

                // check that it is valid
                if (!File.Exists($@"ROMs\{temp.Code}.ini"))
                    throw new Exception($"ROM type {temp.Code} is not supported!");

                // TODO: custom settings
                romInfo = Settings.FromFile($@"ROMs\{temp.Code}.ini", "ini");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                success = false;
            }

            // finish
            if (success)
            {
                rom?.Dispose();
                rom = temp;
            }
            else
            {
                temp?.Dispose();
            }
            return success;
        }

        void LoadAll()
        {
            if (rom == null) return;

            // get limits from .ini
            pokemonCount = romInfo.GetInt32("pokemon", "Count");
            itemCount = romInfo.GetInt32("items", "Count");
            attackCount = romInfo.GetInt32("attacks", "Count");

            trainerCount = romInfo.GetInt32("trainers", "Count");
            trainerSpriteCount = romInfo.GetInt32("trainer_sprites", "Count");
            classCount = romInfo.GetInt32("trainer_classes", "Count");

            // load all data needed
            LoadNames();
            LoadClasses();

            LoadPokemonNames();
            LoadAttacks();
            LoadItems();

            txtSpecies.MaximumValue = pokemonCount;
            txtClassID.MaximumValue = classCount;
            nSprite.Maximum = trainerSpriteCount;
        }
    }
}
