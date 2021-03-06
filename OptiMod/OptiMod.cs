﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ProTrackerTools;

namespace OptiMod
{
    public partial class OptiMod : Form
    {
        private Module _mod;
        private string _fileName;

        public OptiMod()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            LoadMod();
        }

        private void LoadMod()
        {
            try
            {
                var ofd = new OpenFileDialog();
                ofd.Filter = "Protacker Module|*.mod;mod.*|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _mod = Serializer.DeSerializeMod(ofd.FileName);
                    lblName.Text = _mod.Name;
                    RefreshDisplay();
                    _fileName = Path.GetFileName(ofd.FileName);
                    lblPatternsA.Text = string.Format("Patterns used: {0}", _mod.Patterns.Count);
                    lblSizeA.Text = string.Format("Size: {0}", new FileInfo(ofd.FileName).Length);
 
                    btnFullOptimse.Enabled = true;
                    btnRemoveDupePatterns.Enabled = true;
                    btnOptimiseSampleLengths.Enabled = true;
                    btnRemoveUnsedSamples.Enabled = true;
                    btnRemoveUnusedPatterns.Enabled = true;
                    btnSave.Enabled = true;
                    btnZeroLeadingSamples.Enabled = true;
                    btnImportASCII.Enabled = true;
                    btnTruncateToLoop.Enabled = true;
                    btnExpandLoops.Enabled = true;
                    btnPadSamples.Enabled = true;
                    btnClearSampleNames.Enabled = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error loading module: {0}", e.Message));
            }
        }

        private void RefreshDisplay()
        {
            lvwSamples.Items.Clear();
            foreach (var sample in _mod.Samples)
            {
                lvwSamples.Items.Add(GetSampleDetail(sample));
            }

            lblPatternsB.Text = string.Format("Patterns used: {0}", _mod.Patterns.Count);
            var data = Serializer.SerializeMod(_mod);
            lblSizeB.Text = string.Format("Size: {0}", data.Length);
        }

        private ListViewItem GetSampleDetail(Sample sample)
        {
            var detail = new List<string>();
            detail.Add(sample.Name);
            detail.Add(sample.Volume.ToString());
            detail.Add(sample.FineTune.ToString());
            detail.Add(sample.Length.ToString());
            detail.Add(sample.RepeatStart.ToString());
            detail.Add(sample.RepeatLength.ToString());
            return (new ListViewItem(detail.ToArray()));
        }

        private void btnOptiLength_Click(object sender, EventArgs e)
        {
            _mod.OptimseLoopedSamples();
            RefreshDisplay();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveMod();
        }

        private void SaveMod()
        {
            try
            {
                var sfd = new SaveFileDialog();
                sfd.FileName = _fileName;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, Serializer.SerializeMod(_mod));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Whoops: {0}", e.Message));
            }
        }

        private void SaveSong()
        {
            try
            {
                var sfd = new SaveFileDialog();
                sfd.FileName = _fileName;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, Serializer.SerializePatternData(_mod));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Whoops: {0}", e.Message));
            }
        }

        private void SaveSamples()
        {
            try
            {
                var sfd = new SaveFileDialog();
                sfd.FileName = _fileName;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, Serializer.SerializeSampleData(_mod));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Whoops: {0}", e.Message));
            }
        }

        private void btnRemoveUnsedSamples_Click(object sender, EventArgs e)
        {
            _mod.RemoveUnusedSamples();
            RefreshDisplay();
        }

        private void btnRemoveDupePatterns_Click(object sender, EventArgs e)
        {
            _mod.RemoveDuplicatePatterns();
            RefreshDisplay();
        }

        private void btnFullOptimse_Click(object sender, EventArgs e)
        {
            _mod.RemoveUnusedPatterns();
            _mod.RemoveDuplicatePatterns();
            _mod.RemoveUnusedSamples();
            _mod.OptimseLoopedSamples();
            _mod.ZeroLeadingSamples();
            RefreshDisplay();
        }

        private void btnZeroLeadingSamples_Click(object sender, EventArgs e)
        {
            _mod.ZeroLeadingSamples();
            Refresh();
        }

        private void btnRemoveUnusedPatterns_Click(object sender, EventArgs e)
        {
            _mod.RemoveUnusedPatterns();
            RefreshDisplay();
        }

        private void btnImportASCII_Click(object sender, EventArgs e)
        {
            ImportASCII();
            RefreshDisplay();
        }



        private void ImportASCII()
        {
            try
            {
                var ofd = new OpenFileDialog();
                ofd.Filter = "All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (new FileInfo(ofd.FileName).Length > 4000)
                    {
                        MessageBox.Show("You are having a laugh right?");
                    }
                    else
                    {
                        var ascii = File.ReadAllLines(ofd.FileName);
                        for (var i = 0; i < 31; i++)
                        {
                            if (ascii.Length > i)
                            {
                                if (ascii[i].Length > 22)
                                    _mod.Samples[i].Name = ascii[i].Substring(0, 22);
                                else
                                    _mod.Samples[i].Name = ascii[i];
                            }
                            else
                            {
                                _mod.Samples[i].Name = "";
                            }
                        }
                    }
                }
                RefreshDisplay();
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Whoops: ", e.Message));
            }
        }

        private void btnTruncateToLoop_Click(object sender, EventArgs e)
        {
            if (lvwSamples.SelectedIndices.Count == 0)
            {
                MessageBox.Show("Select at least one sample first");
                return;
            }

            var samples = "Are you sure you want to truncate the following samples to loop data only?\n\n";
            foreach (int i in lvwSamples.SelectedIndices)
            {
                samples += lvwSamples.Items[i].Text.Replace("\0","") + "\n";
            }

            if (MessageBox.Show(samples, "Are you sure?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                foreach (int i in lvwSamples.SelectedIndices)
                {
                    if (!_mod.TruncateToLoop(i))
                    {
                        MessageBox.Show(string.Format("Unable to truncate {0}", _mod.Samples[i].Name));
                    }
                }
            }

            RefreshDisplay();
        }

        private void btnExpandLoops_Click(object sender, EventArgs e)
        {
            _mod.ExpandPatternLoops();
            RefreshDisplay();
        }

        private void btnPadSamples_Click(object sender, EventArgs e)
        {
            var sampleSize = 0;
            try
            {
                sampleSize = Convert.ToInt32(txtPadSize.Text);
            }
            catch
            {
                MessageBox.Show(string.Format("{0} isn't a number", txtPadSize.Text));
            }

            if (sampleSize > 4)
            {
                _mod.PadSamples(sampleSize);
                RefreshDisplay();
            }
        }

        private void btnClearSampleNames_Click(object sender, EventArgs e)
        {
            _mod.ClearSampleNames();
            RefreshDisplay();
        }

        private void btnSaveSong_Click(object sender, EventArgs e)
        {
            SaveSong();
        }

        private void btnSaveSamples_Click(object sender, EventArgs e)
        {
            SaveSamples();
        }
    }
}
