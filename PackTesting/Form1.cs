using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChunkAndChainFileManager;
using ChunkChainDataTypes;
using ReceiverPackLib;
using System.IO;
using System.Threading;

namespace PackTesting
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        delegate void UpdateControls(bool first_iteration,ulong ticks,ulong iterations,bool update_all_statistics);

        void Update_Controls(bool first_iteration, ulong ticks, ulong iterations, bool update_all_statistics)
        {
            if (first_iteration)
            {
                labelFirstIteration.Text = Convert.ToString(ticks);
            }
            else
            {
                labelAverage.Text = Convert.ToString(ticks);
            }
            labelIterations.Text = Convert.ToString(iterations);
            if (update_all_statistics)
            {
                listView1.Clear();
                listView1.Items.Add(new ListViewItem("ChunkAndChainFileManager "));
                listView1.Items.Add(new ListViewItem("CannotFindChunkInChain " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.CannotFindChunkInChain)));
                listView1.Items.Add(new ListViewItem("ChainAdded2ExistingChunk " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChainAdded2ExistingChunk)));
                listView1.Items.Add(new ListViewItem("CannotGetChain " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.CannotGetChain)));
                listView1.Items.Add(new ListViewItem("LongerChainsFound " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.LongerChainsFound)));
                listView1.Items.Add(new ListViewItem("ChainExistInChunkChainsList " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChainExistInChunkChainsList)));
                listView1.Items.Add(new ListViewItem("ChainsCreated " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChainsCreated)));
                listView1.Items.Add(new ListViewItem("DifferentChains " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.DifferentChains)));
                listView1.Items.Add(new ListViewItem("ChainsLoaded " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChainsLoaded)));
                listView1.Items.Add(new ListViewItem("ChainsReturned " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChainsReturned)));
                listView1.Items.Add(new ListViewItem("ChunkExists " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChunkExists)));
                listView1.Items.Add(new ListViewItem("ChunksCreated " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChunksCreated)));
                listView1.Items.Add(new ListViewItem("ChunksLoaded " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.ChunksLoaded)));
                listView1.Items.Add(new ListViewItem("FoundEqualChains " + Convert.ToString(ChunkAndChainFileManager.ChunkAndChainFileManager.FoundEqualChains)));

                listView1.Items.Add(new ListViewItem("ReceiverPackLib "));
                listView1.Items.Add(new ListViewItem("PredMsgSent " + Convert.ToString(ReceiverPackLib.ReceiverPackLib.m_PredMsgSent)));
                listView1.Items.Add(new ListViewItem("ChunksProcessed " + Convert.ToString(ReceiverPackLib.ReceiverPackLib.m_ChunksProcessed)));
                listView1.Items.Add(new ListViewItem("PredAckMsgReceived " + Convert.ToString(ReceiverPackLib.ReceiverPackLib.m_PredAckMsgReceived)));
            }
        }

        void OnDataReceived(byte[] data,int offset,int length)
        {
            FileStream fs = File.Open("received_file", FileMode.Append, FileAccess.Write);
            fs.Write(data, offset, data.Length);
            fs.Flush();
            fs.Close();
        }

        void OnTransactionEnd(object param)
        {
        }

        void DoTest(object arg)
        {
            string fileName = (string)arg;
            UpdateControls updateControls = new UpdateControls(Update_Controls);
            if (File.Exists("received_file"))
            {
                File.Delete("received_file");
            }
            FileStream fs = File.OpenRead(fileName);
            if (fs == null)
            {
                return;
            }
            byte[] raw_file = new byte[fs.Length];
            if (fs.Read(raw_file, 0, raw_file.Length) != raw_file.Length)
            {
                return;
            }
            fs.Close();
            fs = null;
            DateTime initialStamp,finishStamp;
            bool firstIteration = true;
            ulong ticks_sum = 0;
            ulong iterations = 0;
            byte[] buf;

            OnData onData = new OnData(OnDataReceived);
            OnEnd onEnd = new OnEnd(OnTransactionEnd);
            
            do
            {
                initialStamp = DateTime.Now;
                buf = raw_file;

                if (File.Exists("received_file"))
                {
                    File.Delete("received_file");
                }

                ReceiverPackLib.ReceiverPackLib receiverPackLib = new ReceiverPackLib.ReceiverPackLib(onData,onEnd,null);
                SenderPackLib.SenderPackLib senderPackLib = new SenderPackLib.SenderPackLib(buf);
                senderPackLib.AddData(buf,false);
#if false
                senderPackLib.AddLast();
                bool SameChunk;
                while ((buf = senderPackLib.GetChunk(out SameChunk)) != null)
                {
                    byte[] predMsg = receiverPackLib.ReceiverOnData(buf,0);
                    if (predMsg != null)
                    {
                        byte[] predAckMsg = senderPackLib.SenderOnData(predMsg, 0);
                        receiverPackLib.ReceiverOnData(predAckMsg, 0);
                    }
                }
#endif
                finishStamp = DateTime.Now;
                object[] args = new object[4];
                args[0] = firstIteration;
                if (firstIteration)
                {
                    args[1] = (ulong)(finishStamp.Ticks - initialStamp.Ticks);
                    firstIteration = false;
                }
                else
                {
                    ticks_sum += (ulong)(finishStamp.Ticks - initialStamp.Ticks);
                    iterations++;
                    args[1] = ticks_sum / iterations;
                }
                args[2] = (ulong)iterations;
                args[3] = true;
                Invoke(updateControls, args);
                while (checkBoxPause.Checked)
                {
                    Thread.Sleep(1000);
                }
            } while (checkBoxDoUntilStopped.Checked);
        }

        private void buttonOpenFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            ParameterizedThreadStart pts = new ParameterizedThreadStart(DoTest);

            Thread thread = new Thread(pts);
            thread.Start(openFileDialog1.FileName);
            //DoTest();
        }
    }
}
