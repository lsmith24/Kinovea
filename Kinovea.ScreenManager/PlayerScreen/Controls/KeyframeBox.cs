/*
Copyright � Joan Charmant 2008.
jcharmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.

*/

using Kinovea.Services;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    /// Represent a key image as a thumbnail.
    /// Holds the key image id in the .Tag prop.
    /// </summary>
    public partial class KeyframeBox : UserControl
    {
        #region Events
        /// <summary>
        /// Asks the main timeline to move to the time of this keyframe.
        /// </summary>
        public event EventHandler<TimeEventArgs> Selected;
        /// <summary>
        /// Move this keyframe the current time of the playhead.
        /// </summary>
        public event EventHandler MoveToCurrentTimeAsked;
        /// <summary>
        /// Delete this keyframe.
        /// </summary>
        public event EventHandler DeleteAsked;
        #endregion

        #region Properties
        public Keyframe Keyframe
        {
            get { return keyframe; }
        }

        public bool Editing
        {
            get { return editing; }
        }
        #endregion

        #region Members
        private bool editing;
        private Keyframe keyframe;
        private bool isSelected;
        private ContextMenuStrip popMenu = new ContextMenuStrip();
        private ToolStripMenuItem mnuMove = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDelete = new ToolStripMenuItem();
        #endregion

        public KeyframeBox(Keyframe keyframe)
        {
            this.keyframe = keyframe;
            
            InitializeComponent();
            lblName.Text = keyframe.Name;
            
            BackColor = keyframe.Color;
            btnClose.Parent = pbThumbnail;

            pbThumbnail.BackColor = Color.Black;
            pbThumbnail.SizeMode = PictureBoxSizeMode.CenterImage;

            BuildContextMenu();
            ReloadMenusCulture();
        }
        
        public void DisplayAsSelected(bool selected)
        {
            isSelected = selected;
            pbThumbnail.BackColor = selected ? keyframe.Color : Color.Black;
        }
        public void UpdateEnableStatus()
        {
            this.Enabled = !keyframe.Disabled;
            UpdateCore();
            UpdateImage();
        }

        public void UpdateImage()
        {
            this.pbThumbnail.Image = keyframe.Disabled ? keyframe.DisabledThumbnail : keyframe.Thumbnail;
            this.Invalidate();
        }

        /// <summary>
        /// Update the name or color after a core change from elsewhere.
        /// </summary>
        public void UpdateContent()
        {
            UpdateCore();
        }

        public void RefreshUICulture()
        {
            ReloadMenusCulture();
        }

        #region Event Handlers
        private void Controls_MouseEnter(object sender, EventArgs e)
        {
            ShowButtons();
        }
        private void Controls_MouseLeave(object sender, EventArgs e)
        {
            // We hide the close button only if we left the whole control.
            Point clientMouse = PointToClient(Control.MousePosition);
            
            if(!pbThumbnail.ClientRectangle.Contains(clientMouse))
            {
                HideButtons();
                editing = false;
            }
        }
        private void Controls_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            // Note: for double click drag and drop conflicts with the double click event.
            // We can use explicit click counting if needed. (e.Clicks == 2)
            Selected?.Invoke(this, new TimeEventArgs(keyframe.Timestamp));
        }
        private void Controls_MouseUp(object sender, MouseEventArgs e)
        {
        }
        private void Controls_MouseMove(object sender, MouseEventArgs e)
        {
            // Support for drag and drop between the keyframe list and the timeline,
            // to move a keyframe to a specific time.
            if (e.Button != MouseButtons.Left)
                return;

            Selected?.Invoke(this, new TimeEventArgs(keyframe.Timestamp));
            this.DoDragDrop(this, DragDropEffects.Move);
        }
        private void Controls_DragDrop(object sender, DragEventArgs e)
        {
            // Called when we "drop" an object on the thumbnail.
            Selected?.Invoke(this, new TimeEventArgs(keyframe.Timestamp));
        }
        private void Controls_DragOver(object sender, DragEventArgs e)
        {
            // Called when we move over the thumbnail during a drag and drop op.
            e.Effect = DragDropEffects.Move;
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            DeleteAsked?.Invoke(this, e);
        }
        private void lblTimecode_Click(object sender, EventArgs e)
        {
            editing = true;
        }

        private void tbTitle_Click(object sender, EventArgs e)
        {
            editing = true;
        }

        #endregion

        #region Private helpers
        
        /// <summary>
        /// Update the UI representation of the core data (name and color).
        /// </summary>
        private void UpdateCore()
        {
            lblName.Text = keyframe.Name;
            BackColor = keyframe.Color;
            DisplayAsSelected(isSelected);
            UpdateToolTip();
        }
        private void BuildContextMenu()
        {
            mnuMove.Image = Properties.Drawings.move_keyframe;
            mnuMove.Click += (s, e) => MoveToCurrentTimeAsked?.Invoke(this, e);
            mnuDelete.Image = Properties.Drawings.delete;
            mnuDelete.Click += (s, e) => DeleteAsked?.Invoke(this, e);

            popMenu.Items.AddRange(new ToolStripItem[] { 
                mnuMove,
                new ToolStripSeparator(),
                mnuDelete
            });

            this.ContextMenuStrip = popMenu;
        }

        private void ReloadMenusCulture()
        {
            // Reload the text for each menu.
            // this is done at construction time and at RefreshUICulture time.
            mnuMove.Text = "Move to current time";
            mnuDelete.Text = Languages.ScreenManagerLang.mnuThumbnailDelete;
        }

        private void ShowButtons()
        {
            btnClose.Visible = true;
        }
        private void HideButtons()
        {
            btnClose.Visible = false;
        }
        
        private void UpdateToolTip()
        {
            toolTips.SetToolTip(pbThumbnail, keyframe.Name + "\n" + keyframe.TimeCode);
        }
        #endregion
    }
}
