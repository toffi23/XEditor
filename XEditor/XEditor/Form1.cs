﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using XEditor.Parser;
using XEditor.XML_Model;
using System.IO;

namespace XEditor
{
    public partial class Form1 : Form
    {
        private Schedule schedule;
        private xLogger logger;
        private string path;

        private Panel editPanel; // store the currently showed panel

        public Form1()
        {
            InitializeComponent();
            logger = new xLogger();
            logger.Show();

            schedule = new Schedule("Veszprem");

            path = Path.Combine(Directory.GetCurrentDirectory(),
                  "VeSchedule_begin.xml"
                  );

            ScheduleParser parser = new ScheduleParser(schedule,path);
            parser.read();

            fillScheduleTree();

            scheduleTree.ExpandAll();

            this.listBox_stopstats.DataSource = schedule.Stations.StationList;

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you want to save the changes?", "Save&Exit", MessageBoxButtons.YesNoCancel);

            switch(result)
            {
                case DialogResult.Yes:
                    schedule.save(path);
                    logger.Hide();
                    break;
                case DialogResult.No:
                    logger.Hide();
                    break;
                default:
                    e.Cancel = true;
                    break;
            }

        }

        private void fillScheduleTree()
        {
            scheduleTree.Nodes.Clear();

            // stations
            TreeNode tStations = scheduleTree.Nodes.Add("Stations");
            tStations.Tag = schedule.Stations;
            foreach(Station station in schedule.Stations.StationList)
            {
                TreeNode tStation = tStations.Nodes.Add(station.Name);
                tStation.Tag = station;
            }

            // lines
            TreeNode tLines = scheduleTree.Nodes.Add("Lines");
            tLines.Tag = schedule.Lines;

            foreach(Line line in schedule.Lines.LineList)
            { 
                TreeNode tLine = tLines.Nodes.Add(line.Name);
                tLine.Tag = line;
                TreeNode tTracks = tLine.Nodes.Add("Tracks");
                tTracks.Tag = line.Tracks;
                TreeNode tStarts = tLine.Nodes.Add("Starts");
                tStarts.Tag = line.Starts;

                // add tracks and stops for them
                foreach (Track track in line.Tracks.TrackList)
                {
                    TreeNode tTrack = tTracks.Nodes.Add(track.ID.ToString());
                    tTrack.Tag = track;

                    foreach(Stop stop in track.Stops)
                    {
                        TreeNode node = new TreeNode(stop.Station.Name);
                        node.Tag = stop;
                        tTrack.Nodes.Add(node);
                    }
                }

                // add starts
                foreach(Start start in line.Starts.StartList)
                {
                    TreeNode node = new TreeNode(start.Time);
                    node.Tag = start;
                    tStarts.Nodes.Add(node);
                }
            }
        }

        private void scheduleTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            /*
                TODO: reduce depth of if-else tree? 
                pro: same branches as treeview  
                con: potential unnecessary if checks
            */

            if (editPanel != null) editPanel.Hide(); // hide previously used panel

            Stations stations;
            if ((stations = e.Node.Tag as Stations) != null)
            { 
                xLogger.add("Selected: " + "Stations");
            }
            else
            {
                Station station;
                if ((station = e.Node.Tag as Station) != null)
                {
                    xLogger.add("Selected: " + station.Name);
                    editStation();
                }
            }

            Lines lines;
            if((lines = e.Node.Tag as Lines) != null)
            {
                xLogger.add("Selected: " + "Lines");
            }
            else
            {
                Line line; 
                if((line = e.Node.Tag as Line) != null)
                {
                    xLogger.add("Selected:" + line.Name);
                    editLine();
                }
                else
                {
                    Tracks tracks;
                    if((tracks = e.Node.Tag as Tracks) != null)
                    {
                        xLogger.add("Selected: " + "Tracks");
                    }
                    else
                    {
                        Track track;
                        if ((track = e.Node.Tag as Track) != null)
                        {
                            xLogger.add("Selected: " + track.ID);
                            editTrack();
                        }
                        else
                        {
                            Stop stop;
                            if ((stop = e.Node.Tag as Stop) != null)
                            {
                                xLogger.add("Selected: " + stop.Station.Name);
                                editStop();
                            }
                            else // bring out this part to same level as Tracks
                            {
                                Starts starts;
                                if ((starts = e.Node.Tag as Starts) != null)
                                {
                                    xLogger.add("Selected: " + "Starts");
                                }
                                else
                                {
                                    Start start;
                                    if ((start = e.Node.Tag as Start) != null)
                                    {
                                        xLogger.add("Selected: " + start.Time);
                                        editstart();
                                    }
                                }
                            }
                        }
                    }
                    
                }
            }
        }

        private void editStop()
        {
            if (editPanel != null) editPanel.Hide();

            Stop stop;
            if ((stop = scheduleTree.SelectedNode.Tag as Stop) != null)
            {
                editPanel = editpanel_stop;
                editPanel.Show();
                listBox_stopstats.SelectedItem = stop.Station;
                numericUpDown_stopdelay.Value = stop.Delay;
            }
        }

        private void editstop_savebutton_Click(object sender, EventArgs e)
        {
            Stop stop;
            if ((stop = scheduleTree.SelectedNode.Tag as Stop) != null)
            {
                stop.Station = (Station)listBox_stopstats.SelectedItem;

                stop.Delay = (int)numericUpDown_stopdelay.Value;

                scheduleTree.SelectedNode.Text = stop.Station.Name;

                editPanel.Hide();
                editPanel = null;

                scheduleTree.SelectedNode = null;
            }
        }

        private void editTrack()
        {
            if (editPanel != null) editPanel.Hide();

            Track track;
            if((track = scheduleTree.SelectedNode.Tag as Track) != null)
            {
                editPanel = editpanel_track;
                editPanel.Show();
                numericUpDown_TID.Value = track.ID;
            }
        }

        private void button_saveTrack_Click(object sender, EventArgs e)
        {
            Track track;
            if ((track = scheduleTree.SelectedNode.Tag as Track) != null)
            {
                Tracks parent = scheduleTree.SelectedNode.Parent.Tag as Tracks;

                foreach (Track t in parent.TrackList)
                {
                    if (t.ID == numericUpDown_TID.Value)
                    {
                        MessageBox.Show("Track ID is already taken.");
                        return;
                    }
                }

                track.ID = (int)numericUpDown_TID.Value;
                scheduleTree.SelectedNode.Text = track.ID.ToString();


                editPanel.Hide();
                editPanel = null;

                scheduleTree.SelectedNode = null;
            }
        }

        private void editstart()
        {
            if (editPanel != null) editPanel.Hide(); // hide previously used panel

            Start start;
            if ((start = scheduleTree.SelectedNode.Tag as Start) != null)
            {
                Tracks parent = scheduleTree.SelectedNode.Parent.Parent.Nodes[0].Tag as Tracks;

                editPanel = editpanel_start;
                editPanel.Show();
                editstartActive.Text = start.Active;
                editstartTimepick.Value = DateTime.Parse(start.Time);
                listBox_startTID.DataSource = parent.TrackList;

                for(int i = 0; i <  parent.TrackList.Count; i++)
                {
                    if(parent.TrackList[i].ID == start.Track.ID)
                    {
                        listBox_startTID.SelectedIndex = i;
                    }
                }
                
            }
        }

        private void button_saveStart_Click(object sender, EventArgs e)
        {
            Start start;
            if ((start = scheduleTree.SelectedNode.Tag as Start) != null)
            {
                Tracks parent = scheduleTree.SelectedNode.Parent.Parent.Nodes[0].Tag as Tracks;

                start.Track = (Track)listBox_startTID.SelectedItem;
                start.Active = editstartActive.Text;
                start.Time = editstartTimepick.Value.ToString("HH:mm");

                scheduleTree.SelectedNode.Text = start.Time;

                editPanel.Hide();
                editPanel = null;

                scheduleTree.SelectedNode = null;
            }
        }

        private void editLine()
        {
            if (editPanel != null) editPanel.Hide(); // hide previously used panel

            Line line;
            if((line = scheduleTree.SelectedNode.Tag as Line) != null)
            {
                editPanel = editpanel_line;
                editPanel.Show();
                textedit_line.Text = line.Name;
            }
            
        }

        private void button_saveline_Click(object sender, EventArgs e)
        {
            Line line;
            if ((line = scheduleTree.SelectedNode.Tag as Line) != null)
            {
                Lines parent = scheduleTree.SelectedNode.Parent.Tag as Lines;
                foreach(Line l in parent.LineList)
                {
                    if(l.Name == textedit_line.Text && l != line)
                    {
                        MessageBox.Show("Line name is already taken.");
                        return;
                    }
                }

                line.Name = textedit_line.Text;
                scheduleTree.SelectedNode.Text = line.Name;

                editPanel.Hide();
                editPanel = null;

                scheduleTree.SelectedNode = null;
            }
        }

        private void editStation()
        {
            if (editPanel != null) editPanel.Hide();

            Station station;
            if((station = scheduleTree.SelectedNode.Tag as Station)!=null)
            {
                editPanel = editpanel_Station;
                editPanel.Show();

                textBox_station.Text = station.Name;
            }
        }

        private void button_saveStation_Click(object sender, EventArgs e)
        {
            Station station;
            if ((station = scheduleTree.SelectedNode.Tag as Station) != null)
            {
                Stations parent = scheduleTree.SelectedNode.Parent.Tag as Stations;
                foreach(Station stat in parent.StationList)
                {
                    if(stat.Name == textBox_station.Text && stat != station)
                    {
                        MessageBox.Show("Station is already exists.");
                        return;
                    }
                }

                station.Name = textBox_station.Text;
                scheduleTree.SelectedNode.Text = station.Name;

                editPanel.Hide();
                editPanel = null;

                scheduleTree.SelectedNode = null;
            }
        }

        private void removeZombieStations(Station toRemove)
        {
            List<Stop> removeList = new List<Stop>();

            Lines lines = schedule.Lines;
            foreach(Line line in lines.LineList)
            { 
                foreach(Track track in line.Tracks.TrackList)
                { 
                    foreach(Stop stop in track.Stops)
                    {
                        if (stop.Station == toRemove)
                        {
                            removeList.Add(stop); 
                        }                       
                    }

                    foreach(Stop stop in removeList)
                    {
                        xLogger.add("Stop is removed: " + stop.Station.Name + " from " + line.Name + "/" + track.ID);
                        track.Stops.Remove(stop);
                    }

                    removeList.Clear();
                   
                }
            }
        }

        private void butRemove_Click(object sender, EventArgs e)
        {
            Station station;
            if ((station = scheduleTree.SelectedNode.Tag as Station) != null)
            {
                removeZombieStations(station);
                schedule.Stations.removeStation(station);


                fillScheduleTree();
                scheduleTree.ExpandAll();

                editPanel.Hide();
                editPanel = null;
                scheduleTree.SelectedNode = null;
            }
            else
            {
                Line line;
                if ((line = scheduleTree.SelectedNode.Tag as Line) != null)
                {
                    Lines parent = scheduleTree.SelectedNode.Parent.Tag as Lines;

                    parent.removeLine(line);
                    scheduleTree.SelectedNode.Remove();

                    editPanel.Hide();
                    editPanel = null;
                    scheduleTree.SelectedNode = null;
                }
                else
                {
                    Track track;
                    if ((track = scheduleTree.SelectedNode.Tag as Track) != null)
                    {
                        Tracks parent = scheduleTree.SelectedNode.Parent.Tag as Tracks;

                        removeZombieStarts(track);
                        parent.removeTrack(track);

                        fillScheduleTree();
                        scheduleTree.ExpandAll();

                        editPanel.Hide();
                        editPanel = null;
                        scheduleTree.SelectedNode = null;
                    }
                    else
                    {
                        Stop stop;
                        if((stop = scheduleTree.SelectedNode.Tag as Stop) != null)
                        {
                            Track parent = scheduleTree.SelectedNode.Parent.Tag as Track;

                            parent.Stops.Remove(stop);
                            scheduleTree.SelectedNode.Remove();

                            editPanel.Hide();
                            editPanel = null;
                            scheduleTree.SelectedNode = null;
                        }
                    }
                }
            }
            

        }

        private void removeZombieStarts(Track toRemove)
        {
            List<Start> removelist = new List<Start>();

            Lines lines = schedule.Lines;
            foreach(Line line in lines.LineList)
            {
                Starts starts = line.Starts;
                foreach(Start start in starts.StartList)
                {
                    if(start.Track == toRemove)
                    {
                        removelist.Add(start);
                    }
                }

                foreach(Start start in removelist)
                {
                    starts.removeStart(start);
                }
                removelist.Clear();
            }
        }

        private void butNew_Click(object sender, EventArgs e)
        {
            if (scheduleTree.SelectedNode == null)
            {
                MessageBox.Show("Select a valid note!");
                return;
            }

            Stations stations;
            if ((stations = scheduleTree.SelectedNode.Tag as Stations) != null)
            {
                int id = schedule.Stations.getNewID();
                Station newStation = new Station(id,"New station " + id);
                TreeNode stNode = new TreeNode(newStation.Name);
                stNode.Tag = newStation;

                schedule.Stations.addStation(newStation);
                scheduleTree.SelectedNode.Nodes.Add(stNode);

                scheduleTree.SelectedNode = stNode;
                scheduleTree.Focus();

                editStation();
            }
            else
            {
                Lines lines;
                if((lines = scheduleTree.SelectedNode.Tag as Lines)!=null)
                {
                    string name = "New Line";
                    Line newline = new Line(name);
                    schedule.Lines.addLine(newline);

                    TreeNode lNode = new TreeNode(newline.Name);
                    lNode.Tag = newline;
                    scheduleTree.SelectedNode.Nodes.Add(lNode);

                    scheduleTree.SelectedNode = lNode;
                    scheduleTree.Focus();

                    TreeNode childNode = new TreeNode("Tracks");
                    childNode.Tag = newline.Tracks;
                    lNode.Nodes.Add(childNode);

                    childNode = new TreeNode("Starts");
                    childNode.Tag = newline.Starts;
                    lNode.Nodes.Add(childNode);
                    lNode.ExpandAll();

                    editLine();
                }
                else
                {
                    Tracks tracks;
                    if((tracks = scheduleTree.SelectedNode.Tag as Tracks)!=null)
                    { // TODO: known issue: adding new track without savind the editpanel can results tracks with the same id saved upon closing program
                        Track newTrack = new Track(0);
                        tracks.addTrack(newTrack);

                        TreeNode tNode = new TreeNode(newTrack.ID.ToString());
                        tNode.Tag = newTrack;
                        scheduleTree.SelectedNode.Nodes.Add(tNode);

                        scheduleTree.SelectedNode = tNode;
                        scheduleTree.Focus();

                        editTrack();
                    }
                }
            }
        }
    }
}
