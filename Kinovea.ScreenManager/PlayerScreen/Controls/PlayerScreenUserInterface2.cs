﻿#region License
/*
Copyright © Joan Charmant 2008-2009.
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
#endregion

#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Globalization;
using Kinovea.ScreenManager.Languages;
using Kinovea.ScreenManager.Properties;
using Kinovea.Video;
using Kinovea.Services;
using System.Xml;
using System.Text;

#endregion

namespace Kinovea.ScreenManager
{
    public partial class PlayerScreenUserInterface : KinoveaControl, IDrawingHostView
    {
        #region Enums
        private enum PlayingMode
        {
            Once,
            Loop,
            Bounce
        }
        #endregion

        #region Events
        public event EventHandler OpenVideoAsked;
        public event EventHandler OpenReplayWatcherAsked;
        public event EventHandler OpenAnnotationsAsked;
        public event EventHandler CloseAsked;
        public event EventHandler StopWatcherAsked;
        public event EventHandler StartWatcherAsked;
        public event EventHandler SetAsActiveScreen;
        public event EventHandler SpeedChanged;
        public event EventHandler TimeOriginChanged;
        public event EventHandler KVAImported;
        public event EventHandler PlayStarted;
        public event EventHandler PauseAsked;
        public event EventHandler ResetAsked;
        public event EventHandler FilterExited;
        public event EventHandler<EventArgs<bool>> SelectionChanged;
        public event EventHandler<EventArgs<Bitmap>> ImageChanged;
        public event EventHandler<KeyframeAddEventArgs> KeyframeAdding;
        public event EventHandler<KeyframeEventArgs> KeyframeDeleting;
        public event EventHandler<DrawingEventArgs> DrawingAdding;
        public event EventHandler<DrawingEventArgs> DrawingDeleting;
        public event EventHandler<MultiDrawingItemEventArgs> MultiDrawingItemAdding;
        public event EventHandler<MultiDrawingItemEventArgs> MultiDrawingItemDeleting;
        public event EventHandler<TrackableDrawingEventArgs> TrackableDrawingAdded;
        public event EventHandler<EventArgs<HotkeyCommand>> DualCommandReceived;
        #endregion

        #region Commands encapsulating domain logic implemented in the presenter.
        public ToggleCommand ToggleTrackingCommand { get; set; }
        public RelayCommand<VideoFrame> TrackDrawingsCommand { get; set; }
        #endregion

        #region Properties
        public bool IsCurrentlyPlaying
        {
            get { return m_bIsCurrentlyPlaying; }
        }
        public bool IsWaitingForIdle { get; private set; }

        public bool ImageFill 
        {  
            get { return m_fill; }
        }

        /// <summary>
        /// Returns the interval between frames in milliseconds, taking slow motion slider into account.
        /// This is suitable for a playback loop timer or metadata in saved file.
        /// </summary>
        public double FrameInterval
        {
            get
            {
                return timeMapper.GetInterval(sldrSpeed.Value);
            }
        }

        /// <summary>
        /// Returns the playback speed as a percentage of the real time speed of the captured action.
        /// This is not the same as the raw slider percentage when the video is not real time.
        /// </summary>
        public double RealtimePercentage
        {
            get
            {
                return timeMapper.GetRealtimeMultiplier(sldrSpeed.Value) * 100;
            }
            set
            {
                // This happens only in the context of synching 
                // when the other video changed its speed percentage (user or forced).
                // We must NOT trigger the SpeedChanged event here, or it will impact the other screen in an infinite loop.

                slowMotion = value * m_FrameServer.Metadata.HighSpeedFactor / 100;
                sldrSpeed.Value = timeMapper.GetInputFromSlowMotion(slowMotion);
                sldrSpeed.Invalidate();

                // Reset timer with new value.
                if (m_bIsCurrentlyPlaying)
                {
                    StopMultimediaTimer();
                    StartMultimediaTimer(GetPlaybackFrameInterval());
                }

                UpdateSpeedLabel();
            }
        }

        /// <summary>
        /// Returns the raw percentage of the slider.
        /// This is the percentage of nominal framerate of the video.
        /// </summary>
        public double SpeedPercentage
        {
            get { return slowMotion * 100; }
        }

        public ScreenDescriptionPlayback LaunchDescription
        {
            get { return m_LaunchDescription; }
            set { m_LaunchDescription = value; }
        }

        public long CurrentTimestamp
        {
            get { return m_iCurrentPosition; }
        }

        public bool Synched
        {
            //get { return m_bSynched; }
            set
            {
                m_bSynched = value;

                if (!m_bSynched)
                {
                    // We do not reset the time origin.
                    trkFrame.UpdateMarkers(m_FrameServer.Metadata);
                    UpdateCurrentPositionLabel();

                    m_bSyncMerge = false;
                    if (m_SyncMergeImage != null)
                        m_SyncMergeImage.Dispose();
                }
            }
        }

        /// <summary>
        /// Returns the current frame time relative to selection start.
        /// The value is a physical time in microseconds, taking high speed factor into account.
        /// </summary>
        public long LocalTime
        {
            get
            {
                return TimestampToRealtime(m_iCurrentPosition - m_iSelStart);
            }
        }

        /// <summary>
        /// Returns the last valid time relative to selection start.
        /// The value is a physical time in microseconds, taking high speed factor into account.
        /// </summary>
        public long LocalLastTime
        {
            get
            {
                return TimestampToRealtime(m_iSelEnd - m_iSelStart);
            }
        }

        /// <summary>
        /// Returns the average time of one frame.
        /// The value is a physical time in microseconds, taking high speed factor into account.
        /// </summary>
        public long LocalFrameTime
        {
            get
            {
                return TimestampToRealtime(m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame);
            }

        }

        /// <summary>
        /// Returns the time origin relative to selection start.
        /// The value is a physical time in microseconds, taking high speed factor into account.
        /// </summary>
        public long LocalTimeOriginPhysical
        {
            get
            {
                return TimestampToRealtime(m_FrameServer.Metadata.TimeOrigin - m_iSelStart);
            }
        }

        /// <summary>
        /// Gets or sets whether we should draw the other screen image on top of this one. 
        /// </summary>
        public bool SyncMerge
        {
            get { return m_bSyncMerge; }
            set
            {
                m_bSyncMerge = value;

                m_FrameServer.ImageTransform.AllowOutOfScreen = m_bSyncMerge;

                if (!m_bSyncMerge && m_SyncMergeImage != null)
                {
                    m_SyncMergeImage.Dispose();
                }

                DoInvalidate();
            }
        }
        public bool DualSaveInProgress
        {
            set { m_DualSaveInProgress = value; }
        }
        #endregion

        #region Members
        private FrameServerPlayer m_FrameServer;

        // Playback current state
        private bool m_bIsCurrentlyPlaying;
        private int m_iFramesToDecode = 1;
        private uint m_IdMultimediaTimer;
        private PlayingMode m_ePlayingMode = PlayingMode.Loop;
        private bool m_bIsBusyRendering;
        private int m_RenderingDrops;
        private object m_TimingSync = new object();

        // Timing
        private TimeMapper timeMapper = new TimeMapper();
        private double slowMotion = 1;  // Current scaling relatively to the nominal speed of the video.
        private float timeGrabSpeed = 25.0f / 500.0f; // Speed of time grab in frames per pixel.

        // Synchronisation
        private bool m_bSynched;
        private bool m_bSyncMerge;
        private Bitmap m_SyncMergeImage;
        private ColorMatrix m_SyncMergeMatrix = new ColorMatrix();
        private ImageAttributes m_SyncMergeImgAttr = new ImageAttributes();
        private float m_SyncAlpha = 0.5f;
        private bool m_DualSaveInProgress;
        private bool saveInProgress;

        // Image
        private ViewportManipulator m_viewportManipulator = new ViewportManipulator();
        private bool m_fill;
        private double m_lastUserStretch = 1.0f;
        private bool m_bShowImageBorder;
        private bool m_bManualSqueeze = true; // If it's allowed to manually reduce the rendering surface under the aspect ratio size.
        private static readonly Pen m_PenImageBorder = Pens.SteelBlue;
        private static readonly Size m_MinimalSize = new Size(160, 120);
        private bool m_bEnableCustomDecodingSize = true;

        // Selection and current position. All values in absolute timestamps.
        // trkSelection.minimum and maximum are also in absolute timestamps.
        private long m_iTotalDuration = 100;
        private long m_iSelStart;
        private long m_iSelEnd = 99;
        private long m_iSelDuration = 100;
        private long m_iCurrentPosition;
        private long m_iStartingPosition;   // Timestamp of the first decoded frame of video.
        private bool m_bHandlersLocked;

        // Keyframes, Drawings, etc.
        private List<KeyframeBox> keyframeBoxes = new List<KeyframeBox>();
        private int m_iActiveKeyFrameIndex = -1;	// The index of the keyframe we are on, or -1 if not a KF.
        private AbstractDrawingTool m_ActiveTool;
        private DrawingToolPointer m_PointerTool;
        private formKeyframeComments m_KeyframeCommentsHub;
        private bool m_bKeyframePanelCollapsed = true;
        private bool m_bKeyframePanelCollapsedManual = false;
        private bool m_bTextEdit;
        private PointF m_DescaledMouse;    // The current mouse point expressed in the original image size coordinates.
        private bool showDrawings = true;

        // Others
        private NativeMethods.TimerCallback m_TimerCallback;
        private ScreenDescriptionPlayback m_LaunchDescription;
        private bool videoFilterIsActive;
        private ZoomHelper zoomHelper = new ZoomHelper();
        private const int m_MaxRenderingDrops = 6;
        private const int m_MaxDecodingDrops = 6;
        private System.Windows.Forms.Timer m_DeselectionTimer = new System.Windows.Forms.Timer();
        private MessageToaster m_MessageToaster;
        private bool m_Constructed;
        private CursorManager cursorManager = new CursorManager();

        #region Context Menus
        private ContextMenuStrip popMenu = new ContextMenuStrip();
        private ToolStripMenuItem mnuTimeOrigin = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDirectTrack = new ToolStripMenuItem();
        private ToolStripMenuItem mnuCopyPic = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPastePic = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPasteDrawing = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOpenVideo = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOpenReplayWatcher = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOpenAnnotations = new ToolStripMenuItem();
        private ToolStripMenuItem mnuSaveAnnotations = new ToolStripMenuItem();
        private ToolStripMenuItem mnuSaveAnnotationsAs = new ToolStripMenuItem();
        private ToolStripMenuItem mnuExportVideo = new ToolStripMenuItem();
        private ToolStripMenuItem mnuExportImage = new ToolStripMenuItem();
        private ToolStripMenuItem mnuCloseScreen = new ToolStripMenuItem();
        private ToolStripMenuItem mnuExitFilter = new ToolStripMenuItem();


        private ContextMenuStrip popMenuDrawings = new ContextMenuStrip();
        private ToolStripMenuItem mnuConfigureDrawing = new ToolStripMenuItem();
        private ToolStripMenuItem mnuSetStyleAsDefault = new ToolStripMenuItem();
        private ToolStripMenuItem mnuVisibility = new ToolStripMenuItem();
        private ToolStripMenuItem mnuVisibilityAlways = new ToolStripMenuItem();
        private ToolStripMenuItem mnuVisibilityDefault = new ToolStripMenuItem();
        private ToolStripMenuItem mnuVisibilityCustom = new ToolStripMenuItem();
        private ToolStripMenuItem mnuVisibilityConfigure = new ToolStripMenuItem();
        private ToolStripMenuItem mnuGotoKeyframe = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDrawingTracking = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDrawingTrackingConfigure = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDrawingTrackingStart = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDrawingTrackingStop = new ToolStripMenuItem();
        private ToolStripSeparator mnuSepDrawing = new ToolStripSeparator();
        private ToolStripSeparator mnuSepDrawing2 = new ToolStripSeparator();
        private ToolStripSeparator mnuSepDrawing3 = new ToolStripSeparator();
        private ToolStripMenuItem mnuCutDrawing = new ToolStripMenuItem();
        private ToolStripMenuItem mnuCopyDrawing = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDeleteDrawing = new ToolStripMenuItem();

        private ContextMenuStrip popMenuTrack = new ContextMenuStrip();
        private ToolStripMenuItem mnuConfigureTrajectory = new ToolStripMenuItem();
        private ToolStripMenuItem mnuDeleteTrajectory = new ToolStripMenuItem();

        private ContextMenuStrip popMenuMagnifier = new ContextMenuStrip();
        private ToolStripMenuItem mnuMagnifierFreeze = new ToolStripMenuItem();
        private ToolStripMenuItem mnuMagnifierTrack = new ToolStripMenuItem();
        private ToolStripMenuItem mnuMagnifierDirect = new ToolStripMenuItem();
        private ToolStripMenuItem mnuMagnifierQuit = new ToolStripMenuItem();

        private ContextMenuStrip popMenuFilter = new ContextMenuStrip();
        #endregion

        private ToolStripButton m_btnAddKeyFrame;
        private ToolStripButton m_btnShowComments;
        private ToolStripButton m_btnToolPresets;
        private InfobarPlayer infobar = new InfobarPlayer();
        private bool showPropertiesPanel;
        private SidePanelKeyframes sidePanelKeyframes = new SidePanelKeyframes();

        private DropWatcher m_DropWatcher = new DropWatcher();
        private TimeWatcher m_TimeWatcher = new TimeWatcher();
        private LoopWatcher m_LoopWatcher = new LoopWatcher();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Constructor
        public PlayerScreenUserInterface(FrameServerPlayer _FrameServer, DrawingToolbarPresenter drawingToolbarPresenter)
        {
            log.Debug("Constructing the PlayerScreen user interface.");

            m_FrameServer = _FrameServer;

            m_FrameServer.Metadata = new Metadata(m_FrameServer.HistoryStack, m_FrameServer.TimeStampsToTimecode);
            m_FrameServer.Metadata.KVAImported += (s, e) => AfterKVAImported();
            m_FrameServer.Metadata.KeyframeAdded += (s, e) => AfterKeyframeAdded(e.KeyframeId);
            m_FrameServer.Metadata.KeyframeModified += (s, e) => AfterKeyframeModified(e.KeyframeId);
            m_FrameServer.Metadata.KeyframeDeleted += (s, e) => AfterKeyframeDeleted();
            m_FrameServer.Metadata.DrawingAdded += (s, e) => AfterDrawingAdded(e.Drawing);
            m_FrameServer.Metadata.DrawingModified += (s, e) => AfterDrawingModified(e.Drawing);
            m_FrameServer.Metadata.DrawingDeleted += (s, e) => AfterDrawingDeleted();
            m_FrameServer.Metadata.MultiDrawingItemAdded += (s, e) => AfterMultiDrawingItemAdded();
            m_FrameServer.Metadata.MultiDrawingItemDeleted += (s, e) => AfterMultiDrawingItemDeleted();
            m_FrameServer.Metadata.VideoFilterModified += (s, e) => AfterVideoFilterModified();

            InitializeComponent();
            InitializeInfobar();
            InitializePropertiesPanel();
            InitializeDrawingTools(drawingToolbarPresenter);
            BuildContextMenus();
            AfterSyncAlphaChange();
            m_MessageToaster = new MessageToaster(pbSurfaceScreen);

            // Drag & drop between keyframe list and bottom panel.
            trkFrame.KeyframeDropped += trkFrame_KeyframeDropped;
            panelVideoControls.AllowDrop = true;
            panelVideoControls.DragOver += PanelVideoControls_DragOver;
            panelVideoControls.DragDrop += PanelVideoControls_DragDrop;

            // Most members and controls should be initialized with the right value.
            // So we don't need to do an extra ResetData here.

            // Controls that renders differently between run time and design time.
            this.Dock = DockStyle.Fill;
            ShowHideRenderingSurface(false);
            SetupPrimarySelectionPanel();
            SetupKeyframeCommentsHub();
            pnlThumbnails.Controls.Clear();
            keyframeBoxes.Clear();
            CollapseKeyframePanel(true);

            m_TimerCallback = MultimediaTimer_Tick;
            m_DeselectionTimer.Interval = 10000;
            m_DeselectionTimer.Tick += DeselectionTimer_OnTick;

            sldrSpeed.Minimum = 0;
            sldrSpeed.Maximum = 1000;
            timeMapper.SetInputRange(sldrSpeed.Minimum, sldrSpeed.Maximum);
            timeMapper.SetSlowMotionRange(0, 2);
            slowMotion = 1;
            sldrSpeed.Initialize(timeMapper.GetInputFromSlowMotion(slowMotion));

            EnableDisableActions(false);

            this.Hotkeys = HotkeySettingsManager.LoadHotkeys("PlayerScreen");
        }
        #endregion

        #region Public Methods
        public void ResetToEmptyState()
        {
            // Called when we load a new video over an already loaded screen.
            // also recalled if the video loaded but the first frame cannot be displayed.
            // This should reset everything except the playback speed.
            log.Debug("Reset screen to empty state.");

            // 1. Reset all data.
            m_FrameServer.Unload();
            ResetData();
            videoFilterIsActive = false;

            // 2. Reset all interface.
            ShowHideRenderingSurface(false);
            SetupPrimarySelectionPanel();
            ClearKeyframeBoxes();
            CollapseKeyframePanel(true);
            UpdateFramesMarkers();
            EnableDisableAllPlayingControls(true);
            EnableDisableDrawingTools(true);
            EnableDisableSnapshot(true);
            buttonPlay.Image = Player.flatplay;
            sldrSpeed.Enabled = false;
            m_KeyframeCommentsHub.Hide();
            m_LaunchDescription = null;
            infobar.Visible = false;

            if (ResetAsked != null)
                ResetAsked(this, EventArgs.Empty);
        }
        private void ClearKeyframeBoxes()
        {
            for (int i = keyframeBoxes.Count - 1; i >= 0; i--)
            {
                KeyframeBox box = keyframeBoxes[i];

                box.DeleteAsked -= KeyframeControl_KeyframeDeleteAsked;
                box.Selected -= KeyframeControl_KeyframeSelected;

                keyframeBoxes.Remove(box);
                pnlThumbnails.Controls.Remove(box);
                box.Dispose();
            }

            sidePanelKeyframes.Clear();
        }
        public void EnableDisableActions(bool _bEnable)
        {
            // Called back after a load error.
            // Prevent any actions.
            if (!_bEnable)
                DisablePlayAndDraw();

            EnableDisableSnapshot(_bEnable);
            EnableDisableDrawingTools(_bEnable);

            if (_bEnable && m_FrameServer.Loaded && m_FrameServer.VideoReader.IsSingleFrame)
                EnableDisableAllPlayingControls(false);
            else
                EnableDisableAllPlayingControls(_bEnable);
        }
        public int PostLoadProcess()
        {
            //---------------------------------------------------------------------------
            // Configure the interface according to he video and try to read first frame.
            // Called from CommandLoadMovie when VideoFile.Load() is successful.
            //---------------------------------------------------------------------------
            log.DebugFormat("Post load process.");

            ShowNextFrame(-1, true);
            UpdatePositionUI();

            if (m_FrameServer.VideoReader.Current == null)
            {
                m_FrameServer.Unload();
                log.Error("First frame couldn't be loaded - aborting");
                return -1;
            }
            else if (m_iCurrentPosition < 0)
            {
                // First frame loaded but inconsistency. (Seen with some AVCHD)
                m_FrameServer.Unload();
                log.Error(String.Format("First frame loaded but negative timestamp ({0}) - aborting", m_iCurrentPosition));
                return -2;
            }

            log.DebugFormat("First frame loaded.");

            //---------------------------------------------------------------------------------------
            // First frame loaded.
            //
            // We will now update the internal data of the screen ui and
            // set up the various child controls (like the timelines).
            // Call order matters.
            // Some bugs come from variations between what the file infos advertised and the reality.
            // We fix what we can with the help of data read from the first frame or 
            // from the analysis mode switch if successful.
            //---------------------------------------------------------------------------------------

            DoInvalidate();

            m_iStartingPosition = m_iCurrentPosition;
            m_iTotalDuration = m_FrameServer.VideoReader.Info.DurationTimeStamps;
            m_iSelStart = m_iStartingPosition;
            m_iSelEnd = m_FrameServer.VideoReader.WorkingZone.End;
            m_iSelDuration = m_iTotalDuration;

            if (!m_FrameServer.VideoReader.CanChangeWorkingZone)
                EnableDisableWorkingZoneControls(false);

            // Update the control.
            // FIXME - already done in ImportSelectionToMemory ?
            SetupPrimarySelectionPanel();

            // Other various infos.
            m_FrameServer.SetupMetadata(true);
            m_FrameServer.Metadata.VideoPath = m_FrameServer.VideoReader.FilePath;
            m_FrameServer.Metadata.SelectionStart = m_iSelStart;
            m_FrameServer.Metadata.SelectionEnd = m_iSelEnd;
            m_FrameServer.Metadata.TimeOrigin = m_iSelStart;
            m_PointerTool.SetImageSize(m_FrameServer.VideoReader.Info.ReferenceSize);
            m_viewportManipulator.Initialize(m_FrameServer.VideoReader);

            // Screen position and size.
            m_FrameServer.ImageTransform.SetReferenceSize(m_FrameServer.VideoReader.Info.ReferenceSize);
            m_FrameServer.ImageTransform.ResetZoom();
            zoomHelper.Value = 1.0f;
            m_PointerTool.SetZoomLocation(new Point(-1, -1));
            SetUpForNewMovie();
            m_KeyframeCommentsHub.UserActivated = false;

            // Check for launch description and startup kva.
            bool recoveredMetadata = false;
            if (m_LaunchDescription != null)
            {
                // Starting the filesystem watcher for .IsReplayWatcher is done in PlayerScreen.
                // Starting the video for .Play is done later at first Idle.
                if (m_LaunchDescription.Id != Guid.Empty)
                    recoveredMetadata = m_FrameServer.Metadata.Recover(m_LaunchDescription.Id);

                if (m_LaunchDescription.Stretch)
                {
                    m_fill = true;
                    ResizeUpdate(true);
                }
            }

            if (!recoveredMetadata)
            {
                // Side-car KVA.
                foreach (string extension in MetadataSerializer.SupportedFileFormats())
                {
                    string candidate = Path.Combine(Path.GetDirectoryName(m_FrameServer.VideoReader.FilePath), Path.GetFileNameWithoutExtension(m_FrameServer.VideoReader.FilePath) + extension);
                    LookForLinkedAnalysis(candidate);
                }

                // Startup KVA.
                string startupFile = PreferencesManager.PlayerPreferences.PlaybackKVA;
                if (!string.IsNullOrEmpty(startupFile))
                {
                    if (Path.IsPathRooted(startupFile))
                        LookForLinkedAnalysis(startupFile);
                    else
                        LookForLinkedAnalysis(Path.Combine(Software.SettingsDirectory, startupFile));
                }
            }

            if (m_LaunchDescription != null)
            {
                // We assume this is a speed percentage of video framerate, not real time.
                // We must do this after KVA loading because it may reset the slowmotion.
                slowMotion = m_LaunchDescription.SpeedPercentage / 100.0;
            }

            UpdateTimebase();
            UpdateInfobar();

            sldrSpeed.Force(timeMapper.GetInputFromSlowMotion(slowMotion));
            sldrSpeed.Enabled = true;

            if (!recoveredMetadata)
                m_FrameServer.Metadata.CleanupHash();

            m_FrameServer.Metadata.StartAutosave();

            log.DebugFormat("End of post load process, waiting for idle.");
            IsWaitingForIdle = true;
            Application.Idle += PostLoad_Idle;

            return 0;
        }
        private void AfterKVAImported()
        {
            InitializeKeyframes();

            // Restore things like aspect ratio, image rotation, deinterlacing, etc.
            m_FrameServer.RestoreImageOptions();

            // Restore selection.
            // Force a reload of the cache to account for possible changes in aspect ratio, image rotation, etc.
            m_iSelStart = m_FrameServer.Metadata.SelectionStart;
            m_iSelEnd = m_FrameServer.Metadata.SelectionEnd;
            m_iSelDuration = m_iSelEnd - m_iSelStart + m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame;
            UpdateWorkingZone(true);

            RestoreActiveVideoFilter();

            UpdateInfobar();
            OrganizeKeyframes();
            if (m_FrameServer.Metadata.Count > 0 && !m_bKeyframePanelCollapsedManual)
                CollapseKeyframePanel(false);

            m_iFramesToDecode = 1;
            ShowNextFrame(m_iSelStart, true);
            UpdatePositionUI();
            ActivateKeyframe(m_iCurrentPosition);

            double oldHSF = m_FrameServer.Metadata.HighSpeedFactor;
            double captureInterval = 1000 / m_FrameServer.Metadata.CalibrationHelper.CaptureFramesPerSecond;
            m_FrameServer.Metadata.HighSpeedFactor = m_FrameServer.Metadata.UserInterval / captureInterval;
            UpdateTimebase();

            m_FrameServer.SetupMetadata(false);
            ImportEditboxes();
            m_PointerTool.SetImageSize(m_FrameServer.Metadata.ImageSize);

            if (KVAImported != null)
                KVAImported(this, EventArgs.Empty);

            trkFrame.UpdateMarkers(m_FrameServer.Metadata);
            UpdateTimeLabels();
            DoInvalidate();
        }

        /// <summary>
        /// Restore the active video filter after KVA import.
        /// </summary>
        private void RestoreActiveVideoFilter()
        {
            if (m_FrameServer.VideoReader.DecodingMode != VideoDecodingMode.Caching)
            {
                // The filter is not allowed to be activated.
                // This may happen if we load a KVA after having lowered the cache size.
                m_FrameServer.DeactivateVideoFilter();
                DeactivateVideoFilter();
            }
            else if (m_FrameServer.Metadata.ActiveVideoFilterType == VideoFilterType.None)
            {
                // Exiting filter.
                m_FrameServer.DeactivateVideoFilter();
                DeactivateVideoFilter();
            }
            else
            {
                // Re-entering filter.
                // It may be a different one so make sure to send it the cached frames.
                m_FrameServer.ActivateVideoFilter(m_FrameServer.Metadata.ActiveVideoFilterType);
                ActivateVideoFilter();
            }
        }
        public void UpdateTimebase()
        {
            timeMapper.FileInterval = m_FrameServer.VideoReader.Info.FrameIntervalMilliseconds;
            timeMapper.UserInterval = m_FrameServer.Metadata.UserInterval;
            timeMapper.CaptureInterval = timeMapper.UserInterval / m_FrameServer.Metadata.HighSpeedFactor;
        }
        public void UpdateTimeLabels()
        {
            UpdateSelectionLabels();
            UpdateCurrentPositionLabel();
            UpdateSpeedLabel();
            UpdateInfobar();
        }

        public void UpdateReplayWatcher(bool replayWatcher, string path)
        {
            infobar.UpdateReplayWatcher(replayWatcher, path);
        }

        /// <summary>
        /// Called after the common controls updated the sync position, impacting time origin in both videos.
        /// </summary>
        public void TimeOriginUpdatedFromSync()
        {
            trkFrame.UpdateMarkers(m_FrameServer.Metadata);
            UpdateCurrentPositionLabel();
        }

        /// <summary>
        /// Try to load the working zone into the cache if possible
        /// and consolidate the boundary values afterwards.
        /// If _bForceReload is true, invalidates the existing cache.
        /// </summary>
        public void UpdateWorkingZone(bool _bForceReload)
        {
            if (!m_FrameServer.Loaded)
                return;

            if (m_FrameServer.VideoReader.CanChangeWorkingZone)
            {
                StopPlaying();
                OnPauseAsked();
                VideoSection newZone = new VideoSection(m_iSelStart, m_iSelEnd);
                m_FrameServer.VideoReader.UpdateWorkingZone(newZone, _bForceReload, PreferencesManager.PlayerPreferences.WorkingZoneMemory, ProgressWorker);
                ResizeUpdate(true);
            }

            // Time origin: we try to maintain user-defined time origin, but we don't want the origin to stay at the absolute zero when the zone changes.
            // Check if we were previously aligned with the start of the zone, if so, keep it that way, otherwise keep the absolute value.
            // A side effect of this approach is that when the start of the zone is moved forward so as to overtake the current time origin, 
            // it will scoop it and drag it along with it.
            if (m_FrameServer.Metadata.TimeOrigin == m_iSelStart)
                m_FrameServer.Metadata.TimeOrigin = m_FrameServer.VideoReader.WorkingZone.Start;

            // Reupdate back the locals as the reader uses more precise values.
            m_iCurrentPosition = m_iCurrentPosition + (m_FrameServer.VideoReader.WorkingZone.Start - m_iSelStart);
            m_iSelStart = m_FrameServer.VideoReader.WorkingZone.Start;
            m_iSelEnd = m_FrameServer.VideoReader.WorkingZone.End;
            m_iSelDuration = m_iSelEnd - m_iSelStart + m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame;

            if (trkSelection.SelStart != m_iSelStart)
                trkSelection.SelStart = m_iSelStart;

            if (trkSelection.SelEnd != m_iSelEnd)
                trkSelection.SelEnd = m_iSelEnd;

            trkFrame.Remap(m_iSelStart, m_iSelEnd, m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame);

            m_iFramesToDecode = 1;
            ShowNextFrame(m_iSelStart, true);

            UpdatePositionUI();
            UpdateSelectionLabels();
            OnPoke();
            RestoreActiveVideoFilter();
            OnSelectionChanged(true);
        }
        private void ProgressWorker(DoWorkEventHandler _doWork)
        {
            formProgressBar2 fpb = new formProgressBar2(true, false, _doWork);
            fpb.ShowDialog();
            fpb.Dispose();
        }
        public void DisplayAsActiveScreen(bool _bActive)
        {
            // Called from ScreenManager.
            ShowBorder(_bActive);
        }
        public void StopPlaying()
        {
            StopPlaying(true);
        }
        public void ForcePosition(long timestamp, bool allowUIUpdate)
        {
            m_iFramesToDecode = 1;
            StopPlaying();

            m_iCurrentPosition = timestamp;

            if (m_iCurrentPosition > m_iSelEnd)
                m_iCurrentPosition = m_iSelEnd;

            ShowNextFrame(m_iCurrentPosition, allowUIUpdate);

            if (allowUIUpdate)
            {
                UpdatePositionUI();
                ActivateKeyframe(m_iCurrentPosition);
            }
        }
        public void ForceCurrentFrame(long frame, bool allowUIUpdate)
        {
            // Called during static sync.
            // Common position changed, we get a new frame to jump to.
            // target frame may be over the total.

            if (!m_FrameServer.Loaded)
                return;

            m_iFramesToDecode = 1;
            StopPlaying();

            if (frame == -1)
            {
                // Special case for +1 frame.
                if (m_iCurrentPosition < m_iSelEnd)
                {
                    ShowNextFrame(-1, allowUIUpdate);
                }
            }
            else
            {
                m_iCurrentPosition = frame * m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame;
                m_iCurrentPosition += m_iSelStart;

                if (m_iCurrentPosition > m_iSelEnd)
                    m_iCurrentPosition = m_iSelEnd;

                ShowNextFrame(m_iCurrentPosition, allowUIUpdate);
            }

            if (allowUIUpdate)
            {
                UpdatePositionUI();
                ActivateKeyframe(m_iCurrentPosition);
            }
        }
        public void RefreshImage()
        {
            // For cases where surfaceScreen.Invalidate() is not enough.
            // Not needed if we are playing.
            if (m_FrameServer.Loaded && !m_bIsCurrentlyPlaying)
                ShowNextFrame(m_iCurrentPosition, true);
        }
        public void RefreshUICulture()
        {
            // Labels
            lblSelStartSelection.AutoSize = true;
            lblSelDuration.AutoSize = true;

            UpdateTimeLabels();
            trkFrame.ShowCacheInTimeline = PreferencesManager.PlayerPreferences.ShowCacheInTimeline;
            
            ReloadTooltipsCulture();
            ReloadToolsCulture();
            ReloadMenusCulture();
            m_KeyframeCommentsHub.RefreshUICulture();
            for (int i = 0; i < keyframeBoxes.Count; i++)
                keyframeBoxes[i].RefreshUICulture();

            // Because this method is called when we change the general preferences,
            // we can use it to update data too.

            // Keyframes positions.
            if (m_FrameServer.Metadata.Count > 0)
            {
                EnableDisableKeyframes();
            }

            m_FrameServer.Metadata.CalibrationHelper.RefreshUnits();
            m_FrameServer.Metadata.UpdateTrajectoriesForKeyframes();

            // Refresh image to update timecode in chronos, grids colors, default fading, etc.
            DoInvalidate();
        }
        public void ActivateVideoFilter()
        {
            videoFilterIsActive = true;
            CollapseKeyframePanel(true);
            m_fill = true;
            ResizeUpdate(true);
        }
        public void DeactivateVideoFilter()
        {
            videoFilterIsActive = false;
            StretchSqueezeSurface(true);
            DoInvalidate();
        }
        
        public void SetSyncMergeImage(Bitmap _SyncMergeImage, bool _bUpdateUI)
        {
            m_SyncMergeImage = _SyncMergeImage;

            if (_bUpdateUI)
            {
                // Ask for a repaint. We don't wait for the next frame to be drawn
                // because the user may be manually moving the other video.
                DoInvalidate();
            }
        }
        public void ReferenceImageSizeChanged()
        {
            m_FrameServer.Metadata.ImageSize = m_FrameServer.VideoReader.Info.ReferenceSize;
            m_PointerTool.SetImageSize(m_FrameServer.VideoReader.Info.ReferenceSize);
            m_FrameServer.ImageTransform.SetReferenceSize(m_FrameServer.VideoReader.Info.ReferenceSize);
            ResetZoom(false);
        }
        public void FullScreen(bool _bFullScreen)
        {
            if (_bFullScreen && !m_fill)
            {
                m_fill = true;
                ResizeUpdate(true);
            }
        }
        public void BeforeAddImageDrawing()
        {
            if (m_bIsCurrentlyPlaying)
            {
                StopPlaying();
                OnPauseAsked();
                ActivateKeyframe(m_iCurrentPosition);
            }

            PrepareKeyframesDock();

            m_FrameServer.Metadata.AllDrawingTextToNormalMode();
            m_FrameServer.Metadata.DeselectAll();
            AddKeyframe();
        }
        #endregion

        #region Various Inits & Setups
        private void InitializeInfobar()
        {
            this.panelTop.Controls.Add(infobar);
            infobar.Visible = false;
            infobar.StopWatcherAsked += (s, e) => StopWatcherAsked?.Invoke(s, e);
            infobar.StartWatcherAsked += (s, e) => StartWatcherAsked?.Invoke(s, e); 
        }

        private void InitializePropertiesPanel()
        {
            // Restore splitter distance and hook preferences save.
            splitViewport_Properties.SplitterDistance = (int)(splitViewport_Properties.Width * PreferencesManager.GeneralPreferences.SidePanelSplitterRatio);
            splitViewport_Properties.SplitterMoved += (s, e) => {
                PreferencesManager.GeneralPreferences.SidePanelSplitterRatio = (float)e.SplitX / splitViewport_Properties.Width;
                PreferencesManager.Save();
            };

            // Create and add all the side panels.
            TabControl tabControl =  splitViewport_Properties.Panel2.Controls[0] as TabControl;
            if (tabControl == null)
                return;

            tabControl.TabPages[0].Controls.Add(sidePanelKeyframes);
            sidePanelKeyframes.Dock = DockStyle.Fill;
            sidePanelKeyframes.KeyframeSelected += KeyframeControl_KeyframeSelected;
            sidePanelKeyframes.KeyframeUpdated += KeyframeControl_KeyframeUpdated;

            // Hide work-in-progress panels.
            tabControl.TabPages.RemoveAt(1);

            splitViewport_Properties.Panel2Collapsed = true;
        }

        private void InitializeDrawingTools(DrawingToolbarPresenter drawingToolbarPresenter)
        {
            m_PointerTool = new DrawingToolPointer();
            m_ActiveTool = m_PointerTool;

            drawingToolbarPresenter.ForceView(stripDrawingTools);

            // Hand tool.
            drawingToolbarPresenter.AddToolButton(m_PointerTool, drawingTool_Click);

            // Create key image.
            m_btnAddKeyFrame = CreateToolButton();
            m_btnAddKeyFrame.Image = Resources.createkeyframe;
            m_btnAddKeyFrame.Click += btnAddKeyframe_Click;
            m_btnAddKeyFrame.ToolTipText = ScreenManagerLang.ToolTip_AddKeyframe;
            drawingToolbarPresenter.AddSpecialButton(m_btnAddKeyFrame);

            // Side panel toggle.
            m_btnShowComments = CreateToolButton();
            m_btnShowComments.Image = Resources.sidepanel;
            m_btnShowComments.Click += btnShowSidePanel_Click;
            m_btnShowComments.ToolTipText = ScreenManagerLang.ToolTip_ShowComments;
            drawingToolbarPresenter.AddSpecialButton(m_btnShowComments);

            drawingToolbarPresenter.AddSeparator();

            // All drawing tools.
            DrawingToolbarImporter importer = new DrawingToolbarImporter();
            importer.Import("player.xml", drawingToolbarPresenter, drawingTool_Click);

            drawingToolbarPresenter.AddToolButton(ToolManager.Tools["Magnifier"], btnMagnifier_Click);

            // Special button: Tool presets
            m_btnToolPresets = CreateToolButton();
            m_btnToolPresets.Image = Resources.SwatchIcon3;
            m_btnToolPresets.Click += btnColorProfile_Click;
            m_btnToolPresets.ToolTipText = ScreenManagerLang.ToolTip_ColorProfile;
            drawingToolbarPresenter.AddSpecialButton(m_btnToolPresets);

            stripDrawingTools.Left = 3;
        }
        private ToolStripButton CreateToolButton()
        {
            ToolStripButton btn = new ToolStripButton();
            btn.AutoSize = false;
            btn.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btn.ImageScaling = ToolStripItemImageScaling.None;
            btn.Size = new Size(25, 25);
            btn.AutoToolTip = false;
            return btn;
        }
        private void ResetData()
        {
            m_iFramesToDecode = 1;

            m_bIsCurrentlyPlaying = false;
            m_ePlayingMode = PlayingMode.Loop;
            m_fill = false;
            m_FrameServer.ImageTransform.Reset();
            m_lastUserStretch = 1.0f;

            // Sync
            m_bSynched = false;
            m_bSyncMerge = false;
            if (m_SyncMergeImage != null)
                m_SyncMergeImage.Dispose();

            m_bShowImageBorder = false;

            SetupPrimarySelectionData();    // Should not be necessary when every data is coming from m_FrameServer.

            m_bHandlersLocked = false;

            m_iActiveKeyFrameIndex = -1;
            m_ActiveTool = m_PointerTool;

            m_bKeyframePanelCollapsed = true;
            m_bTextEdit = false;

            m_FrameServer.Metadata.HighSpeedFactor = 1.0f;
            UpdateTimebase();
            UpdateTimeLabels();
        }
        private void SetupPrimarySelectionData()
        {
            // Setup data
            if (m_FrameServer.Loaded)
            {
                m_iSelStart = m_iStartingPosition;
                m_iSelEnd = m_iStartingPosition + m_iTotalDuration - m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame;
                m_iSelDuration = m_iTotalDuration;
            }
            else
            {
                m_iSelStart = 0;
                m_iSelEnd = 99;
                m_iSelDuration = 100;
                m_iTotalDuration = 100;

                m_iCurrentPosition = 0;
                m_iStartingPosition = 0;
            }

            m_FrameServer.Metadata.TimeOrigin = m_iSelStart;
        }
        private void SetupPrimarySelectionPanel()
        {
            // Setup controls & labels.
            // Update internal state only, doesn't trigger the events.
            trkSelection.UpdateInternalState(m_iSelStart, m_iSelEnd, m_iSelStart, m_iSelEnd, m_iSelStart);
            UpdateSelectionLabels();
        }
        private void SetUpForNewMovie()
        {
            OnPoke();
        }
        private void SetupKeyframeCommentsHub()
        {
            m_KeyframeCommentsHub = new formKeyframeComments(this);
            FormsHelper.MakeTopmost(m_KeyframeCommentsHub);
        }
        private void LookForLinkedAnalysis(string file)
        {
            if (File.Exists(file))
            {
                MetadataSerializer s = new MetadataSerializer();
                s.Load(m_FrameServer.Metadata, file, true);
            }
        }
        private void UpdateInfobar()
        {
            if (!m_FrameServer.Loaded)
                return;

            string size = string.Format("{0}×{1} px", m_FrameServer.Metadata.ImageSize.Width, m_FrameServer.Metadata.ImageSize.Height);
            string fps = string.Format("{0:0.00} fps", 1000 / timeMapper.UserInterval);

            infobar.Visible = true;
            infobar.Dock = DockStyle.Fill;
            infobar.UpdateValues(m_FrameServer.VideoReader.FilePath, size, fps);
        }
        private void ShowHideRenderingSurface(bool _bShow)
        {
            ImageResizerNE.Visible = _bShow;
            ImageResizerNW.Visible = _bShow;
            ImageResizerSE.Visible = _bShow;
            ImageResizerSW.Visible = _bShow;
            pbSurfaceScreen.Visible = _bShow;
        }
        private void BuildContextMenus()
        {
            // Attach the event handlers and build the menus.
            // Depending on the context, more menus are added and configured on the fly in SurfaceScreen_RightDown.

            // Background context menu.
            mnuTimeOrigin.Click += mnuTimeOrigin_Click;
            mnuTimeOrigin.Image = Properties.Resources.marker;
            mnuDirectTrack.Click += mnuDirectTrack_Click;
            mnuDirectTrack.Image = Properties.Drawings.track;
            mnuCopyPic.Click += (s, e) => { CopyImageToClipboard(); };
            mnuCopyPic.Image = Properties.Resources.clipboard_block;
            mnuPastePic.Click += mnuPastePic_Click;
            mnuPastePic.Image = Properties.Drawings.paste;
            mnuPasteDrawing.Click += mnuPasteDrawing_Click;
            mnuPasteDrawing.Image = Properties.Drawings.paste;

            mnuOpenVideo.Click += (s, e) => OpenVideoAsked?.Invoke(this, EventArgs.Empty);
            mnuOpenVideo.Image = Properties.Resources.folder;
            mnuOpenReplayWatcher.Click += (s, e) => OpenReplayWatcherAsked?.Invoke(this, EventArgs.Empty);
            mnuOpenReplayWatcher.Image = Properties.Resources.replaywatcher;
            mnuOpenAnnotations.Click += (s, e) => OpenAnnotationsAsked?.Invoke(this, EventArgs.Empty);
            mnuOpenAnnotations.Image = Properties.Resources.file_kva2;

            mnuSaveAnnotations.Click += btnSaveAnnotations_Click;
            mnuSaveAnnotations.Image = Properties.Resources.filesave;
            mnuSaveAnnotationsAs.Click += btnSaveAnnotationsAs_Click;
            mnuSaveAnnotationsAs.Image = Properties.Resources.filesave;
            mnuExportVideo.Click += btnSaveVideo_Click;
            mnuExportVideo.Image = Properties.Resources.film_save;
            mnuExportImage.Click += btnSnapShot_Click;
            mnuExportImage.Image = Properties.Resources.picture_save;
            mnuCloseScreen.Click += btnClose_Click;
            mnuCloseScreen.Image = Properties.Resources.closeplayer;
            mnuExitFilter.Click += MnuExitFilter_Click;
            mnuExitFilter.Image = Properties.Resources.exit_filter;
            popMenu.Items.AddRange(new ToolStripItem[]
            {
                mnuTimeOrigin, mnuDirectTrack, new ToolStripSeparator(),
                mnuCopyPic, mnuPastePic, mnuPasteDrawing, new ToolStripSeparator(),
                mnuOpenVideo, mnuOpenReplayWatcher, mnuOpenAnnotations, new ToolStripSeparator(),
                mnuSaveAnnotations, mnuSaveAnnotationsAs, mnuExportVideo, mnuExportImage, new ToolStripSeparator(),
                mnuCloseScreen
            });

            // Drawings context menu (Configure, Delete, Tracking)
            mnuConfigureDrawing.Click += new EventHandler(mnuConfigureDrawing_Click);
            mnuConfigureDrawing.Image = Properties.Drawings.configure;
            mnuSetStyleAsDefault.Click += new EventHandler(mnuSetStyleAsDefault_Click);
            mnuSetStyleAsDefault.Image = Resources.SwatchIcon3;

            mnuVisibility.Image = Properties.Drawings.persistence;
            mnuVisibilityAlways.Image = Properties.Drawings.persistence;
            mnuVisibilityDefault.Image = Properties.Drawings.persistence;
            mnuVisibilityCustom.Image = Properties.Drawings.persistence;
            mnuVisibilityConfigure.Image = Properties.Drawings.configure;
            mnuVisibilityAlways.Click += mnuVisibilityAlways_Click;
            mnuVisibilityDefault.Click += mnuVisibilityDefault_Click;
            mnuVisibilityCustom.Click += mnuVisibilityCustom_Click;
            mnuVisibilityConfigure.Click += mnuVisibilityConfigure_Click;
            mnuVisibility.DropDownItems.AddRange(new ToolStripItem[]
            {
                mnuVisibilityAlways,
                mnuVisibilityDefault,
                mnuVisibilityCustom,
                new ToolStripSeparator(),
                mnuVisibilityConfigure
            });

            mnuGotoKeyframe.Click += new EventHandler(mnuGotoKeyframe_Click);
            mnuGotoKeyframe.Image = Properties.Resources.page_white_go;

            mnuDrawingTracking.Image = Properties.Drawings.track;
            mnuDrawingTrackingConfigure.Click += mnuDrawingTrackingConfigure_Click;
            mnuDrawingTrackingConfigure.Image = Properties.Drawings.configure;
            mnuDrawingTrackingStart.Click += mnuDrawingTrackingToggle_Click;
            mnuDrawingTrackingStart.Image = Properties.Drawings.trackingplay;
            mnuDrawingTrackingStop.Click += mnuDrawingTrackingToggle_Click;
            mnuDrawingTrackingStop.Image = Properties.Drawings.trackstop;
            mnuDrawingTracking.DropDownItems.AddRange(new ToolStripItem[] { 
                mnuDrawingTrackingStart, 
                mnuDrawingTrackingStop 
            });

            mnuCutDrawing.Click += new EventHandler(mnuCutDrawing_Click);
            mnuCutDrawing.Image = Properties.Drawings.cut;
            mnuCopyDrawing.Click += new EventHandler(mnuCopyDrawing_Click);
            mnuCopyDrawing.Image = Properties.Drawings.copy;
            mnuDeleteDrawing.Click += new EventHandler(mnuDeleteDrawing_Click);
            mnuDeleteDrawing.Image = Properties.Drawings.delete;

            // Tracks.
            mnuConfigureTrajectory.Click += new EventHandler(mnuConfigureTrajectory_Click);
            mnuConfigureTrajectory.Image = Properties.Drawings.configure;
            mnuDeleteTrajectory.Click += new EventHandler(mnuDeleteTrajectory_Click);
            mnuDeleteTrajectory.Image = Properties.Drawings.delete;

            // Magnifier
            mnuMagnifierFreeze.Click += mnuMagnifierFreeze_Click;
            mnuMagnifierFreeze.Image = Properties.Resources.image;
            mnuMagnifierTrack.Click += mnuMagnifierTrack_Click;
            mnuMagnifierTrack.Image = Properties.Drawings.track;
            mnuMagnifierDirect.Click += mnuMagnifierDirect_Click;
            mnuMagnifierDirect.Image = Properties.Resources.arrow_out;
            mnuMagnifierQuit.Click += mnuMagnifierQuit_Click;
            mnuMagnifierQuit.Image = Properties.Resources.hide;

            // The right context menu and its content will be choosen upon MouseDown.
            panelCenter.ContextMenuStrip = popMenu;

            // Load texts
            ReloadMenusCulture();
        }

        private void PostLoad_Idle(object sender, EventArgs e)
        {
            Application.Idle -= PostLoad_Idle;
            m_Constructed = true;
            IsWaitingForIdle = false;

            log.DebugFormat("Post load idle event.");

            if (!m_FrameServer.Loaded)
                return;

            // This would be a good time to start the prebuffering if supported.
            // The UpdateWorkingZone call may try to go full cache if possible.
            m_FrameServer.VideoReader.PostLoad();

            if (m_LaunchDescription != null && m_LaunchDescription.IsReplayWatcher)
                UpdateWorkingZone(false);
            else
                UpdateWorkingZone(true);

            UpdateFramesMarkers();
            ShowHideRenderingSurface(true);
            ResizeUpdate(true);

            if (m_LaunchDescription != null && m_LaunchDescription.Autoplay)
            {
                buttonPlay.Image = Resources.flatpause3b;
                StartMultimediaTimer(GetPlaybackFrameInterval());
                PlayStarted?.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion

        #region Commands
        protected override bool ExecuteCommand(int cmd)
        {
            // Method called by KinoveaControl in the context of preprocessing hotkeys.
            // If the hotkey can be handled by the dual player, we defer to it instead.

            if (m_FrameServer.Metadata.TextEditingInProgress)
                return false;

            if (keyframeBoxes.Any(t => t.Editing))
                return false;

            if (sidePanelKeyframes.Editing)
                return false;

            if (!m_bSynched)
                return ExecuteScreenCommand(cmd);

            HotkeyCommand command = Hotkeys.FirstOrDefault(h => h != null && h.CommandCode == cmd);
            if (command == null)
                return false;

            bool dualPlayerHandled = HotkeySettingsManager.IsHandler("DualPlayer", command.KeyData);

            if (dualPlayerHandled && DualCommandReceived != null)
            {
                DualCommandReceived(this, new EventArgs<HotkeyCommand>(command));
                return true;
            }
            else
            {
                return ExecuteScreenCommand(cmd);
            }
        }

        public bool ExecuteScreenCommand(int cmd)
        {
            if (!m_FrameServer.Loaded)
                return false;

            PlayerScreenCommands command = (PlayerScreenCommands)cmd;

            switch (command)
            {
                // General
                case PlayerScreenCommands.ResetViewport:
                    DisablePlayAndDraw();
                    DoInvalidate();
                    break;
                case PlayerScreenCommands.Close:
                    btnClose_Click(this, EventArgs.Empty);
                    break;

                // Playback control
                case PlayerScreenCommands.TogglePlay:
                    OnButtonPlay();
                    break;
                case PlayerScreenCommands.IncreaseSpeed1:
                    ChangeSpeed(1);
                    break;
                case PlayerScreenCommands.IncreaseSpeedRoundTo10:
                    ChangeSpeed(10);
                    break;
                case PlayerScreenCommands.IncreaseSpeedRoundTo25:
                    ChangeSpeed(25);
                    break;
                case PlayerScreenCommands.DecreaseSpeed1:
                    ChangeSpeed(-1);
                    break;
                case PlayerScreenCommands.DecreaseSpeedRoundTo10:
                    ChangeSpeed(-10);
                    break;
                case PlayerScreenCommands.DecreaseSpeedRoundTo25:
                    ChangeSpeed(-25);
                    break;

                // Frame by frame navigation
                case PlayerScreenCommands.GotoPreviousImage:
                    buttonGotoPrevious_Click(null, EventArgs.Empty);
                    break;
                case PlayerScreenCommands.GotoNextImage:
                    buttonGotoNext_Click(null, EventArgs.Empty);
                    break;
                case PlayerScreenCommands.GotoFirstImage:
                    buttonGotoFirst_Click(null, EventArgs.Empty);
                    break;
                case PlayerScreenCommands.GotoLastImage:
                    buttonGotoLast_Click(null, EventArgs.Empty);
                    break;
                case PlayerScreenCommands.GotoPreviousImageForceLoop:
                    if (m_iCurrentPosition <= m_iSelStart)
                        buttonGotoLast_Click(null, EventArgs.Empty);
                    else
                        buttonGotoPrevious_Click(null, EventArgs.Empty);
                    break;
                case PlayerScreenCommands.BackwardRound10Percent:
                    JumpToPercent(10, false);
                    break;
                case PlayerScreenCommands.ForwardRound10Percent:
                    JumpToPercent(10, true);
                    break;
                case PlayerScreenCommands.BackwardRound1Percent:
                    JumpToPercent(1, false);
                    break;
                case PlayerScreenCommands.ForwardRound1Percent:
                    JumpToPercent(1, true);
                    break;
                case PlayerScreenCommands.GotoPreviousKeyframe:
                    GotoPreviousKeyframe();
                    break;
                case PlayerScreenCommands.GotoNextKeyframe:
                    GotoNextKeyframe();
                    break;
                case PlayerScreenCommands.GotoSyncPoint:
                    ForceCurrentFrame(m_FrameServer.Metadata.TimeOrigin, true);
                    break;

                // Synchronization
                case PlayerScreenCommands.IncreaseSyncAlpha:
                    IncreaseSyncAlpha();
                    break;
                case PlayerScreenCommands.DecreaseSyncAlpha:
                    DecreaseSyncAlpha();
                    break;

                // Zoom
                case PlayerScreenCommands.IncreaseZoom:
                    IncreaseDirectZoom(new Point(pbSurfaceScreen.Width / 2, pbSurfaceScreen.Height / 2));
                    break;
                case PlayerScreenCommands.DecreaseZoom:
                    DecreaseDirectZoom(new Point(pbSurfaceScreen.Width / 2, pbSurfaceScreen.Height / 2));
                    break;
                case PlayerScreenCommands.ResetZoom:
                    ResetZoom(true);
                    break;

                // Keyframes
                case PlayerScreenCommands.AddKeyframe:
                    AddKeyframe();
                    break;
                case PlayerScreenCommands.DeleteKeyframe:
                    if (m_iActiveKeyFrameIndex >= 0)
                    {
                        Guid id = m_FrameServer.Metadata.GetKeyframeId(m_iActiveKeyFrameIndex);
                        DeleteKeyframe(id);
                    }
                    break;
                case PlayerScreenCommands.Preset1:
                case PlayerScreenCommands.Preset2:
                case PlayerScreenCommands.Preset3:
                case PlayerScreenCommands.Preset4:
                case PlayerScreenCommands.Preset5:
                case PlayerScreenCommands.Preset6:
                case PlayerScreenCommands.Preset7:
                case PlayerScreenCommands.Preset8:
                case PlayerScreenCommands.Preset9:
                case PlayerScreenCommands.Preset10:
                    // Get user-defined keyframe preset.
                    KeyframePreset preset = PreferencesManager.PlayerPreferences.KeyframePresets.GetPreset(command);
                    AddPresetKeyframe(preset.Name, preset.Color);
                    break;

                // Annotations
                case PlayerScreenCommands.CutDrawing:
                    CutDrawing();
                    break;
                case PlayerScreenCommands.CopyDrawing:
                    CopyDrawing();
                    break;
                case PlayerScreenCommands.PasteDrawing:
                    PasteDrawing(false);
                    break;
                case PlayerScreenCommands.PasteInPlaceDrawing:
                    PasteDrawing(true);
                    break;
                case PlayerScreenCommands.DeleteDrawing:
                    DeleteSelectedDrawing();
                    break;
                case PlayerScreenCommands.ValidateDrawing:
                    ValidateDrawing();
                    break;
                case PlayerScreenCommands.CopyImage:
                    CopyImageToClipboard();
                    break;
                case PlayerScreenCommands.ToggleDrawingsVisibility:
                    showDrawings = !showDrawings;
                    DoInvalidate();
                    break;
                case PlayerScreenCommands.ChronometerStartStop:
                    ChronometerStartStop();
                    break;
                case PlayerScreenCommands.ChronometerSplit:
                    ChronometerSplit();
                    break;
                
                

                default:
                    return base.ExecuteCommand(cmd);
            }

            return true;
        }

        public void AfterClose()
        {
            m_KeyframeCommentsHub.Owner = null;
            m_KeyframeCommentsHub.Dispose();
            m_KeyframeCommentsHub = null;

            m_DeselectionTimer.Tick -= DeselectionTimer_OnTick;
            m_DeselectionTimer.Dispose();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();

                panelCenter.ContextMenuStrip = null;

                popMenu.Dispose();
                popMenuDrawings.Dispose();
                popMenuTrack.Dispose();
                popMenuMagnifier.Dispose();
                popMenuFilter.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Misc Events
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (m_KeyframeCommentsHub.Visible)
                m_KeyframeCommentsHub.CommitChanges();

            // Propagate to PlayerScreen which will report to ScreenManager.
            if (CloseAsked != null)
                CloseAsked(this, EventArgs.Empty);
        }
        private void PanelVideoControls_MouseEnter(object sender, EventArgs e)
        {
            // Set focus to enable mouse scroll
            panelVideoControls.Focus();
        }
        private void MnuExitFilter_Click(object sender, EventArgs e)
        {
            m_FrameServer.DeactivateVideoFilter();
            DeactivateVideoFilter();
            FilterExited?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Misc private helpers
        private void OnPauseAsked()
        {
            if (PauseAsked != null)
                PauseAsked(this, EventArgs.Empty);
        }
        private void OnSelectionChanged(bool initialization)
        {
            if (SelectionChanged != null)
                SelectionChanged(this, new EventArgs<bool>(initialization));
        }
        private void RaiseSetAsActiveScreenEvent()
        {
            SetAsActiveScreen?.Invoke(this, EventArgs.Empty);
        }

        private void OnPoke()
        {
            //------------------------------------------------------------------------------
            // This function is a hub event handler for all button press, mouse clicks, etc.
            // Signal itself as the active screen to the ScreenManager
            // This will trigger an update of the top-level menu to enable/disable specific menus.
            //---------------------------------------------------------------------
            RaiseSetAsActiveScreenEvent();

            // 1. Ensure no DrawingText is in edit mode.
            m_FrameServer.Metadata.AllDrawingTextToNormalMode();

            m_ActiveTool = m_ActiveTool.KeepToolFrameChanged ? m_ActiveTool : m_PointerTool;
            if (m_ActiveTool == m_PointerTool)
            {
                SetCursor(m_PointerTool.GetCursor(-1));
            }

            // 3. Dock Keyf panel if nothing to see.
            if (m_FrameServer.Metadata.Count < 1)
            {
                CollapseKeyframePanel(true);
            }
        }

        /// <summary>
        /// Update the markers in the main timeline.
        /// </summary>
        public void UpdateFramesMarkers()
        {
            trkFrame.UpdateMarkers(m_FrameServer.Metadata);
        }
        private void ShowBorder(bool _bShow)
        {
            m_bShowImageBorder = _bShow;
            DoInvalidate();
        }
        private void DrawImageBorder(Graphics _canvas)
        {
            // Draw the border around the screen to mark it as selected.
            // Called back from main drawing routine.
            _canvas.DrawRectangle(m_PenImageBorder, 0, 0, pbSurfaceScreen.Width - m_PenImageBorder.Width, pbSurfaceScreen.Height - m_PenImageBorder.Width);
        }
        private void DisablePlayAndDraw()
        {
            StopPlaying();
            m_ActiveTool = m_PointerTool;
            SetCursor(m_PointerTool.GetCursor(0));
            DisableMagnifier();
            ResetZoom(false);
            m_FrameServer.Metadata.InitializeEnd(true);
            m_FrameServer.Metadata.StopAllTracking();
            m_FrameServer.Metadata.DeselectAll();
            CheckCustomDecodingSize(false);
        }
        private void ValidateDrawing()
        {
            if (m_FrameServer.Metadata.DrawingInitializing)
            {
                m_FrameServer.Metadata.InitializeEnd(true);
                DoInvalidate();
            }
        }

        private void ChronometerStartStop()
        {
            foreach (var drawing in m_FrameServer.Metadata.ChronoManager.Drawings)
            {
                var timeable = drawing as ITimeable;
                if (timeable == null)
                    continue;

                timeable.StartStop(m_iCurrentPosition);
            }

            DoInvalidate();
            UpdateFramesMarkers();
        }

        private void ChronometerSplit()
        {
            foreach (var drawing in m_FrameServer.Metadata.ChronoManager.Drawings)
            {
                var timeable = drawing as ITimeable;
                if (timeable == null)
                    continue;

                timeable.Split(m_iCurrentPosition);
            }

            DoInvalidate();
            UpdateFramesMarkers();
        }



        /// <summary>
        /// Returns the physical time in microseconds for this timestamp.
        /// Used in the context of synchronization.
        /// Input in timestamps relative to sel start.
        /// convert it into video time then to real time using high speed factor.
        private long TimestampToRealtime(long timestamp)
        {
            double correctedTPS = m_FrameServer.VideoReader.Info.FrameIntervalMilliseconds * m_FrameServer.VideoReader.Info.AverageTimeStampsPerSeconds / m_FrameServer.Metadata.UserInterval;

            if (correctedTPS == 0 || m_FrameServer.Metadata.HighSpeedFactor == 0)
                return 0;

            double videoSeconds = (double)timestamp / correctedTPS;
            double realSeconds = videoSeconds / m_FrameServer.Metadata.HighSpeedFactor;
            double realMicroseconds = realSeconds * 1000000;
            return (long)realMicroseconds;
        }
        #endregion

        #region Video Controls

        #region Playback Controls
        public void buttonGotoFirst_Click(object sender, EventArgs e)
        {
            // Jump to start.
            if (m_FrameServer.Loaded)
            {
                OnPoke();
                StopPlaying();
                OnPauseAsked();

                m_iFramesToDecode = 1;
                ShowNextFrame(m_iSelStart, true);

                UpdatePositionUI();
                ActivateKeyframe(m_iCurrentPosition);
            }
        }
        public void buttonGotoPrevious_Click(object sender, EventArgs e)
        {
            if (m_FrameServer.Loaded)
            {
                OnPoke();
                StopPlaying();
                OnPauseAsked();

                //---------------------------------------------------------------------------
                // If we are outside the primary selection or we are about to leave it,
                // reset to the start point.
                //---------------------------------------------------------------------------
                if ((m_iCurrentPosition <= m_iSelStart) || (m_iCurrentPosition > m_iSelEnd))
                {
                    m_iFramesToDecode = 1;
                    ShowNextFrame(m_iSelStart, true);
                }
                else
                {
                    long oldPos = m_iCurrentPosition;
                    m_iFramesToDecode = -1;
                    ShowNextFrame(-1, true);

                    // If it didn't work, try going back two frames to unstuck the situation.
                    // Todo: check if we're going to endup outside the working zone ?
                    if (m_iCurrentPosition == oldPos)
                    {
                        log.Debug("Seeking to previous frame did not work. Moving backward 2 frames.");
                        m_iFramesToDecode = -2;
                        ShowNextFrame(-1, true);
                    }

                    // Reset to normal.
                    m_iFramesToDecode = 1;
                }

                UpdatePositionUI();
                ActivateKeyframe(m_iCurrentPosition);
            }

        }
        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (m_FrameServer.Loaded)
            {
                OnPoke();
                OnButtonPlay();
            }
        }
        public void buttonGotoNext_Click(object sender, EventArgs e)
        {
            if (!m_FrameServer.Loaded)
                return;

            OnPoke();
            StopPlaying();
            OnPauseAsked();
            m_iFramesToDecode = 1;

            // If we are outside the primary zone or going to get out, seek to start.
            // We also only do the seek if we are after the m_iStartingPosition,
            // Sometimes, the second frame will have a time stamp inferior to the first,
            // which sort of breaks our sentinels.
            if (((m_iCurrentPosition < m_iSelStart) || (m_iCurrentPosition >= m_iSelEnd)) &&
                (m_iCurrentPosition >= m_iStartingPosition))
                ShowNextFrame(m_iSelStart, true);
            else
                ShowNextFrame(-1, true);

            UpdatePositionUI();
            ActivateKeyframe(m_iCurrentPosition);
        }
        public void JumpToPercent(int round, bool forward)
        {
            if (!m_FrameServer.Loaded)
                return;

            StopPlaying();
            OnPauseAsked();
            m_iFramesToDecode = 1;

            float normalized = ((float)m_iCurrentPosition - m_iSelStart) / m_iSelDuration;
            int currentPercentage = (int)Math.Round(normalized * 100);
            int maxSteps = 100 / round;
            int currentStep = currentPercentage / round;
            int nextStep = forward ? currentStep + 1 : currentStep - 1;
            nextStep = Math.Max(Math.Min(nextStep, maxSteps), 0);
            int newPercentage = nextStep * round;
            long newPosition = m_iSelStart + (long)(((float)newPercentage / 100) * m_iSelDuration);

            ShowNextFrame(newPosition, true);

            UpdatePositionUI();
            ActivateKeyframe(m_iCurrentPosition);
        }
        public void buttonGotoLast_Click(object sender, EventArgs e)
        {
            if (m_FrameServer.Loaded)
            {
                OnPoke();
                StopPlaying();
                OnPauseAsked();

                m_iFramesToDecode = 1;
                ShowNextFrame(m_iSelEnd, true);

                UpdatePositionUI();
                ActivateKeyframe(m_iCurrentPosition);
            }
        }
        public void OnButtonPlay()
        {
            //--------------------------------------------------------------
            // This function is accessed from ScreenManager.
            // Eventually from a worker thread. (no SetAsActiveScreen here).
            //--------------------------------------------------------------
            if (!m_FrameServer.Loaded)
                return;

            if (m_FrameServer.Metadata.DrawingInitializing)
                return;

            if (m_bIsCurrentlyPlaying)
            {
                // Pause playback.
                StopPlaying();
                OnPauseAsked();
                buttonPlay.Image = Player.flatplay;
                ActivateKeyframe(m_iCurrentPosition);
            }
            else
            {
                // Start playback.
                buttonPlay.Image = Resources.flatpause3b;
                StartMultimediaTimer(GetPlaybackFrameInterval());
                PlayStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Make sure we are playing. 
        /// Does not raise the play asked event.
        /// Used for synchronization.
        /// </summary>
        public void EnsurePlaying()
        {
            if (!m_FrameServer.Loaded || m_FrameServer.Metadata.DrawingInitializing || m_bIsCurrentlyPlaying)
                return;

            buttonPlay.Image = Resources.flatpause3b;
            StartMultimediaTimer(GetPlaybackFrameInterval());
        }

        public void Common_MouseWheel(object sender, MouseEventArgs e)
        {
            // MouseWheel was recorded on one of the controls.
            int steps = e.Delta * SystemInformation.MouseWheelScrollLines / 120;

            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (steps > 0)
                    IncreaseDirectZoom(e.Location);
                else
                    DecreaseDirectZoom(e.Location);
                
            }
            else if ((ModifierKeys & Keys.Alt) == Keys.Alt)
            {
                if (steps > 0)
                    IncreaseSyncAlpha();
                else
                    DecreaseSyncAlpha();
            }
            else
            {
                if (steps > 0)
                {
                    buttonGotoNext_Click(null, EventArgs.Empty);
                }
                else
                {
                    // Shift + Left on first => loop backward.
                    if (((ModifierKeys & Keys.Shift) == Keys.Shift) && m_iCurrentPosition <= m_iSelStart)
                        buttonGotoLast_Click(null, EventArgs.Empty);
                    else
                        buttonGotoPrevious_Click(null, EventArgs.Empty);
                }
            }
        }
        #endregion

        #region Working Zone Selection
        private void BtnTimeOrigin_Click(object sender, EventArgs e)
        {
            MarkTimeOrigin();
        }

        private void MarkTimeOrigin()
        {
            // Set time origin to current time.
            log.DebugFormat("Changing time origin from player. {0} -> {1}.", m_FrameServer.Metadata.TimeOrigin, m_iCurrentPosition);

            m_FrameServer.Metadata.TimeOrigin = m_iCurrentPosition;
            trkFrame.UpdateMarkers(m_FrameServer.Metadata);
            UpdateCurrentPositionLabel();
            sidePanelKeyframes.UpdateTimecodes();

            // This will update the timecode on keyframe boxes if the user hasn't changed the kf name.
            EnableDisableKeyframes();

            // This will update the timecode on any clock object still using the overall time origin.
            DoInvalidate();

            if (TimeOriginChanged != null)
                TimeOriginChanged(this, EventArgs.Empty);
        }

        private void trkSelection_SelectionChanging(object sender, EventArgs e)
        {
            if (m_FrameServer.Loaded)
            {
                StopPlaying();
                OnPauseAsked();

                // Update selection timestamps and labels.
                UpdateSelectionDataFromControl();
                UpdateSelectionLabels();

                // Update the frame tracker internal timestamps (including position if needed).
                trkFrame.Remap(m_iSelStart, m_iSelEnd, m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame);
            }
        }
        private void trkSelection_SelectionChanged(object sender, EventArgs e)
        {
            // Actual update.
            if (m_FrameServer.Loaded)
            {
                UpdateSelectionDataFromControl();
                UpdateWorkingZone(false);

                AfterSelectionChanged();
            }
        }
        private void trkSelection_TargetAcquired(object sender, EventArgs e)
        {
            // User clicked inside selection: Jump to position.
            if (m_FrameServer.Loaded)
            {
                OnPoke();
                StopPlaying();
                OnPauseAsked();
                m_iFramesToDecode = 1;

                ShowNextFrame(trkSelection.SelPos, true);
                m_iCurrentPosition = trkSelection.SelPos + trkSelection.Minimum;

                UpdatePositionUI();
                ActivateKeyframe(m_iCurrentPosition);
            }

        }
        private void btn_HandlersLock_Click(object sender, EventArgs e)
        {
            // Lock the selection handlers.
            if (m_FrameServer.Loaded)
            {
                m_bHandlersLocked = !m_bHandlersLocked;
                trkSelection.SelLocked = m_bHandlersLocked;

                // Update UI accordingly.
                if (m_bHandlersLocked)
                {
                    btn_HandlersLock.Image = Resources.primselec_locked3;
                    toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionUnlock);
                }
                else
                {
                    btn_HandlersLock.Image = Resources.primselec_unlocked3;
                    toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionLock);
                }
            }
        }
        private void btnSetHandlerLeft_Click(object sender, EventArgs e)
        {
            // Set the left handler of the selection at the current frame.
            if (m_FrameServer.Loaded && !m_bHandlersLocked)
            {
                trkSelection.SelStart = m_iCurrentPosition;
                UpdateSelectionDataFromControl();
                UpdateSelectionLabels();
                trkFrame.Remap(m_iSelStart, m_iSelEnd, m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame);
                UpdateWorkingZone(false);

                AfterSelectionChanged();
            }
        }
        private void btnSetHandlerRight_Click(object sender, EventArgs e)
        {
            // Set the right handler of the selection at the current frame.
            if (m_FrameServer.Loaded && !m_bHandlersLocked)
            {
                trkSelection.SelEnd = m_iCurrentPosition;
                UpdateSelectionDataFromControl();
                UpdateSelectionLabels();
                trkFrame.Remap(m_iSelStart, m_iSelEnd, m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame);
                UpdateWorkingZone(false);

                AfterSelectionChanged();
            }
        }
        private void btnHandlersReset_Click(object sender, EventArgs e)
        {
            // Reset both selection sentinels to their max values.
            if (m_FrameServer.Loaded && !m_bHandlersLocked)
            {
                trkSelection.Reset();
                UpdateSelectionDataFromControl();

                // We need to force the reloading of all frames.
                UpdateWorkingZone(true);

                AfterSelectionChanged();
            }
        }

        private void UpdateFramePrimarySelection()
        {
            //--------------------------------------------------------------
            // Update the visible image to reflect the new selection.
            // Checks that the previous current frame is still within selection,
            // jumps to closest sentinel otherwise.
            //--------------------------------------------------------------

            if (m_FrameServer.VideoReader.DecodingMode == VideoDecodingMode.Caching)
            {
                if (m_FrameServer.VideoReader.Current == null)
                    ShowNextFrame(m_iSelStart, true);
                else
                    ShowNextFrame(m_FrameServer.VideoReader.Current.Timestamp, true);
            }
            else if (m_iCurrentPosition < m_iSelStart || m_iCurrentPosition > m_iSelEnd)
            {
                m_iFramesToDecode = 1;
                if (m_iCurrentPosition < m_iSelStart)
                    ShowNextFrame(m_iSelStart, true);
                else
                    ShowNextFrame(m_iSelEnd, true);
            }

            UpdatePositionUI();
        }
        private void UpdateSelectionLabels()
        {
            long start = 0;
            long duration = 0;

            if (m_FrameServer.Loaded)
            {
                start = m_iSelStart - m_iStartingPosition;
                duration = m_iSelDuration;
            }

            string startTimecode = m_FrameServer.TimeStampsToTimecode(start, TimeType.Absolute, PreferencesManager.PlayerPreferences.TimecodeFormat, true);
            lblSelStartSelection.Text = "◢ " + startTimecode;

            duration -= m_FrameServer.Metadata.AverageTimeStampsPerFrame;
            string durationTimecode = m_FrameServer.TimeStampsToTimecode(duration, TimeType.Duration, PreferencesManager.PlayerPreferences.TimecodeFormat, true);
            int right = lblSelDuration.Right;
            lblSelDuration.Text = "[" + durationTimecode + "]";
            lblSelDuration.Left = right - lblSelDuration.Width;

        }
        private void UpdateSelectionDataFromControl()
        {
            // Update WorkingZone data according to control.
            if ((m_iSelStart != trkSelection.SelStart) || (m_iSelEnd != trkSelection.SelEnd))
            {
                // Time origin: we try to maintain user-defined time origin, but we don't want the origin to stay at the absolute zero when the zone changes.
                // Check if we were previously aligned with the start of the zone, if so, keep it that way, otherwise keep the absolute value.
                if (m_FrameServer.Metadata.TimeOrigin == m_iSelStart)
                    m_FrameServer.Metadata.TimeOrigin = trkSelection.SelStart;

                m_iSelStart = trkSelection.SelStart;
                m_iSelEnd = trkSelection.SelEnd;
                m_iSelDuration = m_iSelEnd - m_iSelStart + m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame;
            }
        }
        private void AfterSelectionChanged()
        {
            // Update everything as if we moved the handlers manually.
            m_FrameServer.Metadata.SelectionStart = m_iSelStart;
            m_FrameServer.Metadata.SelectionEnd = m_iSelEnd;

            UpdateFramesMarkers();

            OnPoke();
            OnSelectionChanged(true);

            // Update current image and keyframe  status.
            UpdateFramePrimarySelection();
            EnableDisableKeyframes();
            ActivateKeyframe(m_iCurrentPosition);
        }
        #endregion

        #region Frame Tracker
        private void trkFrame_PositionChanging(object sender, TimeEventArgs e)
        {
            if (!PreferencesManager.PlayerPreferences.InteractiveFrameTracker)
                return;

            if (m_FrameServer.Loaded)
            {
                // Update image but do not touch cursor, as the user is manipulating it.
                // If the position needs to be adjusted to an actual timestamp, it'll be done later.
                StopPlaying();
                UpdateFrameCurrentPosition(false);
                UpdateCurrentPositionLabel();
                lblTimeTip.Visible = true;

                ActivateKeyframe(m_iCurrentPosition);
            }
        }
        private void trkFrame_PositionChanged(object sender, TimeEventArgs e)
        {
            if (m_FrameServer.Loaded)
            {
                OnPoke();

                m_iCurrentPosition = trkFrame.Position;

                StopPlaying();
                OnPauseAsked();

                // Update image and cursor.
                UpdateFrameCurrentPosition(true);
                UpdateCurrentPositionLabel();
                lblTimeTip.Visible = false;
                ActivateKeyframe(m_iCurrentPosition);

                // Update WorkingZone hairline.
                trkSelection.SelPos = m_iCurrentPosition;
                trkSelection.Invalidate();
            }
        }
        private void trkFrame_KeyframeDropped(object sender, EventArgs e)
        {
            // A keyframe was dropped on the frame timeline.
            // By this point we should be on the target time.
            // This is now similar to the "move keyframe here" action.
            KeyframeControl_MoveToCurrentTimeAsked(sender, e);
        }
        private void UpdateFrameCurrentPosition(bool _bUpdateNavCursor)
        {
            // Displays the image corresponding to the current position within working zone.
            // Trigerred by user (or first load). i.e: cursor moved, show frame.
            if (m_FrameServer.VideoReader.DecodingMode != VideoDecodingMode.Caching)
                this.Cursor = Cursors.WaitCursor;

            m_iCurrentPosition = trkFrame.Position;
            m_iFramesToDecode = 1;
            ShowNextFrame(m_iCurrentPosition, true);

            // The following may readjust the cursor in case the mouse wasn't on a valid timestamp value.
            if (_bUpdateNavCursor)
                UpdatePositionUI();

            if (m_FrameServer.VideoReader.DecodingMode != VideoDecodingMode.Caching)
                this.Cursor = Cursors.Default;
        }
        private void UpdateCurrentPositionLabel()
        {
            // Note: among other places, this is run inside the playloop.
            string timecode = m_FrameServer.TimeStampsToTimecode(m_iCurrentPosition, TimeType.UserOrigin, PreferencesManager.PlayerPreferences.TimecodeFormat, true);
            lblTimeCode.Text = "▼ " + timecode;
            lblTimeTip.Text = timecode;
            lblTimeTip.Left = trkFrame.PixelPosition;
        }
        private void UpdatePositionUI()
        {
            // Update markers and label for position.
            if (PreferencesManager.PlayerPreferences.ShowCacheInTimeline)
            {
                VideoSection section;
                if (m_FrameServer.VideoReader.DecodingMode == VideoDecodingMode.Caching)
                    section = new VideoSection(m_iSelStart, m_iSelEnd);
                else if (m_FrameServer.VideoReader.DecodingMode == VideoDecodingMode.PreBuffering)
                    section = m_FrameServer.VideoReader.PreBufferingSegment;
                else
                    section = new VideoSection(m_iCurrentPosition, m_iCurrentPosition);
                
                trkFrame.UpdateCacheSegmentMarker(section);
            }

            trkFrame.Position = m_iCurrentPosition;
            trkFrame.Invalidate();
            trkSelection.SelPos = m_iCurrentPosition;
            trkSelection.Invalidate();
            UpdateCurrentPositionLabel();
        }

        private void PanelVideoControls_DragDrop(object sender, DragEventArgs e)
        {
            // Dropping a keyframe somewhere in the bottom part.
            trkFrame.Commit();
            
            // Handle the drop.
            object keyframeBox = e.Data.GetData(typeof(KeyframeBox));
            if (keyframeBox != null && keyframeBox is KeyframeBox)
            {
                KeyframeControl_MoveToCurrentTimeAsked(keyframeBox, EventArgs.Empty);
            }
        }

        private void PanelVideoControls_DragOver(object sender, DragEventArgs e)
        {
            // Dragging a keyframe anywhere on the video controls panel.
            // We turn the whole panel into a timeline.
            e.Effect = DragDropEffects.Move;
            trkFrame.Scrub();
        }
        #endregion

        #region Speed Slider
        private void sldrSpeed_ValueChanged(object sender, EventArgs e)
        {
            slowMotion = timeMapper.GetSlowMotion(sldrSpeed.Value);

            if (m_FrameServer.Loaded)
            {
                // Reset timer with new value.
                if (m_bIsCurrentlyPlaying)
                {
                    StopMultimediaTimer();
                    StartMultimediaTimer(GetPlaybackFrameInterval());
                }

                if (SpeedChanged != null)
                    SpeedChanged(this, EventArgs.Empty);
            }

            UpdateSpeedLabel();
        }
        private void ChangeSpeed(int change)
        {
            // The value is a target diff percentage.
            // Ex: we are on 86%, value = -25, the target is 75%.

            if (change == 0)
                return;

            sldrSpeed.StepJump(change / 200.0);
        }
        private void lblSpeedTuner_DoubleClick(object sender, EventArgs e)
        {
            slowMotion = 1;
            sldrSpeed.Force(timeMapper.GetInputFromSlowMotion(slowMotion));
        }
        private void UpdateSpeedLabel()
        {
            double multiplier = timeMapper.GetRealtimeMultiplier(sldrSpeed.Value);
            string speedValue = "";

            if (multiplier < 1.0)
                speedValue = string.Format("{0:0.##}%", multiplier * 100);
            else
                speedValue = string.Format("{0:0.##}x", multiplier);

            lblSpeedTuner.Text = speedValue;
        }
        #endregion

        #endregion

        #region Auto Stretch & Manual Resize
        private void StretchSqueezeSurface(bool finished)
        {
            // Compute the rendering size, and the corresponding optimal decoding size.
            // We don't ask the VideoReader to update its decoding size here.
            // (We might want to wait the end of a resizing process for example.).
            // Similarly, we don't update the rendering zoom factor, so that during resizing process,
            // the zoom window is still computed based on the current decoding size.
            if (!m_FrameServer.Loaded)
                return;

            double targetStretch = m_FrameServer.ImageTransform.Stretch;

            // If we have been forced to a different stretch (due to application resizing or minimizing), 
            // make sure we aim for the user's last requested value.
            if (!m_fill && m_lastUserStretch != m_viewportManipulator.Stretch)
                targetStretch = m_lastUserStretch;

            // Stretch factor, zoom, or container size have been updated, update the rendering and decoding sizes.
            // During the process, stretch and fill may be forced to different values.
            // Custom scaling vs decoding modes:
            // - We try to decode the images at the smallest size possible.
            // - Some states of the applications like tracking prevent this, this is stored in m_bEnableCustomDecodingSize.
            // - Some decoding modes also prevent changing the decoding size, this is set in scalable here.
            // Note: do not update decoding scale here, as this function is called during stretching of the rendering surface, 
            // while the decoding size isn't updated. 
            
            // TODO: move this to a function on video readers.
            bool scalable = m_FrameServer.VideoReader.CanScaleIndefinitely || m_FrameServer.VideoReader.DecodingMode == VideoDecodingMode.PreBuffering;
            bool canCustomDecodingSize = m_bEnableCustomDecodingSize && scalable;

            bool rotatedCanvas = false;
            if (videoFilterIsActive)
                rotatedCanvas = m_FrameServer.Metadata.ActiveVideoFilter.RotatedCanvas;

            m_viewportManipulator.Manipulate(finished, panelCenter.Size, targetStretch, m_fill, m_FrameServer.ImageTransform.Zoom, canCustomDecodingSize, rotatedCanvas);
            
            pbSurfaceScreen.Location = m_viewportManipulator.RenderingLocation;
            pbSurfaceScreen.Size = m_viewportManipulator.RenderingSize;
            m_FrameServer.ImageTransform.Stretch = m_viewportManipulator.Stretch;
            ReplaceResizers();
        }
        private void ReplaceResizers()
        {
            ImageResizerSE.Left = pbSurfaceScreen.Right - (ImageResizerSE.Width / 2);
            ImageResizerSE.Top = pbSurfaceScreen.Bottom - (ImageResizerSE.Height / 2);

            ImageResizerSW.Left = pbSurfaceScreen.Left - (ImageResizerSW.Width / 2);
            ImageResizerSW.Top = pbSurfaceScreen.Bottom - (ImageResizerSW.Height / 2);

            ImageResizerNE.Left = pbSurfaceScreen.Right - (ImageResizerNE.Width / 2);
            ImageResizerNE.Top = pbSurfaceScreen.Top - (ImageResizerNE.Height / 2);

            ImageResizerNW.Left = pbSurfaceScreen.Left - ImageResizerNW.Width / 2;
            ImageResizerNW.Top = pbSurfaceScreen.Top - ImageResizerNW.Height / 2;
        }
        private void ToggleImageFillMode()
        {
            if (!m_fill)
            {
                m_fill = true;
            }
            else
            {
                // If the image doesn't fit in the container, we stay in fill mode.
                if (m_FrameServer.ImageTransform.Stretch >= 1)
                {
                    m_FrameServer.ImageTransform.Stretch = 1;
                    m_fill = false;
                }
            }

            ResetZoom(false);
            ResizeUpdate(true);
        }
        private void ImageResizerSE_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            int iTargetHeight = (ImageResizerSE.Top - pbSurfaceScreen.Top + e.Y);
            int iTargetWidth = (ImageResizerSE.Left - pbSurfaceScreen.Left + e.X);
            ManualResizeImage(iTargetWidth, iTargetHeight);
        }
        private void ImageResizerSW_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int iTargetHeight = (ImageResizerSW.Top - pbSurfaceScreen.Top + e.Y);
                int iTargetWidth = pbSurfaceScreen.Width + (pbSurfaceScreen.Left - (ImageResizerSW.Left + e.X));
                ManualResizeImage(iTargetWidth, iTargetHeight);
            }
        }
        private void ImageResizerNW_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int iTargetHeight = pbSurfaceScreen.Height + (pbSurfaceScreen.Top - (ImageResizerNW.Top + e.Y));
                int iTargetWidth = pbSurfaceScreen.Width + (pbSurfaceScreen.Left - (ImageResizerNW.Left + e.X));
                ManualResizeImage(iTargetWidth, iTargetHeight);
            }
        }
        private void ImageResizerNE_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int iTargetHeight = pbSurfaceScreen.Height + (pbSurfaceScreen.Top - (ImageResizerNE.Top + e.Y));
                int iTargetWidth = (ImageResizerNE.Left - pbSurfaceScreen.Left + e.X);
                ManualResizeImage(iTargetWidth, iTargetHeight);
            }
        }
        private void ManualResizeImage(int _iTargetWidth, int _iTargetHeight)
        {
            Size targetSize = new Size(_iTargetWidth, _iTargetHeight);
            if (!targetSize.FitsIn(panelCenter.Size))
                return;

            if (!m_bManualSqueeze && !m_FrameServer.VideoReader.Info.ReferenceSize.FitsIn(targetSize))
                return;

            // Area of the original size is sticky on the inside.
            if (!m_FrameServer.VideoReader.Info.ReferenceSize.FitsIn(targetSize) &&
               (m_FrameServer.VideoReader.Info.ReferenceSize.Width - _iTargetWidth < 40 &&
                m_FrameServer.VideoReader.Info.ReferenceSize.Height - _iTargetHeight < 40))
            {
                _iTargetWidth = m_FrameServer.VideoReader.Info.ReferenceSize.Width;
                _iTargetHeight = m_FrameServer.VideoReader.Info.ReferenceSize.Height;
            }

            if (!m_MinimalSize.FitsIn(targetSize))
                return;

            double fHeightFactor = ((_iTargetHeight) / (double)m_FrameServer.VideoReader.Info.ReferenceSize.Height);
            double fWidthFactor = ((_iTargetWidth) / (double)m_FrameServer.VideoReader.Info.ReferenceSize.Width);

            m_FrameServer.ImageTransform.Stretch = (fWidthFactor + fHeightFactor) / 2;
            m_fill = false;
            m_lastUserStretch = m_FrameServer.ImageTransform.Stretch;

            ResizeUpdate(false);
        }
        private void Resizers_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ToggleImageFillMode();
        }
        private void Resizers_MouseUp(object sender, MouseEventArgs e)
        {
            ResizeUpdate(true);
        }
        private void ResizeUpdate(bool finished)
        {
            if (!m_FrameServer.Loaded)
                return;

            StretchSqueezeSurface(finished);

            if (finished)
            {
                // Update the decoding size at the file reader level. 
                // This may clear and restart the prebuffering.
                // It may not be honored by the video reader.
                if (m_FrameServer.VideoReader.CanChangeDecodingSize)
                {
                    bool accepted = m_FrameServer.VideoReader.ChangeDecodingSize(m_viewportManipulator.PreferredDecodingSize);
                    if (accepted)
                        m_FrameServer.ImageTransform.DecodingScale = m_viewportManipulator.PreferredDecodingScale;
                }
                m_FrameServer.Metadata.ResizeFinished();
                RefreshImage();
            }
            else
            {
                DoInvalidate();
            }
        }
        private void CheckCustomDecodingSize(bool _forceDisable)
        {
            // Enable or disable custom decoding size depending on current state.
            // Custom decoding size is not compatible with tracking.
            // The boolean will later be used each time we attempt to change decoding size in StretchSqueezeSurface.
            // This is not concerned with decoding mode (prebuffering, caching, etc.) as this will be checked inside the reader.
            bool wasEnabled = m_bEnableCustomDecodingSize;
            m_bEnableCustomDecodingSize = !_forceDisable && !m_FrameServer.Metadata.Tracking;

            if (wasEnabled && !m_bEnableCustomDecodingSize)
            {
                m_FrameServer.VideoReader.DisableCustomDecodingSize();
                ResizeUpdate(true);
            }
            else if (!wasEnabled && m_bEnableCustomDecodingSize)
            {
                ResizeUpdate(true);
            }
        }
        #endregion

        #region Timers & Playloop
        private void StartMultimediaTimer(int _interval)
        {
            //log.DebugFormat("starting playback timer at {0} ms interval.", _interval);
            ActivateKeyframe(-1);
            m_DropWatcher.Restart();
            m_LoopWatcher.Restart();

            Application.Idle += Application_Idle;
            m_FrameServer.VideoReader.BeforePlayloop();
            m_FrameServer.Metadata.PauseAutosave();

            uint eventType = NativeMethods.TIME_PERIODIC | NativeMethods.TIME_KILL_SYNCHRONOUS;
            m_IdMultimediaTimer = NativeMethods.timeSetEvent((uint)_interval, (uint)_interval, m_TimerCallback, UIntPtr.Zero, eventType);
            m_bIsCurrentlyPlaying = true;
        }
        private void StopMultimediaTimer()
        {
            if (m_IdMultimediaTimer != 0)
                NativeMethods.timeKillEvent(m_IdMultimediaTimer);

            m_IdMultimediaTimer = 0;
            m_bIsCurrentlyPlaying = false;
            Application.Idle -= Application_Idle;
            m_FrameServer.Metadata.UnpauseAutosave();

            log.DebugFormat("Playback paused. Avg frame time: {0:0.000} ms. Drop ratio: {1:0.00}", m_LoopWatcher.Average, m_DropWatcher.Ratio);
        }
        private void MultimediaTimer_Tick(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2)
        {
            if (!m_FrameServer.Loaded)
                return;

            // We cannot change the pointer to current here in case the UI is painting it,
            // so we will pass the number of drops along to the rendering.
            // The rendering will then ask for an update of the pointer to current, skipping as
            // many frames we missed during the interval.
            lock (m_TimingSync)
            {
                if (!m_bIsBusyRendering)
                {
                    int drops = m_RenderingDrops;
                    BeginInvoke((Action)delegate { Rendering_Invoked(drops); });
                    m_bIsBusyRendering = true;
                    m_RenderingDrops = 0;
                    m_DropWatcher.AddDropStatus(false);
                }
                else
                {
                    m_RenderingDrops++;
                    m_DropWatcher.AddDropStatus(true);
                }
            }
        }
        private void Rendering_Invoked(int missedFrames)
        {
            // This is in UI thread space.
            // Rendering in the context of continuous playback (play loop).
            m_TimeWatcher.Restart();

            bool tracking = m_FrameServer.Metadata.Tracking;
            int skip = tracking ? 0 : missedFrames;

            long estimateNext = m_iCurrentPosition + ((skip + 1) * m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame);

            if (estimateNext > m_iSelEnd)
            {
                EndOfFile();
            }
            else
            {
                long oldPosition = m_iCurrentPosition;

                // This may be slow (several ms) due to delete call when dequeuing the pre-buffer. To investigate.
                m_FrameServer.VideoReader.MoveNext(skip, false);

                // In case the frame wasn't available in the pre-buffer, don't render anything.
                // This means if we missed the previous frame because the UI was busy, we won't 
                // render it now either. On the other hand, it means we will have less chance to
                // miss the next frame while trying to render an already outdated one.
                // We must also "unreset" the rendering drop counter, since we didn't actually render the frame.
                if (m_FrameServer.VideoReader.Drops > 0)
                {
                    if (m_FrameServer.VideoReader.Drops > m_MaxDecodingDrops)
                    {
                        log.DebugFormat("Failsafe triggered on Decoding Drops ({0})", m_FrameServer.VideoReader.Drops);
                        ForceSlowdown();
                    }
                    else
                    {
                        lock (m_TimingSync)
                            m_RenderingDrops = missedFrames;
                    }
                }
                else if (m_FrameServer.VideoReader.Current != null)
                {
                    if (videoFilterIsActive)
                        m_FrameServer.Metadata.ActiveVideoFilter.UpdateTime(m_FrameServer.VideoReader.Current.Timestamp);
                    
                    DoInvalidate();
                    m_iCurrentPosition = m_FrameServer.VideoReader.Current.Timestamp;

                    TrackDrawingsCommand.Execute(null);
                    ComputeOrStopTracking(skip == 0);

                    // This causes Invalidates and will postpone the idle event.
                    // Update UI. For speed purposes, we don't update Selection Tracker hairline.
                    trkFrame.Position = m_iCurrentPosition;
                    trkFrame.UpdateCacheSegmentMarker(m_FrameServer.VideoReader.PreBufferingSegment);
                    trkFrame.Invalidate();
                    UpdateCurrentPositionLabel();

                    ReportForSyncMerge();
                }

                if (m_iCurrentPosition < oldPosition && m_bSynched)
                {
                    // Sometimes the test to preemptively detect the end of file won't work.
                    StopPlaying();
                    ShowNextFrame(m_iSelStart, true);
                    UpdatePositionUI();
                    m_iFramesToDecode = 1;
                }
            }
        }
        private void EndOfFile()
        {
            m_FrameServer.Metadata.StopAllTracking();

            if (m_bSynched)
            {
                StopPlaying();
                ShowNextFrame(m_iSelStart, true);
            }
            else if (m_ePlayingMode == PlayingMode.Loop)
            {
                StopMultimediaTimer();
                bool rewound = ShowNextFrame(m_iSelStart, true);

                if (rewound)
                    StartMultimediaTimer(GetPlaybackFrameInterval());
                else
                    StopPlaying();
            }
            else
            {
                StopPlaying();
            }

            UpdatePositionUI();
            m_iFramesToDecode = 1;
        }
        private void ForceSlowdown()
        {
            m_FrameServer.VideoReader.ResetDrops();
            m_iFramesToDecode = 0;
            sldrSpeed.StepJump(-0.05);
        }
        private void ComputeOrStopTracking(bool _contiguous)
        {
            if (!m_FrameServer.Metadata.Tracking)
                return;

            // Fixme: Tracking only supports contiguous frames,
            // but this should be the responsibility of the track tool anyway.
            if (!_contiguous)
                m_FrameServer.Metadata.StopAllTracking();
            else
                m_FrameServer.Metadata.PerformTracking(m_FrameServer.VideoReader.Current);

            UpdateFramesMarkers();
            CheckCustomDecodingSize(false);
        }
        private void Application_Idle(object sender, EventArgs e)
        {
            // This event fires when the window has consumed all its messages.
            // Forcing the rendering to synchronize with this event allows
            // the UI to have a chance to process non-rendering related events like
            // button clicks, mouse move, etc.
            lock (m_TimingSync)
                m_bIsBusyRendering = false;

            m_TimeWatcher.LogTime("Back to idleness");
            //m_TimeWatcher.DumpTimes();
            m_LoopWatcher.AddLoopTime(m_TimeWatcher.RawTime("Back to idleness"));
        }
        private bool ShowNextFrame(long _iSeekTarget, bool _bAllowUIUpdate)
        {
            if (!m_FrameServer.VideoReader.Loaded)
                return false;

            // TODO: More refactoring needed.
            // Eradicate the scheme where we use the _iSeekTarget parameter to mean two things.
            if (m_bIsCurrentlyPlaying)
                throw new ThreadStateException("ShowNextFrame called while play loop.");

            bool refreshInPlace = _iSeekTarget == m_iCurrentPosition;
            bool hasMore = false;

            if (_iSeekTarget < 0)
            {
                hasMore = m_FrameServer.VideoReader.MoveBy(m_iFramesToDecode, true);
            }
            else
            {
                hasMore = m_FrameServer.VideoReader.MoveTo(m_iCurrentPosition, _iSeekTarget);
            }

            if (m_FrameServer.VideoReader.Current != null)
            {
                if (videoFilterIsActive)
                    m_FrameServer.Metadata.ActiveVideoFilter.UpdateTime(m_FrameServer.VideoReader.Current.Timestamp);
                
                m_iCurrentPosition = m_FrameServer.VideoReader.Current.Timestamp;

                TrackDrawingsCommand.Execute(null);

                bool contiguous = _iSeekTarget < 0 && m_iFramesToDecode <= 1;
                if (!refreshInPlace)
                    ComputeOrStopTracking(contiguous);

                if (_bAllowUIUpdate)
                    DoInvalidate();

                ReportForSyncMerge();
            }

            if (!hasMore)
            {
                // End of working zone reached.
                m_iCurrentPosition = m_iSelEnd;
                if (_bAllowUIUpdate)
                {
                    trkSelection.SelPos = m_iCurrentPosition;
                    DoInvalidate();
                }

                m_FrameServer.Metadata.StopAllTracking();
            }

            return hasMore;
        }
        private void StopPlaying(bool _bAllowUIUpdate)
        {
            if (!m_FrameServer.Loaded || !m_bIsCurrentlyPlaying)
                return;

            StopMultimediaTimer();

            lock (m_TimingSync)
            {
                m_bIsBusyRendering = false;
                m_RenderingDrops = 0;
            }

            m_iFramesToDecode = 0;

            if (_bAllowUIUpdate)
            {
                buttonPlay.Image = Player.flatplay;
                DoInvalidate();
                UpdatePositionUI();
            }
        }
        private int GetPlaybackFrameInterval()
        {
            return (int)Math.Round(timeMapper.GetInterval(sldrSpeed.Value));
        }
        private void DeselectionTimer_OnTick(object sender, EventArgs e)
        {
            if (m_FrameServer.Metadata.TextEditingInProgress)
            {
                // Ignore the timer if we are editing text, so we don't close the text editor under the user.
                m_DeselectionTimer.Stop();
                return;
            }

            // Deselect the currently selected drawing.
            // This is used for drawings that must show extra stuff for being transformed, but we 
            // don't want to show the extra stuff all the time for clarity.
            m_FrameServer.Metadata.DeselectAll();
            m_DeselectionTimer.Stop();
            DoInvalidate();
            OnPoke();
        }
        #endregion

        #region Culture
        private void ReloadMenusCulture()
        {
            // Reload the text for each menu.
            // this is done at construction time and at RefreshUICulture time.

            // Background context menu.
            mnuTimeOrigin.Text = ScreenManagerLang.mnuMarkTimeAsOrigin;
            mnuDirectTrack.Text = ScreenManagerLang.mnuTrackTrajectory;
            mnuPasteDrawing.Text = ScreenManagerLang.mnuPasteDrawing;
            mnuPasteDrawing.ShortcutKeys = HotkeySettingsManager.GetMenuShortcut("PlayerScreen", (int)PlayerScreenCommands.PasteDrawing);
            mnuOpenVideo.Text = ScreenManagerLang.mnuOpenVideo;
            mnuOpenReplayWatcher.Text = ScreenManagerLang.mnuOpenReplayWatcher;
            mnuOpenAnnotations.Text = ScreenManagerLang.mnuLoadAnalysis;
            mnuSaveAnnotations.Text = ScreenManagerLang.Generic_SaveKVA;
            mnuSaveAnnotationsAs.Text = ScreenManagerLang.Generic_SaveKVAAs;
            mnuExportVideo.Text = ScreenManagerLang.Generic_ExportVideo;
            mnuExportImage.Text = ScreenManagerLang.Generic_SaveImage;
            mnuCopyPic.Text = ScreenManagerLang.mnuCopyImageToClipboard;
            mnuCopyPic.ShortcutKeys = HotkeySettingsManager.GetMenuShortcut("PlayerScreen", (int)PlayerScreenCommands.CopyImage);
            mnuPastePic.Text = ScreenManagerLang.mnuPasteImage;
            mnuCloseScreen.Text = ScreenManagerLang.mnuCloseScreen;
            mnuCloseScreen.ShortcutKeys = HotkeySettingsManager.GetMenuShortcut("PlayerScreen", (int)PlayerScreenCommands.Close);

            // Drawings context menu.
            mnuConfigureDrawing.Text = ScreenManagerLang.Generic_ConfigurationElipsis;
            mnuSetStyleAsDefault.Text = ScreenManagerLang.mnuSetStyleAsDefault;
            mnuVisibility.Text = ScreenManagerLang.Generic_Visibility;
            mnuVisibilityAlways.Text = ScreenManagerLang.dlgConfigureFading_chkAlwaysVisible;
            mnuVisibilityDefault.Text = ScreenManagerLang.mnuVisibilityDefault;
            mnuVisibilityCustom.Text = ScreenManagerLang.mnuVisibilityCustom;
            mnuVisibilityConfigure.Text = ScreenManagerLang.mnuVisibilityConfigure;
            mnuGotoKeyframe.Text = ScreenManagerLang.mnuGotoKeyframe;
            mnuCutDrawing.Text = ScreenManagerLang.Generic_Cut;
            mnuCutDrawing.ShortcutKeys = HotkeySettingsManager.GetMenuShortcut("PlayerScreen", (int)PlayerScreenCommands.CutDrawing);
            mnuCopyDrawing.Text = ScreenManagerLang.Generic_Copy;
            mnuCopyDrawing.ShortcutKeys = HotkeySettingsManager.GetMenuShortcut("PlayerScreen", (int)PlayerScreenCommands.CopyDrawing);
            mnuDeleteDrawing.Text = ScreenManagerLang.mnuDeleteDrawing;
            mnuDeleteDrawing.ShortcutKeys = HotkeySettingsManager.GetMenuShortcut("PlayerScreen", (int)PlayerScreenCommands.DeleteDrawing);

            mnuDrawingTracking.Text = ScreenManagerLang.dlgConfigureTrajectory_Tracking;
            mnuDrawingTrackingConfigure.Text = ScreenManagerLang.Generic_ConfigurationElipsis;
            mnuDrawingTrackingStart.Text = ScreenManagerLang.mnuDrawingTrackingStart;
            mnuDrawingTrackingStop.Text = ScreenManagerLang.mnuDrawingTrackingStop;

            // Tracking pop menu (Restart, Stop tracking)
            mnuConfigureTrajectory.Text = ScreenManagerLang.Generic_ConfigurationElipsis;
            mnuDeleteTrajectory.Text = ScreenManagerLang.mnuDeleteDrawing;
            mnuDeleteTrajectory.ShortcutKeys = HotkeySettingsManager.GetMenuShortcut("PlayerScreen", (int)PlayerScreenCommands.DeleteDrawing);

            // Magnifier.
            mnuMagnifierFreeze.Text = "Freeze";
            mnuMagnifierTrack.Text = ScreenManagerLang.mnuTrackTrajectory;
            mnuMagnifierDirect.Text = ScreenManagerLang.mnuMagnifierDirect;
            mnuMagnifierQuit.Text = ScreenManagerLang.mnuMagnifierQuit;
        }


        private void ReloadTooltipsCulture()
        {
            // Video controls
            toolTips.SetToolTip(buttonPlay, ScreenManagerLang.Generic_PlayPause);
            toolTips.SetToolTip(buttonGotoPrevious, ScreenManagerLang.ToolTip_Back);
            toolTips.SetToolTip(buttonGotoNext, ScreenManagerLang.ToolTip_Next);
            toolTips.SetToolTip(buttonGotoFirst, ScreenManagerLang.ToolTip_First);
            toolTips.SetToolTip(buttonGotoLast, ScreenManagerLang.ToolTip_Last);

            // Export buttons
            toolTips.SetToolTip(btnSnapShot, ScreenManagerLang.Generic_SaveImage);
            toolTips.SetToolTip(btnRafale, ScreenManagerLang.ToolTip_Rafale);
            toolTips.SetToolTip(btnDiaporama, ScreenManagerLang.ToolTip_SaveDiaporama);
            toolTips.SetToolTip(btnSaveVideo, ScreenManagerLang.CommandExportVideo_FriendlyName);
            toolTips.SetToolTip(btnPausedVideo, ScreenManagerLang.ToolTip_SavePausedVideo);

            // Working zone and sliders.
            if (m_bHandlersLocked)
            {
                toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionUnlock);
            }
            else
            {
                toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionLock);
            }
            toolTips.SetToolTip(btnSetHandlerLeft, ScreenManagerLang.ToolTip_SetHandlerLeft);
            toolTips.SetToolTip(btnSetHandlerRight, ScreenManagerLang.ToolTip_SetHandlerRight);
            toolTips.SetToolTip(btnHandlersReset, ScreenManagerLang.ToolTip_ResetWorkingZone);
            trkSelection.ToolTip = ScreenManagerLang.ToolTip_trkSelection;

            toolTips.SetToolTip(btnTimeOrigin, ScreenManagerLang.mnuMarkTimeAsOrigin);

            toolTips.SetToolTip(lblTimeCode, ScreenManagerLang.lblTimeCode_Text);
            toolTips.SetToolTip(lblSpeedTuner, "Speed");
            toolTips.SetToolTip(sldrSpeed, "Speed");
            toolTips.SetToolTip(lblSelStartSelection, ScreenManagerLang.lblSelStartSelection_Text);
            toolTips.SetToolTip(lblSelDuration, ScreenManagerLang.lblSelDuration_Text);
        }
        private void ReloadToolsCulture()
        {
            foreach (ToolStripItem tsi in stripDrawingTools.Items)
            {
                if (tsi is ToolStripSeparator)
                    continue;

                if (tsi is ToolStripButtonWithDropDown)
                {
                    foreach (ToolStripItem subItem in ((ToolStripButtonWithDropDown)tsi).DropDownItems)
                    {
                        if (!(subItem is ToolStripMenuItem))
                            continue;

                        AbstractDrawingTool tool = subItem.Tag as AbstractDrawingTool;
                        if (tool != null)
                        {
                            subItem.Text = tool.DisplayName;
                            subItem.ToolTipText = tool.DisplayName;
                        }
                    }

                    ((ToolStripButtonWithDropDown)tsi).UpdateToolTip();
                }
                else if (tsi is ToolStripButton)
                {
                    AbstractDrawingTool tool = tsi.Tag as AbstractDrawingTool;
                    if (tool != null)
                        tsi.ToolTipText = tool.DisplayName;
                }
            }

            m_btnAddKeyFrame.ToolTipText = ScreenManagerLang.ToolTip_AddKeyframe;
            m_btnShowComments.ToolTipText = ScreenManagerLang.ToolTip_ShowComments;
            m_btnToolPresets.ToolTipText = ScreenManagerLang.ToolTip_ColorProfile;
        }
        #endregion

        #region SurfaceScreen Events
        private void SurfaceScreen_MouseDown(object sender, MouseEventArgs e)
        {
            RaiseSetAsActiveScreenEvent();
            
            if (!m_FrameServer.Loaded)
                return;

            m_DeselectionTimer.Stop();
            m_DescaledMouse = m_FrameServer.ImageTransform.Untransform(e.Location);

            if (e.Button == MouseButtons.Left)
                SurfaceScreen_LeftDown();
            else if (e.Button == MouseButtons.Middle)
                SurfaceScreen_MiddleDown();
            else if (e.Button == MouseButtons.Right)
                SurfaceScreen_RightDown();

            DoInvalidate();
        }
        private void SurfaceScreen_LeftDown()
        {
            if (m_bIsCurrentlyPlaying)
            {
                // MouseDown while playing: pause the video.
                StopPlaying();
                OnPauseAsked();
                ActivateKeyframe(m_iCurrentPosition);
            }

            m_FrameServer.Metadata.AllDrawingTextToNormalMode();

            if (m_ActiveTool == m_PointerTool)
            {
                HandToolDown();
            }
            else if (m_ActiveTool == ToolManager.Tools["Spotlight"])
            {
                CreateNewMultiDrawingItem(m_FrameServer.Metadata.DrawingSpotlight);
            }
            else if (m_ActiveTool == ToolManager.Tools["NumberSequence"])
            {
                CreateNewMultiDrawingItem(m_FrameServer.Metadata.DrawingNumberSequence);
            }
            else if (m_ActiveTool == ToolManager.Tools["Chrono"] || 
                m_ActiveTool == ToolManager.Tools["Clock"] || 
                m_ActiveTool == ToolManager.Tools["ChronoMulti"])
            {
                CreateNewDrawing(m_FrameServer.Metadata.ChronoManager.Id);
            }
            else
            {
                // Note: if the active drawing is at initialization stage, it will receive the point commit during mouse up.
                if (!m_FrameServer.Metadata.DrawingInitializing)
                {
                    AddKeyframe();
                    if (m_iActiveKeyFrameIndex >= 0)
                        CreateNewDrawing(m_FrameServer.Metadata.GetKeyframeId(m_iActiveKeyFrameIndex));
                }
            }
        }

        private void SurfaceScreen_MiddleDown()
        {
            // Middle mouse button is a shortcut to temporary use the hand tool, disregarding the selected tool.
            // It should provide exactly the same interaction mechanics as if we were using Left mouse button with hand tool selected.

            if (m_bIsCurrentlyPlaying)
            {
                // MouseDown while playing: Halt the video.
                StopPlaying();
                OnPauseAsked();
                ActivateKeyframe(m_iCurrentPosition);
            }

            HandToolDown();
        }

        private void HandToolDown()
        {
            m_PointerTool.OnMouseDown(m_FrameServer.Metadata, m_iActiveKeyFrameIndex, m_DescaledMouse, m_iCurrentPosition, PreferencesManager.PlayerPreferences.DefaultFading.Enabled);

            if (m_FrameServer.Metadata.HitDrawing != null)
            {
                SetCursor(cursorManager.GetManipulationCursor(m_FrameServer.Metadata.HitDrawing));
            }
            else
            {
                SetCursor(m_PointerTool.GetCursor(1));

                bool hitMagnifier = false;
                if (m_FrameServer.Metadata.Magnifier.Mode == MagnifierMode.Active)
                {
                    hitMagnifier = m_FrameServer.Metadata.Magnifier.OnMouseDown(m_DescaledMouse, m_FrameServer.Metadata.ImageTransform);
                }

                if (!hitMagnifier)
                {
                    if (videoFilterIsActive)
                        m_FrameServer.Metadata.ActiveVideoFilter.StartMove(m_DescaledMouse);
                }
            }
        }

        private void CreateNewDrawing(Guid managerId)
        {
            m_FrameServer.Metadata.DeselectAll();

            IImageToViewportTransformer transformer = m_FrameServer.Metadata.ImageTransform;
            bool zooming = m_FrameServer.Metadata.ImageTransform.Zooming;
            DistortionHelper distorter = m_FrameServer.Metadata.CalibrationHelper.DistortionHelper;

            // Special case for the text tool: if we hit on another label we go into edit mode instead of adding a new one on top of it.
            bool editingLabel = false;
            if (m_ActiveTool == ToolManager.Tools["Label"])
            {
                foreach (DrawingText label in m_FrameServer.Metadata.Labels())
                {
                    int hit = label.HitTest(m_DescaledMouse, m_iCurrentPosition, distorter, transformer, zooming);
                    if (hit < 0)
                        continue;

                    label.SetEditMode(true, m_DescaledMouse, m_FrameServer.ImageTransform);
                    editingLabel = true;
                    break;
                }
            }

            if (!editingLabel)
            {
                AbstractDrawing drawing = m_ActiveTool.GetNewDrawing(m_DescaledMouse, m_iCurrentPosition, m_FrameServer.Metadata.AverageTimeStampsPerFrame, m_FrameServer.Metadata.ImageTransform);
                if (DrawingAdding != null)
                    DrawingAdding(this, new DrawingEventArgs(drawing, managerId));
            }
        }
        private void AfterDrawingAdded(AbstractDrawing drawing)
        {
            if (drawing is DrawingText)
            {
                DrawingText drawingText = drawing as DrawingText;
                drawingText.InitializeText();
                ImportEditbox(drawingText);
            }

            if (drawing is DrawingTrack)
            {
                ((DrawingTrack)drawing).DisplayClosestFrame = DisplayClosestFrame;
                ((DrawingTrack)drawing).CheckCustomDecodingSize = CheckCustomDecodingSize;

                // TODO: move this to a tool.
                m_ActiveTool = m_PointerTool;
                SetCursor(m_PointerTool.GetCursor(0));
            }

            if (!m_FrameServer.Metadata.KVAImporting)
            {
                m_FrameServer.Metadata.UpdateTrackPoint(m_FrameServer.CurrentImage);
                UpdateFramesMarkers();
                RefreshImage();
            }
        }
        private void AfterDrawingModified(AbstractDrawing drawing)
        {
            UpdateFramesMarkers();
            RefreshImage();
        }
        private void AfterVideoFilterModified()
        {
            RefreshImage();
        }
        private void ImportEditboxes()
        {
            // Import edit boxes of all drawing text after a KVA import.
            foreach (DrawingText drawingText in m_FrameServer.Metadata.Labels())
            {
                ImportEditbox(drawingText);
            }
        }
        private void ImportEditbox(DrawingText drawing)
        {
            if (panelCenter.Controls.Contains(drawing.EditBox))
                return;

            drawing.ContainerScreen = pbSurfaceScreen;
            panelCenter.Controls.Add(drawing.EditBox);
            drawing.EditBox.BringToFront();
            drawing.EditBox.Focus();
            drawing.EditBox.Tag = this;
        }
        private void AfterDrawingDeleted()
        {
            if (!m_FrameServer.Metadata.KVAImporting)
            {
                UpdateFramesMarkers();
                RefreshImage();
            }
        }
        private void CreateNewMultiDrawingItem(AbstractMultiDrawing manager)
        {
            m_FrameServer.Metadata.DeselectAll();
            AddKeyframe();

            AbstractMultiDrawingItem item = manager.GetNewItem(m_DescaledMouse, m_iCurrentPosition, m_FrameServer.Metadata.AverageTimeStampsPerFrame);

            if (MultiDrawingItemAdding != null)
                MultiDrawingItemAdding(this, new MultiDrawingItemEventArgs(item, manager));
        }
        private void AfterMultiDrawingItemAdded()
        {
            if (!m_FrameServer.Metadata.KVAImporting)
                RefreshImage();
        }
        private void AfterMultiDrawingItemDeleted()
        {
            if (!m_FrameServer.Metadata.KVAImporting)
                RefreshImage();
        }
        private void SurfaceScreen_RightDown()
        {
            // Show the right Pop Menu depending on context.
            // (Drawing, Trajectory, Chronometer, Magnifier, Nothing)
            if (m_bIsCurrentlyPlaying)
            {
                PrepareBackgroundContextMenu(popMenu);

                mnuTimeOrigin.Enabled = false;
                mnuDirectTrack.Enabled = false;
                mnuPasteDrawing.Enabled = false;
                mnuPastePic.Enabled = false;
                panelCenter.ContextMenuStrip = popMenu;
                return;
            }

            m_FrameServer.Metadata.DeselectAll();
            AbstractDrawing hitDrawing = null;

            if (m_FrameServer.Metadata.IsOnDrawing(m_iActiveKeyFrameIndex, m_DescaledMouse, m_iCurrentPosition))
            {
                AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
                PrepareDrawingContextMenu(drawing, popMenuDrawings);

                popMenuDrawings.Items.Add(mnuDeleteDrawing);
                panelCenter.ContextMenuStrip = popMenuDrawings;
            }
            else if ((hitDrawing = m_FrameServer.Metadata.IsOnDetachedDrawing(m_DescaledMouse, m_iCurrentPosition)) != null)
            {
                // Unlike attached drawings, each extra drawing type has its own context menu for now.
                // TODO: use the custom menus system to host these menus inside the drawing instead of here.
                // Only the drawing itself knows what to do upon click anyway.

                if (hitDrawing is DrawingChrono || hitDrawing is DrawingChronoMulti)
                {
                    AbstractDrawing drawing = hitDrawing;
                    PrepareDrawingContextMenu(drawing, popMenuDrawings);
                    popMenuDrawings.Items.Add(mnuDeleteDrawing);
                    panelCenter.ContextMenuStrip = popMenuDrawings;
                }
                else if (hitDrawing is DrawingTrack)
                {
                    DrawingTrack track = (DrawingTrack)hitDrawing;
                    PrepareTrackContextMenu(track, popMenuTrack);
                    popMenuTrack.Items.Add(mnuDeleteTrajectory);
                    panelCenter.ContextMenuStrip = popMenuTrack;
                }
                else if (hitDrawing is DrawingCoordinateSystem || hitDrawing is DrawingTestGrid)
                {
                    PrepareDrawingContextMenu(hitDrawing, popMenuDrawings);
                    panelCenter.ContextMenuStrip = popMenuDrawings;
                }
                else if (hitDrawing is AbstractMultiDrawing)
                {
                    PrepareDrawingContextMenu(hitDrawing, popMenuDrawings);
                    popMenuDrawings.Items.Add(mnuDeleteDrawing);
                    panelCenter.ContextMenuStrip = popMenuDrawings;
                }
            }
            else if (m_FrameServer.Metadata.IsOnMagnifier(m_DescaledMouse))
            {
                PrepareMagnifierContextMenu(popMenuMagnifier);
                
                popMenuMagnifier.Items.AddRange(new ToolStripItem[] { 
                    new ToolStripSeparator(), 
                    mnuMagnifierFreeze,
                    mnuMagnifierTrack, 
                    new ToolStripSeparator(), 
                    mnuMagnifierDirect, 
                    mnuMagnifierQuit });

                panelCenter.ContextMenuStrip = popMenuMagnifier;
            }
            else if (m_ActiveTool != m_PointerTool)
            {
                // Right click in the background with tool active: tool preset configuration.
                FormToolPresets ftp = new FormToolPresets(m_ActiveTool);
                FormsHelper.Locate(ftp);
                ftp.ShowDialog();
                ftp.Dispose();
                UpdateCursor();
            }
            else
            {
                // Right click in the background with hand tool.
                if (videoFilterIsActive)
                {
                    PrepareFilterContextMenu(m_FrameServer.Metadata.ActiveVideoFilter, popMenuFilter);

                    popMenuFilter.Items.Add(new ToolStripSeparator());
                    mnuExitFilter.Text = string.Format("Exit {0}", m_FrameServer.Metadata.ActiveVideoFilter.FriendlyName);
                    popMenuFilter.Items.Add(mnuExitFilter);
                    popMenuFilter.Items.Add(new ToolStripSeparator());
                    popMenuFilter.Items.Add(mnuSaveAnnotations);
                    popMenuFilter.Items.Add(mnuSaveAnnotationsAs);

                    if (m_FrameServer.Metadata.ActiveVideoFilter.CanExportVideo)
                        popMenuFilter.Items.Add(mnuExportVideo);

                    if (m_FrameServer.Metadata.ActiveVideoFilter.CanExportImage)
                        popMenuFilter.Items.Add(mnuExportImage);

                    popMenuFilter.Items.Add(new ToolStripSeparator());
                    popMenuFilter.Items.Add(mnuCloseScreen);
                    panelCenter.ContextMenuStrip = popMenuFilter;
                }
                else
                {
                    PrepareBackgroundContextMenu(popMenu);

                    mnuTimeOrigin.Visible = true;
                    mnuDirectTrack.Visible = true;
                    mnuDirectTrack.Enabled = true;
                    mnuPasteDrawing.Visible = true;
                    mnuPasteDrawing.Enabled = DrawingClipboard.HasContent;
                    mnuPastePic.Visible = true;
                    mnuPastePic.Enabled = Clipboard.ContainsImage();
                    
                    panelCenter.ContextMenuStrip = popMenu;
                }
            }
        }
        private void PrepareBackgroundContextMenu(ContextMenuStrip popMenu)
        {
            popMenu.Items.Clear();
            popMenu.Items.AddRange(new ToolStripItem[]
            {
                        mnuTimeOrigin, 
                        mnuDirectTrack, 
                        new ToolStripSeparator(),
                        mnuCopyPic, 
                        mnuPastePic, 
                        mnuPasteDrawing, 
                        new ToolStripSeparator(),
                        mnuOpenVideo, 
                        mnuOpenReplayWatcher, 
                        mnuOpenAnnotations, 
                        new ToolStripSeparator(),
                        mnuSaveAnnotations, 
                        mnuSaveAnnotationsAs, 
                        mnuExportVideo, 
                        mnuExportImage, 
                        new ToolStripSeparator(),
                        mnuCloseScreen
            });
        }
        private void PrepareDrawingContextMenu(AbstractDrawing drawing, ContextMenuStrip popMenu)
        {
            popMenu.Items.Clear();

            
            // Generic menus based on the drawing capabilities: configuration (style), visibility, tracking.
            if (!m_FrameServer.Metadata.DrawingInitializing)
                PrepareDrawingContextMenuCapabilities(drawing, popMenu);

            if (popMenu.Items.Count > 0)
                popMenu.Items.Add(mnuSepDrawing);

            // Custom menu handlers implemented by the drawing itself.
            // These change the drawing core state. (ex: angle orientation, measurement display option, start/stop chrono, etc.).
            bool hasExtraMenus = AddDrawingCustomMenus(drawing, popMenu.Items);

            // "Goto parent keyframe" menu.
            if (!m_FrameServer.Metadata.DrawingInitializing && drawing.InfosFading != null && m_FrameServer.Metadata.IsAttachedDrawing(drawing))
            {
                bool gotoVisible = PreferencesManager.PlayerPreferences.DefaultFading.Enabled && (drawing.InfosFading.ReferenceTimestamp != m_iCurrentPosition);
                if (gotoVisible)
                {
                    popMenu.Items.Add(mnuGotoKeyframe);
                    hasExtraMenus = true;
                }
            }

            // Below the custom menus and the goto keyframe we have the generic copy-paste and the delete menu.
            // Some singleton drawings cannot be deleted nor copy-pasted, so they don't need the separator.
            if (drawing is DrawingCoordinateSystem || drawing is DrawingTestGrid)
                return;

            if (hasExtraMenus)
                popMenu.Items.Add(mnuSepDrawing2);

            if (drawing.IsCopyPasteable)
            {
                popMenuDrawings.Items.Add(mnuCutDrawing);
                popMenuDrawings.Items.Add(mnuCopyDrawing);
                popMenuDrawings.Items.Add(mnuSepDrawing3);
            }
        }
        private void PrepareDrawingContextMenuCapabilities(AbstractDrawing drawing, ContextMenuStrip popMenu)
        {
            // Generic context menu from drawing capabilities.
            if ((drawing.Caps & DrawingCapabilities.ConfigureColor) == DrawingCapabilities.ConfigureColor ||
               (drawing.Caps & DrawingCapabilities.ConfigureColorSize) == DrawingCapabilities.ConfigureColorSize)
            {
                mnuConfigureDrawing.Text = ScreenManagerLang.Generic_ConfigurationElipsis;
                popMenu.Items.Add(mnuConfigureDrawing);

                bool isSingleton = drawing is DrawingCoordinateSystem || drawing is DrawingTestGrid || drawing is DrawingNumberSequence;
                if (!isSingleton)
                {
                    mnuSetStyleAsDefault.Text = ScreenManagerLang.mnuSetStyleAsDefault;
                    popMenu.Items.Add(mnuSetStyleAsDefault);
                }
            }

            if (PreferencesManager.PlayerPreferences.DefaultFading.Enabled && ((drawing.Caps & DrawingCapabilities.Fading) == DrawingCapabilities.Fading))
            {
                mnuVisibilityDefault.Checked = drawing.InfosFading.UseDefault;
                mnuVisibilityAlways.Checked = !drawing.InfosFading.UseDefault && drawing.InfosFading.AlwaysVisible;
                mnuVisibilityCustom.Checked = !drawing.InfosFading.UseDefault && !drawing.InfosFading.AlwaysVisible;
                popMenu.Items.Add(mnuVisibility);
            }

            if ((drawing.Caps & DrawingCapabilities.Opacity) == DrawingCapabilities.Opacity)
            {
                popMenu.Items.Add(mnuVisibility);
            }

            if ((drawing.Caps & DrawingCapabilities.Track) == DrawingCapabilities.Track)
            {
                bool tracked = ToggleTrackingCommand.CurrentState(drawing);
                mnuDrawingTrackingStart.Visible = !tracked;
                mnuDrawingTrackingStop.Visible = tracked;
                popMenu.Items.Add(mnuDrawingTracking);
            }
        }
        private bool AddDrawingCustomMenus(AbstractDrawing drawing, ToolStripItemCollection menuItems)
        {
            List<ToolStripItem> extraMenu;
            
            if (drawing is DrawingChronoMulti)
                extraMenu = ((DrawingChronoMulti)drawing).GetContextMenu(m_iCurrentPosition);
            else
                extraMenu = drawing.ContextMenu;

            bool hasExtraMenu = (extraMenu != null && extraMenu.Count > 0);
            if (!hasExtraMenu)
                return false;

            foreach (ToolStripItem tsmi in extraMenu)
            {
                ToolStripMenuItem menuItem = tsmi as ToolStripMenuItem;

                // Inject a dependency on this screen into the drawing.
                // Since the drawing now owns a piece of the UI, it may need to call back into functions here.
                // This is used to invalidate the view and complete operations that are normally handled here and 
                // require calls to other objects that the drawing itself doesn't have access to, like when the 
                // polyline drawing handles InitializeEnd and needs to remove the last point added to tracking.
                tsmi.Tag = this;

                // Also inject for all the sub menus.
                if (menuItem != null && menuItem.DropDownItems.Count > 0)
                {
                    foreach (ToolStripItem subMenu in menuItem.DropDownItems)
                        subMenu.Tag = this;
                }

                if (tsmi.MergeIndex >= 0)
                    menuItems.Insert(tsmi.MergeIndex, tsmi);
                else
                    menuItems.Add(tsmi);
            }

            return true;
        }
        private void PrepareTrackContextMenu(DrawingTrack track, ContextMenuStrip popMenu)
        {
            popMenu.Items.Clear();
            popMenu.Items.Add(mnuConfigureTrajectory);
            popMenu.Items.Add(new ToolStripSeparator());

            bool customMenus = AddDrawingCustomMenus(track, popMenu.Items);
            if (customMenus)
                popMenu.Items.Add(new ToolStripSeparator());
        }
        private void PrepareMagnifierContextMenu(ContextMenuStrip popMenu)
        {
            popMenu.Items.Clear();
            Magnifier magnifier = m_FrameServer.Metadata.Magnifier;

            foreach (ToolStripItem tsmi in magnifier.ContextMenu)
            {
                ToolStripMenuItem menuItem = tsmi as ToolStripMenuItem;

                // Inject dependency on the UI for invalidation.
                tsmi.Tag = this;
                if (menuItem != null && menuItem.DropDownItems.Count > 0)
                {
                    foreach (ToolStripItem subMenu in menuItem.DropDownItems)
                        subMenu.Tag = this;
                }

                popMenu.Items.Add(tsmi);
            }

            mnuMagnifierFreeze.Text = magnifier.Frozen ? "Unfreeze" : "Freeze";
            mnuMagnifierTrack.Checked = ToggleTrackingCommand.CurrentState(m_FrameServer.Metadata.Magnifier);
        }
        private void PrepareFilterContextMenu(IVideoFilter filter, ContextMenuStrip popMenu)
        {
            popMenu.Items.Clear();

            if (filter == null || !filter.HasContextMenu)
                return;

            List< ToolStripItem> menus = filter.GetContextMenu(m_DescaledMouse, m_iCurrentPosition);
            foreach (ToolStripItem tsmi in menus)
            {
                ToolStripMenuItem menuItem = tsmi as ToolStripMenuItem;

                // Inject dependency on the UI into the menu for invalidation.
                tsmi.Tag = this;
                if (menuItem != null && menuItem.DropDownItems.Count > 0)
                {
                    foreach (ToolStripItem subMenu in menuItem.DropDownItems)
                        subMenu.Tag = this;
                }

                if (tsmi.MergeIndex >= 0)
                    popMenu.Items.Insert(tsmi.MergeIndex, tsmi);
                else
                    popMenu.Items.Add(tsmi);
            }
        }
        private void SurfaceScreen_MouseMove(object sender, MouseEventArgs e)
        {
            // We must keep the same Z order.
            // 1:Magnifier, 2:Drawings, 3:Chronos/Tracks
            // When creating a drawing, the active tool will stay on this drawing until its setup is over.
            // After the drawing is created, we either fall back to Pointer tool or stay on the same tool.
            
            if (!m_FrameServer.Loaded)
                return;

            m_DescaledMouse = m_FrameServer.ImageTransform.Untransform(e.Location);

            if (e.Button == MouseButtons.None && m_FrameServer.Metadata.Magnifier.Initializing)
            {
                // Moving the magnifier source area around.
                m_FrameServer.Metadata.Magnifier.InitializeMove(m_DescaledMouse, ModifierKeys);
                DoInvalidate();
            }
            else if (e.Button == MouseButtons.None && m_FrameServer.Metadata.DrawingInitializing)
            {
                // Moving the third+ point of a drawing that was just created.
                IInitializable initializableDrawing = m_FrameServer.Metadata.HitDrawing as IInitializable;
                if (initializableDrawing != null)
                {
                    initializableDrawing.InitializeMove(m_DescaledMouse, ModifierKeys);
                    DoInvalidate();
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (m_ActiveTool != m_PointerTool)
                {
                    // Moving the second point of a drawing that was just created.
                    // Tools that are not IInitializable should reset to Pointer tool right after creation.
                    if (m_ActiveTool == ToolManager.Tools["Spotlight"])
                    {
                        IInitializable initializableDrawing = m_FrameServer.Metadata.DrawingSpotlight as IInitializable;
                        initializableDrawing.InitializeMove(m_DescaledMouse, ModifierKeys);
                    }
                    else if (!m_bIsCurrentlyPlaying && m_iActiveKeyFrameIndex >= 0 && m_FrameServer.Metadata.HitDrawing != null)
                    {
                        IInitializable initializableDrawing = m_FrameServer.Metadata.HitDrawing as IInitializable;
                        if (initializableDrawing != null)
                            initializableDrawing.InitializeMove(m_DescaledMouse, ModifierKeys);
                    }

                    if (!m_bIsCurrentlyPlaying)
                    {
                        DoInvalidate();
                    }
                }
                else if (!m_bIsCurrentlyPlaying)
                {
                    HandMove();
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                // Middle mouse button: supercedes the selected tool to provide manipulation.
                if (!m_bIsCurrentlyPlaying)
                {
                    HandMove();
                }
            }
        }

        private void HandMove()
        {
            // Hand tool interaction.
            // - Manipulation of an existing drawing via a handle.
            // - Time grab.
            // - Manipulation in a video filter.
            // - Panning the video while zoomed in.
            
            bool movedObject = m_PointerTool.OnMouseMove(m_FrameServer.Metadata, m_DescaledMouse, m_FrameServer.ImageTransform.ZoomWindow.Location, ModifierKeys);
            if (movedObject)
            {
                DoInvalidate();
                return;
            }

            if (m_FrameServer.Metadata.Magnifier.Mode == MagnifierMode.Active)
            {
                movedObject = m_FrameServer.Metadata.Magnifier.OnMouseMove(m_DescaledMouse, ModifierKeys);
                if (movedObject)
                {
                    DoInvalidate();
                    return;
                }
            }

            // User is not moving anything: time-grab, filter interaction, pan.
            // TODO: let filters delegate the handling to the normal mechanics.
            bool isAlt = (ModifierKeys & Keys.Alt) == Keys.Alt;
            bool isCtrl = (ModifierKeys & Keys.Control) == Keys.Control;
            if (isAlt)
            {
                // Time grab.
                float dtx = m_PointerTool.MouseDeltaOrigin.X * timeGrabSpeed;
                float dty = m_PointerTool.MouseDeltaOrigin.Y * timeGrabSpeed;
                float dt = Math.Abs(dtx) > Math.Abs(dty) ? dtx : dty;
                long target = m_PointerTool.OriginTime - (long)(dt * m_FrameServer.Metadata.AverageTimeStampsPerFrame);
                target = Math.Min(Math.Max(m_iSelStart, target), m_iSelEnd);

                // FIXME: Ignore / skip if busy.
                m_iFramesToDecode = 1;
                ShowNextFrame(target, true);
                UpdatePositionUI();
            }
            else if (videoFilterIsActive && !isCtrl)
            {
                // Filter-specific.
                float dx = m_PointerTool.MouseDelta.X;
                float dy = m_PointerTool.MouseDelta.Y;
                m_FrameServer.Metadata.ActiveVideoFilter.Move(dx, dy, ModifierKeys);
            }
            else
            {
                // CTRL or no modifiers on background: pan.
                float dx = m_PointerTool.MouseDelta.X;
                float dy = m_PointerTool.MouseDelta.Y;
                bool contain = m_FrameServer.Metadata.Magnifier.Mode != MagnifierMode.Inactive;
                m_FrameServer.ImageTransform.MoveZoomWindow(dx, dy, contain);
            }

            DoInvalidate();
        }

        private void SurfaceScreen_MouseUp(object sender, MouseEventArgs e)
        {
            // End of an action.
            // Depending on the active tool we have various things to do.

            if (!m_FrameServer.Loaded)
                return;

            if (videoFilterIsActive && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle))
                m_FrameServer.Metadata.ActiveVideoFilter.StopMove();

            if (e.Button == MouseButtons.Middle)
            {
                // Special case where we pan around with an active tool that is not the hand tool.
                // Restore the cursor of the active tool.
                UpdateCursor();
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            m_DescaledMouse = m_FrameServer.ImageTransform.Untransform(e.Location);

            if (m_ActiveTool == m_PointerTool)
            {
                OnPoke();
                m_FrameServer.Metadata.UpdateTrackPoint(m_FrameServer.CurrentImage);
                ReportForSyncMerge();
            }

            m_FrameServer.Metadata.InitializeCommit(m_FrameServer.VideoReader.Current, m_DescaledMouse);

            if (m_bTextEdit && m_ActiveTool != m_PointerTool && m_iActiveKeyFrameIndex >= 0)
                m_bTextEdit = false;

            // The fact that we stay on this tool or fall back to pointer tool, depends on the tool.
            m_ActiveTool = m_ActiveTool.KeepTool ? m_ActiveTool : m_PointerTool;

            if (m_ActiveTool == m_PointerTool)
            {
                SetCursor(m_PointerTool.GetCursor(0));
                m_PointerTool.OnMouseUp();
                m_FrameServer.Metadata.Magnifier.OnMouseUp();

                // If we were resizing an SVG drawing, trigger a render.
                // TODO: this is currently triggered on every mouse up, not only on resize !
                DrawingSVG d = m_FrameServer.Metadata.HitDrawing as DrawingSVG;
                if (d != null)
                    d.ResizeFinished();
            }

            if (m_FrameServer.Metadata.HitDrawing != null && !m_FrameServer.Metadata.DrawingInitializing)
                m_DeselectionTimer.Start();

            DoInvalidate();
        }
        private void SurfaceScreen_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!m_FrameServer.Loaded || e.Button != MouseButtons.Left || m_ActiveTool != m_PointerTool)
                return;

            OnPoke();

            m_DescaledMouse = m_FrameServer.ImageTransform.Untransform(e.Location);
            m_FrameServer.Metadata.AllDrawingTextToNormalMode();
            m_FrameServer.Metadata.DeselectAll();

            AbstractDrawing hitDrawing = null;

            //------------------------------------------------------------------------------------
            // - If on text, switch to edit mode.
            // - If on other drawing, launch the configuration dialog.
            // - Otherwise -> Maximize/Reduce image.
            //------------------------------------------------------------------------------------
            if (m_FrameServer.Metadata.IsOnDrawing(m_iActiveKeyFrameIndex, m_DescaledMouse, m_iCurrentPosition))
            {
                // Double click on a drawing:
                // turn text tool into edit mode, launch config for others.
                AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
                if (drawing is DrawingText)
                {
                    ((DrawingText)drawing).SetEditMode(true, m_DescaledMouse, m_FrameServer.ImageTransform);
                    m_ActiveTool = ToolManager.Tools["Label"];
                    m_bTextEdit = true;
                }
                else
                {
                    mnuConfigureDrawing_Click(null, EventArgs.Empty);
                }
            }
            else if ((hitDrawing = m_FrameServer.Metadata.IsOnDetachedDrawing(m_DescaledMouse, m_iCurrentPosition)) != null)
            {
                if (hitDrawing is DrawingChrono || hitDrawing is DrawingChronoMulti)
                {
                    mnuConfigureDrawing_Click(null, EventArgs.Empty);
                }
                else if (hitDrawing is DrawingTrack)
                {
                    mnuConfigureTrajectory_Click(null, EventArgs.Empty);
                }
            }
            else
            {
                ToggleImageFillMode();
            }
        }
        private void SurfaceScreen_Paint(object sender, PaintEventArgs e)
        {
            //-------------------------------------------------------------------
            // We always draw at full SurfaceScreen size.
            // It is the SurfaceScreen itself that is resized if needed.
            //-------------------------------------------------------------------
            if (!m_FrameServer.Loaded || saveInProgress || m_DualSaveInProgress)
                return;

            m_TimeWatcher.LogTime("Actual start of paint");

            if (m_FrameServer.CurrentImage != null)
            {
                try
                {
                    // If we are on a keyframe, see if it has any drawing.
                    int iKeyFrameIndex = -1;
                    if (m_iActiveKeyFrameIndex >= 0)
                    {
                        if (m_FrameServer.Metadata[m_iActiveKeyFrameIndex].Drawings.Count > 0)
                        {
                            iKeyFrameIndex = m_iActiveKeyFrameIndex;
                        }
                    }

                    FlushOnGraphics(m_FrameServer.CurrentImage, e.Graphics, m_viewportManipulator.RenderingSize, iKeyFrameIndex, m_iCurrentPosition, m_FrameServer.ImageTransform);

                    if (m_MessageToaster.Enabled)
                        m_MessageToaster.Draw(e.Graphics);

                    //log.DebugFormat("play loop to end of paint: {0}/{1}", m_Stopwatch.ElapsedMilliseconds, m_FrameServer.VideoReader.Info.FrameIntervalMilliseconds);
                }
                catch (System.InvalidOperationException)
                {
                    log.Error("Error while painting image. Object is currently in use elsewhere.");
                }
                catch (Exception)
                {
                    log.Error("Error while painting image.");
                }
            }
            else
            {
                log.Error("Painting screen - no image to display.");
            }

            // Draw Selection Border if needed.
            if (m_bShowImageBorder)
            {
                DrawImageBorder(e.Graphics);
            }

            m_TimeWatcher.LogTime("Finished painting.");
        }
        private void SurfaceScreen_MouseEnter(object sender, EventArgs e)
        {
            // Set focus to surfacescreen to enable mouse scroll
            if (!m_FrameServer.Metadata.TextEditingInProgress)
                pbSurfaceScreen.Focus();
        }
        private void FlushOnGraphics(Bitmap _sourceImage, Graphics g, Size _renderingSize, int _iKeyFrameIndex, long _iPosition, ImageTransform _transform)
        {
            // This function is used both by the main rendering loop and by image export functions.
            // Video export get its image from the VideoReader or the cache.

            // Notes on performances:
            // - The global performance depends on the size of the *source* image. Not destination.
            //   (rendering 1 pixel from an HD source will still be slow)
            // - Using a matrix transform instead of the buit in interpolation doesn't seem to do much.
            // - InterpolationMode has a sensible effect. but can look ugly for lowest values.
            // - Using unmanaged BitBlt or StretchBlt doesn't seem to do much... (!?)
            // - the scaling and interpolation better be done directly from ffmpeg. (cut on memory usage too)
            // - furthermore ffmpeg has a mode called 'FastBilinear' that seems more promising.
            // - Drawing unscaled avoid the interpolation altogether and provide ~8x perfs.

            // 1. Image
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            //g.CompositingQuality = CompositingQuality.HighSpeed;
            //g.InterpolationMode = InterpolationMode.Bilinear;
            //g.InterpolationMode = InterpolationMode.NearestNeighbor;
            //g.SmoothingMode = SmoothingMode.None;

            m_TimeWatcher.LogTime("Before DrawImage");

            // New.
            Rectangle rDst = new Rectangle(Point.Empty, _renderingSize);

            bool drawn = false;
            if (m_viewportManipulator.MayDrawUnscaled && m_FrameServer.VideoReader.CanDrawUnscaled)
            {
                // Source image should be at the right size, unless it has been temporarily disabled.
                // This is an optimization where the video reader is asked to decode images that might be smaller than the original size, 
                // in order to match the rendering size.
                if (!m_FrameServer.Metadata.Mirrored  && _transform.ZoomWindowInDecodedImage.Size.CloseTo(_renderingSize, 4))
                {
                    g.DrawImageUnscaled(_sourceImage, -_transform.ZoomWindowInDecodedImage.Left, -_transform.ZoomWindowInDecodedImage.Top);
                    drawn = true;
                }
            }
            else if (!m_FrameServer.Metadata.Mirrored && !_transform.Zooming && _transform.Stretch == 1.0f && _transform.DecodingScale == 1.0)
            {
                // This allow to draw unscaled while tracking or caching for example, provided we are rendering at original size.
                g.DrawImageUnscaled(_sourceImage, -_transform.ZoomWindowInDecodedImage.Left, -_transform.ZoomWindowInDecodedImage.Top);
                drawn = true;
            }
            
            if (!drawn)
            {
                Rectangle rSrc;
                if (m_FrameServer.Metadata.Mirrored)
                {
                    rSrc = new Rectangle(
                        _sourceImage.Width - 1 - _transform.ZoomWindowInDecodedImage.X,
                        _transform.ZoomWindowInDecodedImage.Top,
                        -_transform.ZoomWindowInDecodedImage.Width,
                        _transform.ZoomWindowInDecodedImage.Height
                     );
                }
                else
                {
                    rSrc = _transform.ZoomWindowInDecodedImage;
                }

                g.DrawImage(_sourceImage, rDst, rSrc, GraphicsUnit.Pixel);
            }

            m_TimeWatcher.LogTime("After DrawImage");

            // .Sync superposition.
            if (m_bSynched && m_bSyncMerge && m_SyncMergeImage != null)
            {
                // The mirroring, if any, will have been done already and applied to the sync image.
                // (because to draw the other image, we take into account its own mirroring option,
                // not the option in this screen.)
                Rectangle rSyncDst = new Rectangle(0, 0, _renderingSize.Width, _renderingSize.Height);
                g.DrawImage(m_SyncMergeImage, rSyncDst, 0, 0, m_SyncMergeImage.Width, m_SyncMergeImage.Height, GraphicsUnit.Pixel, m_SyncMergeImgAttr);
            }

            // Background fader.
            Color backgroundColor = PreferencesManager.PlayerPreferences.BackgroundColor;
            if (backgroundColor.A != 0)
            {
                SolidBrush brush = new SolidBrush(backgroundColor);
                g.FillRectangle(brush, rDst);
                brush.Dispose();
            }

            if (
                (showDrawings && m_bIsCurrentlyPlaying && PreferencesManager.PlayerPreferences.DrawOnPlay) || 
                (showDrawings && !m_bIsCurrentlyPlaying))
            {
                // First draw the magnifier, this includes drawing the objects that are under
                // the source area onto the destination area, and then draw the objects on the 
                // image. This way we can still have drawings on top of the magnifier destination area.
                FlushMagnifierOnGraphics(_sourceImage, g, _transform, _iKeyFrameIndex, _iPosition);
                FlushDrawingsOnGraphics(g, _transform, _iKeyFrameIndex, _iPosition);
            }
        }
        private void FlushDrawingsOnGraphics(Graphics canvas, ImageTransform transformer, int keyFrameIndex, long timestamp)
        {
            DistortionHelper distorter = m_FrameServer.Metadata.CalibrationHelper.DistortionHelper;
            
            // Prepare for drawings
            canvas.SmoothingMode = SmoothingMode.AntiAlias;
            canvas.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            bool drawAttached = true;
            bool drawDetached = true;
            if (m_FrameServer.Metadata.ActiveVideoFilter != null)
            {
                m_FrameServer.Metadata.ActiveVideoFilter.DrawExtra(canvas, distorter, transformer, timestamp, false);
                drawAttached = m_FrameServer.Metadata.ActiveVideoFilter.DrawAttachedDrawings;
                drawDetached = m_FrameServer.Metadata.ActiveVideoFilter.DrawDetachedDrawings;
            }

            if (drawDetached)
            {
                foreach (AbstractDrawing chrono in m_FrameServer.Metadata.ChronoManager.Drawings)
                {
                    bool selected = m_FrameServer.Metadata.HitDrawing == chrono;
                    chrono.Draw(canvas, distorter, transformer, selected, timestamp);
                }

                foreach (DrawingTrack track in m_FrameServer.Metadata.TrackManager.Drawings)
                {
                    bool selected = m_FrameServer.Metadata.HitDrawing == track;
                    track.Draw(canvas, distorter, transformer, selected, timestamp);
                }

                foreach (AbstractDrawing drawing in m_FrameServer.Metadata.SingletonDrawingsManager.Drawings)
                {
                    bool selected = m_FrameServer.Metadata.HitDrawing == drawing;
                    drawing.Draw(canvas, distorter, transformer, selected, timestamp);
                }
            }

            if (drawAttached)
            {
                if (PreferencesManager.PlayerPreferences.DefaultFading.Enabled)
                {
                    // If fading is on, we ask all drawings to draw themselves with their respective
                    // fading factor for this position.

                    int[] zOrder = m_FrameServer.Metadata.GetKeyframesZOrder(timestamp);

                    // Draw in reverse keyframes z order so the closest next keyframe gets drawn on top (last).
                    for (int kfIndex = zOrder.Length - 1; kfIndex >= 0; kfIndex--)
                    {
                        Keyframe keyframe = m_FrameServer.Metadata.Keyframes[zOrder[kfIndex]];
                        for (int drawingIndex = keyframe.Drawings.Count - 1; drawingIndex >= 0; drawingIndex--)
                        {
                            bool selected = keyframe.Drawings[drawingIndex] == m_FrameServer.Metadata.HitDrawing;
                            keyframe.Drawings[drawingIndex].Draw(canvas, distorter, transformer, selected, timestamp);
                        }
                    }
                }
                else if (keyFrameIndex >= 0)
                {
                    // if fading is off, only draw the current keyframe.
                    // Draw all drawings in reverse order to get first object on the top of Z-order.
                    Keyframe keyframe = m_FrameServer.Metadata.Keyframes[keyFrameIndex];
                    for (int drawingIndex = keyframe.Drawings.Count - 1; drawingIndex >= 0; drawingIndex--)
                    {
                        bool selected = keyframe.Drawings[drawingIndex] == m_FrameServer.Metadata.HitDrawing;
                        keyframe.Drawings[drawingIndex].Draw(canvas, distorter, transformer, selected, timestamp);
                    }
                }
                else
                {
                    // This is not a Keyframe, and fading is off.
                    // Hence, there is no drawings to draw here.
                }
            }
        }
        private void FlushMagnifierOnGraphics(Bitmap currentImage, Graphics canvas, ImageTransform transform, int keyFrameIndex, long timestamp)
        {
            // Note: the Graphics object must not be the one extracted from the image itself.
            // If needed, clone the image.
            if (currentImage == null || m_FrameServer.Metadata.Magnifier.Mode == MagnifierMode.Inactive)
                return;
            
            // Draw the magnifier source rectangle and magnified area.
            m_FrameServer.Metadata.Magnifier.Draw(currentImage, canvas, transform, m_FrameServer.Metadata.Mirrored, m_FrameServer.VideoReader.Info.ReferenceSize);

            // Redraw the annotations on top of the magnified area.
            m_FrameServer.Metadata.Magnifier.TransformCanvas(canvas, transform);
            FlushDrawingsOnGraphics(canvas, transform, keyFrameIndex, timestamp);
            canvas.ResetTransform();
            canvas.ResetClip();
        }
        public void DoInvalidate()
        {
            // This function should be the single point where we call for rendering.
            // Here we can decide to render directly on the surface, go through the Windows message pump, force the refresh, etc.

            // Invalidate is asynchronous and several Invalidate calls will be grouped together. (Only one repaint will be done).
            pbSurfaceScreen.Invalidate();
        }
        public void InvalidateFromMenu()
        {
            if (SetAsActiveScreen != null)
                SetAsActiveScreen(this, EventArgs.Empty);

            DoInvalidate();
        }
        public void InitializeEndFromMenu(bool cancelLastPoint)
        {
            m_FrameServer.Metadata.InitializeEnd(cancelLastPoint);
        }
        #endregion

        #region PanelCenter Events
        private void PanelCenter_MouseEnter(object sender, EventArgs e)
        {
            panelCenter.Focus();
        }
        private void PanelCenter_MouseClick(object sender, MouseEventArgs e)
        {
            OnPoke();
        }
        private void PanelCenter_Resize(object sender, EventArgs e)
        {
            if (m_Constructed)
                ResizeUpdate(true);
        }
        private void PanelCenter_MouseDown(object sender, MouseEventArgs e)
        {
            mnuDirectTrack.Enabled = false;
            mnuPasteDrawing.Enabled = false;
            mnuPastePic.Enabled = false;
            panelCenter.ContextMenuStrip = popMenu;
            RaiseSetAsActiveScreenEvent();
        }
        #endregion

        #region Keyframes Panel
        private void pnlThumbnails_MouseEnter(object sender, EventArgs e)
        {
            // Give focus to disable keyframe box editing.
            pnlThumbnails.Focus();
        }
        private void splitKeyframes_Resize(object sender, EventArgs e)
        {
            // Redo the dock/undock if needed to be at the right place.
            // (Could be handled by layout ?)
            CollapseKeyframePanel(m_bKeyframePanelCollapsed);
        }
        private void btnAddKeyframe_Click(object sender, EventArgs e)
        {
            if (m_FrameServer.Loaded)
            {
                AddKeyframe();

                // Set as active screen is done afterwards, so the export as pdf menu is activated
                // even if we had no keyframes yet.
                OnPoke();
            }
        }
        public void OrganizeKeyframes()
        {
            // Should only be called when adding/removing a Thumbnail
            ClearKeyframeBoxes();

            if (m_FrameServer.Metadata.Count > 0)
            {
                int pixelsOffset = 0;
                int pixelsSpacing = 20;

                foreach (Keyframe kf in m_FrameServer.Metadata.Keyframes)
                {
                    KeyframeBox box = new KeyframeBox(kf);
                    SetupDefaultThumbBox(box);

                    // Finish the setup
                    box.Left = pixelsOffset + pixelsSpacing;
                    box.DeleteAsked += KeyframeControl_KeyframeDeleteAsked;
                    box.Selected += KeyframeControl_KeyframeSelected;
                    box.MoveToCurrentTimeAsked += KeyframeControl_MoveToCurrentTimeAsked;

                    pixelsOffset += (pixelsSpacing + box.Width);

                    pnlThumbnails.Controls.Add(box);
                    keyframeBoxes.Add(box);
                }

                EnableDisableKeyframes();
                pnlThumbnails.Refresh();
            }
            else
            {
                CollapseKeyframePanel(true);
                m_iActiveKeyFrameIndex = -1;
            }

            sidePanelKeyframes.Reset(m_FrameServer.Metadata);
            UpdateFramesMarkers();
            DoInvalidate(); // Because of trajectories with keyframes labels.
        }
        private void SetupDefaultThumbBox(UserControl _box)
        {
            _box.Top = 10;
            _box.Cursor = Cursors.Hand;
        }
        private void ActivateKeyframe(long timestamp)
        {
            ActivateKeyframe(timestamp, true);
            sidePanelKeyframes.HighlightKeyframe(timestamp);
        }
        private void ActivateKeyframe(long _iPosition, bool _bAllowUIUpdate)
        {
            //--------------------------------------------------------------
            // Black border every keyframe, unless it is at the given position.
            // This method might be called with -1 to force complete blackout.
            //--------------------------------------------------------------

            // This method is called on each frame during frame-by-frame navigation.
            // keep it fast or fix the strategy.

            sidePanelKeyframes.HighlightKeyframe(_iPosition);

            m_iActiveKeyFrameIndex = -1;
            if (keyframeBoxes.Count != m_FrameServer.Metadata.Count)
                return;

            for (int i = 0; i < keyframeBoxes.Count; i++)
            {
                if (m_FrameServer.Metadata[i].Timestamp == _iPosition)
                {
                    m_iActiveKeyFrameIndex = i;
                    if (_bAllowUIUpdate)
                    {
                        keyframeBoxes[i].DisplayAsSelected(true);
                        pnlThumbnails.ScrollControlIntoView(keyframeBoxes[i]);

                        if (!m_FrameServer.Metadata[i].HasThumbnails && m_FrameServer.CurrentImage != null)
                        {
                            m_FrameServer.Metadata[i].InitializeImage(m_FrameServer.CurrentImage);
                            keyframeBoxes[i].UpdateImage();
                        }
                    }
                }
                else
                {
                    if (_bAllowUIUpdate)
                        keyframeBoxes[i].DisplayAsSelected(false);
                }
            }

            if (_bAllowUIUpdate && m_KeyframeCommentsHub.UserActivated && m_iActiveKeyFrameIndex >= 0)
            {
                m_KeyframeCommentsHub.UpdateContent(m_FrameServer.Metadata[m_iActiveKeyFrameIndex]);
                m_KeyframeCommentsHub.Visible = true;
            }
            else
            {
                if (m_KeyframeCommentsHub.Visible)
                    m_KeyframeCommentsHub.CommitChanges();

                m_KeyframeCommentsHub.Visible = false;
            }
        }
        private void EnableDisableKeyframes()
        {
            m_FrameServer.Metadata.EnableDisableKeyframes();

            foreach (KeyframeBox box in keyframeBoxes)
                box.UpdateEnableStatus();
        }

        // The keyframe name or color was changed.
        public void OnKeyframeNameChanged()
        {
            m_FrameServer.Metadata.UpdateTrajectoriesForKeyframes();
            EnableDisableKeyframes();
            UpdateFramesMarkers();
            DoInvalidate();
        }
        public void GotoNextKeyframe()
        {
            if (m_FrameServer.Metadata.Count == 0)
                return;

            int next = -1;
            for (int i = 0; i < m_FrameServer.Metadata.Count; i++)
            {
                if (m_iCurrentPosition < m_FrameServer.Metadata[i].Timestamp)
                {
                    next = i;
                    break;
                }
            }

            if (next >= 0 && m_FrameServer.Metadata[next].Timestamp <= m_iSelEnd)
                KeyframeControl_KeyframeSelected(null, new TimeEventArgs(m_FrameServer.Metadata[next].Timestamp));
        }
        public void GotoPreviousKeyframe()
        {
            if (m_FrameServer.Metadata.Count == 0)
                return;

            int prev = -1;
            for (int i = m_FrameServer.Metadata.Count - 1; i >= 0; i--)
            {
                if (m_iCurrentPosition > m_FrameServer.Metadata[i].Timestamp)
                {
                    prev = i;
                    break;
                }
            }

            if (prev >= 0 && m_FrameServer.Metadata[prev].Timestamp >= m_iSelStart)
                KeyframeControl_KeyframeSelected(null, new TimeEventArgs(m_FrameServer.Metadata[prev].Timestamp));
        }

        public void AddKeyframe()
        {
            int keyframeIndex = m_FrameServer.Metadata.GetKeyframeIndex(m_iCurrentPosition);
            if (keyframeIndex >= 0)
            {
                // There is already a keyframe here, just select it.
                m_iActiveKeyFrameIndex = keyframeIndex;
                Keyframe keyframe = m_FrameServer.Metadata.GetKeyframe(m_FrameServer.Metadata.GetKeyframeId(keyframeIndex));
                m_FrameServer.Metadata.SelectKeyframe(keyframe);
                return;
            }

            if (KeyframeAdding != null)
                KeyframeAdding(this, new KeyframeAddEventArgs(m_iCurrentPosition, null, Keyframe.DefaultColor));
        }

        public void AddPresetKeyframe(string name, Color color)
        {
            int keyframeIndex = m_FrameServer.Metadata.GetKeyframeIndex(m_iCurrentPosition);
            if (keyframeIndex >= 0)
            {
                // If there is already a keyframe here, do not overwrite it.
                return;
            }

            if (KeyframeAdding != null)
                KeyframeAdding(this, new KeyframeAddEventArgs(m_iCurrentPosition, name, color));
        }

        private void AfterKeyframeAdded(Guid keyframeId)
        {
            if (m_FrameServer.Metadata.KVAImporting)
                return;

            Keyframe keyframe = m_FrameServer.Metadata.GetKeyframe(keyframeId);
            if (keyframe == null)
                return;

            if (!keyframe.HasThumbnails)
                InitializeKeyframe(keyframe);

            OrganizeKeyframes();
            UpdateFramesMarkers();

            if (m_FrameServer.Metadata.Count == 1)
                CollapseKeyframePanel(false);

            if (!m_bIsCurrentlyPlaying)
                ActivateKeyframe(m_iCurrentPosition);
        }

        private void AfterKeyframeModified(Guid id)
        {
            // A keyframe was modified from the outside. This happens on undo for example.
            // Update the UI version of the keyframe.
            sidePanelKeyframes.UpdateKeyframe(id);

            KeyframeControl_KeyframeUpdated(null, new EventArgs<Guid>(id));
        }

        /// <summary>
        /// Initialize keyframes after KVA file import.
        /// </summary>
        private void InitializeKeyframes()
        {
            int firstOutOfRange = -1;
            int currentKeyframe = -1;
            long lastTimestamp = m_FrameServer.VideoReader.Info.FirstTimeStamp + m_FrameServer.VideoReader.Info.DurationTimeStamps;

            // We only create thumbnails for a few keyframes to avoid freezing on large load.
            // The other ones will be initialized later when the play head lands on them.
            int preloaded = 0;
            int maxPreload = PreferencesManager.PlayerPreferences.PreloadKeyframes;
            foreach (Keyframe kf in m_FrameServer.Metadata.Keyframes)
            {
                currentKeyframe++;

                if (kf.Timestamp < lastTimestamp)
                {
                    if (!kf.HasThumbnails && preloaded < maxPreload)
                        InitializeKeyframe(kf);

                    preloaded++;
                    continue;
                }

                if (firstOutOfRange < 0)
                {
                    firstOutOfRange = currentKeyframe;
                    break;
                }
            }

            if (firstOutOfRange != -1)
                m_FrameServer.Metadata.Keyframes.RemoveRange(firstOutOfRange, m_FrameServer.Metadata.Keyframes.Count - firstOutOfRange);
        }

        /// <summary>
        /// Fully initialize a keyframe thunbmail by seeking to the keyframe position and getting the image.
        /// </summary>
        private void InitializeKeyframe(Keyframe keyframe)
        {
            if (m_iCurrentPosition != keyframe.Timestamp)
            {
                m_iFramesToDecode = 1;
                ShowNextFrame(keyframe.Timestamp, true);
                UpdatePositionUI();
            }

            if (m_FrameServer.CurrentImage == null)
                return;

            // The actual position may differ from what was originally stored in the keyframe.
            keyframe.InitializePosition(m_iCurrentPosition);
            keyframe.InitializeImage(m_FrameServer.CurrentImage);
        }
        private void DeleteKeyframe(Guid keyframeId)
        {
            if (KeyframeDeleting != null)
                KeyframeDeleting(this, new KeyframeEventArgs(keyframeId));
        }
        private void AfterKeyframeDeleted()
        {
            m_iActiveKeyFrameIndex = m_FrameServer.Metadata.GetKeyframeIndex(m_iCurrentPosition);
            OrganizeKeyframes();
            UpdateFramesMarkers();
            DoInvalidate();
        }
        public void UpdateKeyframes()
        {
            // Primary selection has been image-adjusted,
            // some keyframes may have been impacted.

            bool bAtLeastOne = false;

            foreach (Keyframe kf in m_FrameServer.Metadata.Keyframes)
            {
                if (kf.Timestamp >= m_iSelStart && kf.Timestamp <= m_iSelEnd)
                {
                    //kf.ImportImage(m_FrameServer.VideoReader.FrameList[(int)m_FrameServer.VideoReader.GetFrameNumber(kf.Position)].BmpImage);
                    //kf.GenerateDisabledThumbnail();
                    bAtLeastOne = true;
                }
                else
                {
                    // Outside selection : couldn't possibly be impacted.
                }
            }

            if (bAtLeastOne)
                OrganizeKeyframes();

        }
        private void pnlThumbnails_DoubleClick(object sender, EventArgs e)
        {
            if (m_FrameServer.Loaded)
            {
                // On double click in the thumbs panel : Add a keyframe at current pos.
                AddKeyframe();
                OnPoke();
            }
        }

        #region ThumbBox event Handlers
        private void KeyframeControl_KeyframeDeleteAsked(object sender, EventArgs e)
        {
            KeyframeBox keyframeBox = sender as KeyframeBox;
            if (keyframeBox == null)
                return;

            DeleteKeyframe(keyframeBox.Keyframe.Id);

            // Set as active screen is done after in case we don't have any keyframes left.
            OnPoke();
        }
       
        private void KeyframeControl_MoveToCurrentTimeAsked(object sender, EventArgs e)
        {
            log.DebugFormat("Moving existing keyframe to a new time.");

            KeyframeBox keyframeBox = sender as KeyframeBox;
            if (keyframeBox == null)
                return;

            Keyframe keyframe = keyframeBox.Keyframe;
            if (keyframe == null)
                return;

            // If there is already a keyframe at the current time we ignore the request.
            int keyframeIndex = m_FrameServer.Metadata.GetKeyframeIndex(m_iCurrentPosition);
            if (keyframeIndex >= 0)
            {
                log.WarnFormat("Ignored move request: there is already a keyframe at the current time.");
                return;
            }

            // Check if this keyframe is ours.
            var knownKeyframe = m_FrameServer.Metadata.GetKeyframe(keyframe.Id);
            if (knownKeyframe == null)
            {
                // The keyframe is coming from outside.
                // Create a brand new one here and import the data.
                log.DebugFormat("Keyframe move: importing an external keyframe.");

                AddKeyframe();
                Keyframe newKf = m_FrameServer.Metadata.HitKeyframe;
                if (newKf == null)
                {
                    log.ErrorFormat("Keyframe move: a problem occurred while creating the recipient keyframe.");
                    return;
                }

                // Serialize the external keyframe.
                // This is mainly to get a clean clone of the drawing list.
                string serialized = "";
                XmlWriterSettings writerSettings = new XmlWriterSettings();
                writerSettings.Indent = false;
                writerSettings.CloseOutput = true;
                StringBuilder builder = new StringBuilder();
                using (XmlWriter w = XmlWriter.Create(builder, writerSettings))
                {
                    w.WriteStartElement("KeyframeMemento");
                    KeyframeSerializer.Serialize(w, keyframe, SerializationFilter.KVA);
                    w.WriteEndElement();
                    w.Flush();
                    serialized = builder.ToString();
                }

                // Deserialize it back.
                Keyframe copy = KeyframeSerializer.DeserializeMemento(serialized, m_FrameServer.Metadata);
                if (copy == null)
                {
                    log.ErrorFormat("Keyframe move: a problem occurred while deserializing the imported keyframe.");
                    return;
                }

                // Import the data manually.
                // Doing a global Metadata.MergeInsertKeyframe wouldn't work as the original keyframe has a different timestamp.
                newKf.Name = copy.Name;
                newKf.Color = copy.Color;
                newKf.Comments = copy.Comments;
                foreach (var d in copy.Drawings)
                    newKf.Drawings.Add(d);

                // Make sure the drawings are anchored to the right time for fading.
                newKf.Timestamp = m_iCurrentPosition;
            }
            else
            {
                // Change the keyframe reference time.
                keyframe.Timestamp = m_iCurrentPosition;
            }

            m_FrameServer.Metadata.Keyframes.Sort();
            OrganizeKeyframes();
            ActivateKeyframe(m_iCurrentPosition);
            UpdateFramesMarkers();
            DoInvalidate();
        }
        
        private void KeyframeControl_KeyframeSelected(object sender, TimeEventArgs e)
        {
            // A keyframe was selected from a keyframe control (thumbnail or side panel),
            // or from a command jumping from keyframe to keyframe.
            // Move to the corresponding time.
            if (e.Time < m_iSelStart || e.Time > m_iSelEnd)
                return;

            OnPoke();
            StopPlaying();
            OnPauseAsked();

            long targetPosition = e.Time;

            trkSelection.SelPos = targetPosition;
            m_iFramesToDecode = 1;

            ShowNextFrame(targetPosition, true);
            m_iCurrentPosition = targetPosition;

            UpdatePositionUI();
            ActivateKeyframe(m_iCurrentPosition);
        }

        private void KeyframeControl_KeyframeUpdated(object sender, EventArgs<Guid> e)
        {
            // A keyframe core data was updated from a keyframe control.
            // This is only raised when we change the name, color or comment from the side panel.
            // Update the corresponding thumbnail box.
            UpdateKeyframeBox(e.Value);

            UpdateFramesMarkers();
        }

        /// <summary>
        /// Update the keyframe box holding this keyframe after an external change.
        /// </summary>
        private void UpdateKeyframeBox(Guid id)
        {
            foreach (KeyframeBox box in keyframeBoxes)
            {
                if (box.Keyframe.Id == id)
                {
                    box.UpdateContent();
                    break;
                }
            }
        }
        #endregion

        #region Docking Undocking
        private void btnDockBottom_Click(object sender, EventArgs e)
        {
            m_bKeyframePanelCollapsedManual = !m_bKeyframePanelCollapsed;
            CollapseKeyframePanel(!m_bKeyframePanelCollapsed);
        }
        private void splitKeyframes_Panel2_DoubleClick(object sender, EventArgs e)
        {
            m_bKeyframePanelCollapsedManual = !m_bKeyframePanelCollapsed;
            CollapseKeyframePanel(!m_bKeyframePanelCollapsed);
        }
        private void CollapseKeyframePanel(bool collapse)
        {
            if (collapse)
            {
                // hide the keyframes, change image.
                splitKeyframes.SplitterDistance = splitKeyframes.Height - 25;
                btnDockBottom.BackgroundImage = Resources.undock16x16;
                btnDockBottom.Visible = m_FrameServer.Metadata.Count > 0;
            }
            else
            {
                // show the keyframes, change image.
                splitKeyframes.SplitterDistance = splitKeyframes.Height - 140;
                btnDockBottom.BackgroundImage = Resources.dock16x16;
                btnDockBottom.Visible = true;
            }

            m_bKeyframePanelCollapsed = collapse;
        }
        private void PrepareKeyframesDock()
        {
            // If there's no keyframe, and we will be using a tool,
            // the keyframes dock should be raised.
            // This way we don't surprise the user when he click the screen and the image moves around.
            // (especially problematic when using the Pencil).

            // this is only done for the very first keyframe.
            if (m_FrameServer.Metadata.Count < 1)
            {
                CollapseKeyframePanel(false);
            }
        }
        #endregion

        #endregion

        #region Drawings Toolbar Events
        private void drawingTool_Click(object sender, EventArgs e)
        {
            // User clicked on a drawing tool button. A reference to the tool is stored in .Tag
            // Set this tool as the active tool (waiting for the actual use) and set the cursor accordingly.

            // Deactivate magnifier if not commited.
            if (m_FrameServer.Metadata.Magnifier.Initializing)
                DisableMagnifier();

            OnPoke();

            AbstractDrawingTool tool = ((ToolStripItem)sender).Tag as AbstractDrawingTool;
            m_ActiveTool = tool ?? m_PointerTool;
            UpdateCursor();

            // Ensure there's a key image at this position, unless the tool creates unattached drawings.
            if (m_ActiveTool == m_PointerTool && m_FrameServer.Metadata.Count < 1)
                CollapseKeyframePanel(true);
            else if (m_ActiveTool.Attached)
                PrepareKeyframesDock();

            DoInvalidate();
        }
        private void btnMagnifier_Click(object sender, EventArgs e)
        {
            if (!m_FrameServer.Loaded)
                return;

            m_ActiveTool = m_PointerTool;

            switch (m_FrameServer.Metadata.Magnifier.Mode)
            {
                case MagnifierMode.Inactive:
                {
                    ResetZoom(false);
                    m_FrameServer.Metadata.Magnifier.Mode = MagnifierMode.Initializing;
                    SetCursor(cursorManager.GetManipulationCursorMagnifier());

                    if (TrackableDrawingAdded != null)
                        TrackableDrawingAdded(this, new TrackableDrawingEventArgs(m_FrameServer.Metadata.Magnifier as ITrackable));

                    break;
                }
                case MagnifierMode.Initializing:
                {
                    // Revert to no magnification.
                    ResetZoom(false);
                    m_FrameServer.Metadata.Magnifier.Mode = MagnifierMode.Inactive;
                    //btnMagnifier.Image = Drawings.magnifier;
                    SetCursor(m_PointerTool.GetCursor(0));
                    DoInvalidate();
                    break;
                }
                case MagnifierMode.Active:
                default:
                {
                    DisableMagnifier();
                    DoInvalidate();
                    break;
                }
            }
        }
        private void btnShowSidePanel_Click(object sender, EventArgs e)
        {
            OnPoke();

            if (!m_FrameServer.Loaded)
                return;

            // Toggle between showing and hiding the properties panel.
            showPropertiesPanel = !showPropertiesPanel;
            splitViewport_Properties.Panel2Collapsed = !showPropertiesPanel;
        }
        private void btnColorProfile_Click(object sender, EventArgs e)
        {
            OnPoke();

            // Load, save or modify current profile.
            FormToolPresets ftp = new FormToolPresets();
            FormsHelper.Locate(ftp);
            ftp.ShowDialog();
            ftp.Dispose();

            UpdateCursor();
            DoInvalidate();
        }
        private void UpdateCursor()
        {
            if (m_ActiveTool == m_PointerTool)
            {
                SetCursor(m_PointerTool.GetCursor(0));
            }
            else
            {
                Cursor cursor = cursorManager.GetToolCursor(m_ActiveTool, m_FrameServer.ImageTransform.Scale);
                SetCursor(cursor);
            }
        }
        private void SetCursor(Cursor _cur)
        {
            pbSurfaceScreen.Cursor = _cur;
        }
        #endregion

        #region Context Menus Events

        #region Main
        private void mnuTimeOrigin_Click(object sender, EventArgs e)
        {
            MarkTimeOrigin();
        }
        private void mnuDirectTrack_Click(object sender, EventArgs e)
        {
            // Track the point.
            // m_DescaledMouse would have been set during the MouseDown event.
            CheckCustomDecodingSize(true);

            Color color = TrackColorCycler.Next();
            DrawingStyle style = new DrawingStyle();
            style.Elements.Add("color", new StyleElementColor(color));
            style.Elements.Add("line size", new StyleElementLineSize(3));
            style.Elements.Add("track shape", new StyleElementTrackShape(TrackShape.Solid));

            DrawingTrack track = new DrawingTrack(m_DescaledMouse, m_iCurrentPosition, m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame, style);
            track.Status = TrackStatus.Edit;

            if (DrawingAdding != null)
                DrawingAdding(this, new DrawingEventArgs(track, m_FrameServer.Metadata.TrackManager.Id));
        }
        private void mnuPastePic_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsImage())
                return;

            Image img = Clipboard.GetImage();
            if (img == null)
                return;

            Bitmap bmp = new Bitmap(img);

            BeforeAddImageDrawing();
            if (m_FrameServer.Metadata.HitKeyframe == null)
                return;

            AbstractDrawing drawing = new DrawingBitmap(m_FrameServer.VideoReader.Current.Timestamp, m_FrameServer.VideoReader.Info.AverageTimeStampsPerFrame, bmp);

            if (drawing != null && DrawingAdding != null)
                DrawingAdding(this, new DrawingEventArgs(drawing, m_FrameServer.Metadata.HitKeyframe.Id));
        }
        #endregion

        #region Drawings Menus
        private void mnuConfigureDrawing_Click(object sender, EventArgs e)
        {
            Metadata metadata = m_FrameServer.Metadata;
            Keyframe kf = metadata.HitKeyframe;
            IDecorable drawing = metadata.HitDrawing as IDecorable;
            if (drawing == null || drawing.DrawingStyle == null || drawing.DrawingStyle.Elements.Count == 0)
                return;

            var drawingId = metadata.HitDrawing.Id;
            var managerId = metadata.FindManagerId(metadata.HitDrawing);
            var memento = new HistoryMementoModifyDrawing(metadata, managerId, drawingId, metadata.HitDrawing.Name, SerializationFilter.Style);
            
            FormConfigureDrawing2 fcd = new FormConfigureDrawing2(drawing, DoInvalidate);
            FormsHelper.Locate(fcd);
            fcd.ShowDialog();

            if (fcd.DialogResult == DialogResult.OK)
            {
                memento.UpdateCommandName(drawing.Name);
                m_FrameServer.HistoryStack.PushNewCommand(memento);

                // If this was a singleton drawing also update the tool-level preset.
                if (metadata.HitDrawing is DrawingCoordinateSystem)
                {
                    ToolManager.SetStylePreset("CoordinateSystem", ((DrawingCoordinateSystem)metadata.HitDrawing).DrawingStyle);
                    ToolManager.SavePresets();
                }
                else if (metadata.HitDrawing is DrawingTestGrid)
                {
                    ToolManager.SetStylePreset("TestGrid", ((DrawingTestGrid)metadata.HitDrawing).DrawingStyle);
                    ToolManager.SavePresets();
                }
                else if (metadata.HitDrawing is DrawingNumberSequence)
                {
                    ToolManager.SetStylePreset("NumberSequence", ((DrawingNumberSequence)metadata.HitDrawing).DrawingStyle);
                    ToolManager.SavePresets();
                }
            }

            fcd.Dispose();
            DoInvalidate();
            UpdateFramesMarkers();
        }
        private void mnuSetStyleAsDefault_Click(object sender, EventArgs e)
        {
            // Assign the style of the active drawing to the drawing tool that generated it.
            Keyframe kf = m_FrameServer.Metadata.HitKeyframe;
            IDecorable drawing = m_FrameServer.Metadata.HitDrawing as IDecorable;
            if (drawing == null || drawing.DrawingStyle == null || drawing.DrawingStyle.Elements.Count == 0)
                return;

            ToolManager.SetStylePreset(m_FrameServer.Metadata.HitDrawing, drawing.DrawingStyle);
            ToolManager.SavePresets();

            UpdateCursor();
        }
        private void mnuVisibilityAlways_Click(object sender, EventArgs e)
        {
            if (mnuVisibilityAlways.Checked)
                return;

            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            Guid managerId = m_FrameServer.Metadata.FindManagerId(drawing);
            HistoryMemento memento = new HistoryMementoModifyDrawing(m_FrameServer.Metadata, managerId, drawing.Id, drawing.Name, SerializationFilter.Fading);
            m_FrameServer.HistoryStack.PushNewCommand(memento);
            
            drawing.InfosFading.AlwaysVisible = true;
            drawing.InfosFading.UseDefault = false;
            
            mnuVisibilityAlways.Checked = true;
            mnuVisibilityDefault.Checked = false;
            mnuVisibilityCustom.Checked = false;
            DoInvalidate();
        }
        private void mnuVisibilityDefault_Click(object sender, EventArgs e)
        {
            if (mnuVisibilityDefault.Checked)
                return;

            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            Guid managerId = m_FrameServer.Metadata.FindManagerId(drawing);
            HistoryMemento memento = new HistoryMementoModifyDrawing(m_FrameServer.Metadata, managerId, drawing.Id, drawing.Name, SerializationFilter.Fading);
            m_FrameServer.HistoryStack.PushNewCommand(memento);
            
            drawing.InfosFading.AlwaysVisible = false;
            drawing.InfosFading.UseDefault = true;
            
            mnuVisibilityAlways.Checked = false;
            mnuVisibilityDefault.Checked = true;
            mnuVisibilityCustom.Checked = false;
            DoInvalidate();
        }
        private void mnuVisibilityCustom_Click(object sender, EventArgs e)
        {
            if (mnuVisibilityCustom.Checked)
            {
                mnuVisibilityConfigure_Click(sender, e);
                return;
            }
            
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            Guid managerId = m_FrameServer.Metadata.FindManagerId(drawing);
            HistoryMemento memento = new HistoryMementoModifyDrawing(m_FrameServer.Metadata, managerId, drawing.Id, drawing.Name, SerializationFilter.Fading);
            m_FrameServer.HistoryStack.PushNewCommand(memento);

            drawing.InfosFading.AlwaysVisible = false;
            drawing.InfosFading.UseDefault = false;

            mnuVisibilityAlways.Checked = false;
            mnuVisibilityDefault.Checked = false;
            mnuVisibilityCustom.Checked = true;
            DoInvalidate();

            // Go to configuration immediately.
            mnuVisibilityConfigure_Click(sender, e);
        }
        private void mnuVisibilityConfigure_Click(object sender, EventArgs e)
        {
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;

            FormConfigureVisibility f = new FormConfigureVisibility(drawing, pbSurfaceScreen);
            FormsHelper.Locate(f);
            f.ShowDialog();
            f.Dispose();
            DoInvalidate();
        }

        private void mnuGotoKeyframe_Click(object sender, EventArgs e)
        {
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            if(drawing.InfosFading == null)
                return;
            
            long target = drawing.InfosFading.ReferenceTimestamp;
            m_iFramesToDecode = 1;
            ShowNextFrame(target, true);
            UpdatePositionUI();
            ActivateKeyframe(m_iCurrentPosition);
        }
        private void mnuDrawingTrackingToggle_Click(object sender, EventArgs e)
        {
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            
            // Tracking is not compatible with custom decoding size, force the use of the original size.
            CheckCustomDecodingSize(true);
            ShowNextFrame(m_iCurrentPosition, true);
            ToggleTrackingCommand.Execute(drawing);
            RefreshImage();
        }

        private void mnuDrawingTrackingConfigure_Click(object sender, EventArgs e)
        {

        }

        private void mnuCutDrawing_Click(object sender, EventArgs e)
        {
            CutDrawing();
        }

        private void CutDrawing()
        {
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            if (drawing == null || !drawing.IsCopyPasteable)
                return;

            Guid managerId = m_FrameServer.Metadata.FindManagerId(m_FrameServer.Metadata.HitDrawing);
            AbstractDrawingManager manager = m_FrameServer.Metadata.GetDrawingManager(managerId);
            string data = DrawingSerializer.SerializeMemento(m_FrameServer.Metadata, manager.GetDrawing(drawing.Id), SerializationFilter.KVA, false);

            DrawingClipboard.Put(data, drawing.GetCopyPoint(), drawing.Name);
            
            if (DrawingDeleting != null)
                DrawingDeleting(this, new DrawingEventArgs(drawing, managerId));

            OnPoke();
        }

        private void mnuCopyDrawing_Click(object sender, EventArgs e)
        {
            CopyDrawing();
        }

        private void CopyDrawing()
        {
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            if (drawing == null || !drawing.IsCopyPasteable)
                return;

            Guid managerId = m_FrameServer.Metadata.FindManagerId(m_FrameServer.Metadata.HitDrawing);
            AbstractDrawingManager manager = m_FrameServer.Metadata.GetDrawingManager(managerId);
            string data = DrawingSerializer.SerializeMemento(m_FrameServer.Metadata, manager.GetDrawing(drawing.Id), SerializationFilter.KVA, false);

            DrawingClipboard.Put(data, drawing.GetCopyPoint(), drawing.Name);

            OnPoke();
        }

        private void mnuPasteDrawing_Click(object sender, EventArgs e)
        {
            PasteDrawing(false);
        }
        private void PasteDrawing(bool inPlace)
        {
            string data = DrawingClipboard.Content;
            if (data == null)
                return;

            AbstractDrawing drawing = DrawingSerializer.DeserializeMemento(data, m_FrameServer.Metadata);
            if (drawing == null || !drawing.IsCopyPasteable)
                return;

            // Note: the keyframe we used to copy from may not exist anymore. In this case we create a new keyframe.
            Guid managerId = m_FrameServer.Metadata.FindManagerId(drawing);
            if (managerId == Guid.Empty && m_FrameServer.Metadata.IsAttachedDrawing(drawing))
            {
                AddKeyframe();
                Keyframe kf = m_FrameServer.Metadata.HitKeyframe;
                managerId = kf.Id;
            }

            drawing.AfterCopy();
            
            if (!inPlace)
            {
                // Relocate the drawing under the mouse based on relative motion since the "copy" or "cut" action.
                float dx = m_DescaledMouse.X - DrawingClipboard.Position.X;
                float dy = m_DescaledMouse.Y - DrawingClipboard.Position.Y;
                drawing.MoveDrawing(dx, dy, Keys.None, m_FrameServer.Metadata.ImageTransform.Zooming);
                log.DebugFormat("Pasted drawing [{0}] under the mouse.", DrawingClipboard.Name);
            }
            else
            {
                log.DebugFormat("Pasted drawing [{0}] in place.", DrawingClipboard.Name);
            }

            if (DrawingAdding != null)
                DrawingAdding(this, new DrawingEventArgs(drawing, managerId));
        }
        
        private void mnuDeleteDrawing_Click(object sender, EventArgs e)
        {
            DeleteSelectedDrawing();
        }
        private void DeleteSelectedDrawing()
        {
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            if(drawing == null)
                return;
            
            if(drawing is AbstractMultiDrawing)
            {
                AbstractMultiDrawing manager = drawing as AbstractMultiDrawing;

                if (MultiDrawingItemDeleting != null)
                    MultiDrawingItemDeleting(this, new MultiDrawingItemEventArgs(manager.SelectedItem, manager));
            }
            else
            {
                Guid managerId = m_FrameServer.Metadata.FindManagerId(m_FrameServer.Metadata.HitDrawing);
                if (DrawingDeleting != null)
                    DrawingDeleting(this, new DrawingEventArgs(drawing, managerId));
            }
        }
        #endregion
        
        #region Trajectory tool menus
        private void mnuDeleteTrajectory_Click(object sender, EventArgs e)
        {
            AbstractDrawing drawing = m_FrameServer.Metadata.HitDrawing;
            if (drawing == null || !(drawing is DrawingTrack))
                return;

            if (DrawingDeleting != null)
                DrawingDeleting(this, new DrawingEventArgs(drawing, m_FrameServer.Metadata.TrackManager.Id));

            // Trigger a refresh of the export to spreadsheet menu, in case we don't have any more trajectory left to export.
            OnPoke();
            CheckCustomDecodingSize(false);
        }
        private void mnuConfigureTrajectory_Click(object sender, EventArgs e)
        {
            DrawingTrack track = m_FrameServer.Metadata.HitDrawing as DrawingTrack;
            if(track == null)
                return;

            // Note that we use SerializationFilter.KVA to backup all data as the dialog allows to modify not only style option but also tracker parameters.
            HistoryMementoModifyDrawing memento = new HistoryMementoModifyDrawing(m_FrameServer.Metadata, m_FrameServer.Metadata.TrackManager.Id, track.Id, track.Name, SerializationFilter.KVA);

            formConfigureTrajectoryDisplay fctd = new formConfigureTrajectoryDisplay(track, m_FrameServer.Metadata, m_FrameServer.CurrentImage, m_iCurrentPosition, DoInvalidate);
            fctd.StartPosition = FormStartPosition.CenterScreen;
            fctd.ShowDialog();

            if (fctd.DialogResult == DialogResult.OK)
            {
                memento.UpdateCommandName(track.Name);
                m_FrameServer.HistoryStack.PushNewCommand(memento);
            }

            fctd.Dispose();
            DoInvalidate();
            UpdateFramesMarkers();
        }
        private void DisplayClosestFrame(Point p, List<AbstractTrackPoint> trackPoints, float timeScale, bool use3D)
        {
            //--------------------------------------------------------------------------
            // This is where the interactivity of the trajectory is done.
            // The user has draged or clicked the trajectory, we find the closest point
            // and we update to the corresponding frame.
            //--------------------------------------------------------------------------

            // Compute the 3D distance (x,y,t) of each point in the path.
            
            float minDistance = float.MaxValue;
            int closestPointIndex = 0;

            if (use3D)
            {
                // Find closest location on screen in 3D (X, Y, T).
                for (int i = 0; i < trackPoints.Count; i++)
                {
                    float dx = p.X - trackPoints[i].X;
                    float dy = p.Y - trackPoints[i].Y;
                    float dt = m_iCurrentPosition - trackPoints[i].T;
                    dt /= timeScale;

                    float dist = (float)Math.Sqrt((dx * dx) + (dy * dy) + (dt * dt));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPointIndex = i;
                    }
                }
            }
            else
            {
                // Find closest location on screen in 2D.
                for (int i = 0; i < trackPoints.Count; i++)
                {
                    float dx = p.X - trackPoints[i].X;
                    float dy = p.Y - trackPoints[i].Y;
                    float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPointIndex = i;
                    }
                }
            }

            // move to corresponding timestamp.
            m_iFramesToDecode = 1;
            ShowNextFrame(trackPoints[closestPointIndex].T, true);
            UpdatePositionUI();
        }
        #endregion

        #region Magnifier Menus
        private void mnuMagnifierQuit_Click(object sender, EventArgs e)
        {
            DisableMagnifier();
            DoInvalidate();
        }
        private void mnuMagnifierDirect_Click(object sender, EventArgs e)
        {
            // Use position and magnification to Direct Zoom.
            // Go to direct zoom, at magnifier zoom factor, centered on same point as magnifier.
            m_FrameServer.ImageTransform.Zoom = m_FrameServer.Metadata.Magnifier.Zoom;
            m_FrameServer.ImageTransform.UpdateZoomWindow(m_FrameServer.Metadata.Magnifier.Center, false);
            DisableMagnifier();
            ToastZoom();
            
            ResizeUpdate(true);
        }
        private void mnuMagnifierFreeze_Click(object sender, EventArgs e)
        {
            Magnifier m = m_FrameServer.Metadata.Magnifier;
            if (m.Frozen)
                m.Unfreeze();
            else
                m.Freeze(m_FrameServer.CurrentImage);

            DoInvalidate();
        }

        private void mnuMagnifierTrack_Click(object sender, EventArgs e)
        {
            ITrackable drawing = m_FrameServer.Metadata.Magnifier as ITrackable;
            
            // Tracking is not compatible with custom decoding size, force the use of the original size.
            CheckCustomDecodingSize(true);
            ShowNextFrame(m_iCurrentPosition, true);
            ToggleTrackingCommand.Execute(drawing);
        }
        
        private void DisableMagnifier()
        {
            // Revert to no magnification.
            m_FrameServer.Metadata.Magnifier.Mode = MagnifierMode.Inactive;
            SetCursor(m_PointerTool.GetCursor(0));
        }
        #endregion
        
        #endregion
        
        #region DirectZoom
        private void ResetZoom(bool toast)
        {
            m_FrameServer.ImageTransform.ResetZoom();
            zoomHelper.Value = 1.0f;
            
            m_PointerTool.SetZoomLocation(m_FrameServer.ImageTransform.ZoomWindow.Location);
            
            if(toast)
                ToastZoom();

            ReportForSyncMerge();
            ResizeUpdate(true);
        }
        private void IncreaseDirectZoom(Point mouseLocation)
        {
            if (m_FrameServer.Metadata.Magnifier.Mode != MagnifierMode.Inactive)
                DisableMagnifier();

            zoomHelper.Increase();
            m_FrameServer.ImageTransform.Zoom = zoomHelper.Value;
            AfterZoomChange(mouseLocation);
        }
        private void DecreaseDirectZoom(Point mouseLocation)
        {
            if (!m_FrameServer.ImageTransform.Zooming)
            {
                // If we are already at the lowest zoom level, recenter the window.
                ResetZoom(false);
                return;
            }

            zoomHelper.Decrease();
            m_FrameServer.ImageTransform.Zoom = zoomHelper.Value;
            AfterZoomChange(mouseLocation);
        }
        private void AfterZoomChange(Point mouseLocation)
        {
            // Mouse location is given in the system of the picture box control.
            m_FrameServer.ImageTransform.UpdateZoomWindow(mouseLocation, false);
            m_PointerTool.SetZoomLocation(m_FrameServer.ImageTransform.ZoomWindow.Location);
            ToastZoom();
            UpdateCursor();
            ReportForSyncMerge();
            ResizeUpdate(true);
        }
        #endregion
        
        #region Toasts
        private void ToastZoom()
        {
            string message = string.Format("Zoom:{0}", zoomHelper.GetLabel());
            m_MessageToaster.SetDuration(750);
            m_MessageToaster.Show(message);
        }
        #endregion

        #region Synchronisation specifics
        private void AfterSyncAlphaChange()
        {
            m_SyncMergeMatrix.Matrix00 = 1.0f;
            m_SyncMergeMatrix.Matrix11 = 1.0f;
            m_SyncMergeMatrix.Matrix22 = 1.0f;
            m_SyncMergeMatrix.Matrix33 = m_SyncAlpha;
            m_SyncMergeMatrix.Matrix44 = 1.0f;
            m_SyncMergeImgAttr.SetColorMatrix(m_SyncMergeMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        }
        private void IncreaseSyncAlpha()
        {
            if(!m_bSyncMerge)
                return;
            m_SyncAlpha = Math.Max(m_SyncAlpha - 0.1f, 0.0f);
            AfterSyncAlphaChange();
            DoInvalidate();
        }
        private void DecreaseSyncAlpha()
        {
            if(!m_bSyncMerge)
                return;
            m_SyncAlpha = Math.Min(m_SyncAlpha + 0.1f, 1.0f);
            AfterSyncAlphaChange();
            DoInvalidate();
        }
        private void ReportForSyncMerge()
        {
            if(!m_bSynched)
                return;
            
            // If we are not actually merging, we don't need to clone and send the image.
            // But we still need to report to the screen manager to trigger sync operations.
            Bitmap img = null;
            
            if(m_bSyncMerge && m_FrameServer.CurrentImage != null)
            {
                // We have to re-apply the transformations here, because when drawing in this screen we draw directly on the canvas.
                // (there is no intermediate image that we could reuse here, this might be a future optimization).
                // We need to clone it anyway, so we might aswell do the transform.
                img = CloneTransformedImage();
            }

            if (ImageChanged != null)
                ImageChanged(this, new EventArgs<Bitmap>(img));
        }
        private Bitmap CloneTransformedImage()
        {
            // TODO: try to render unscaled here as well when possible.
            Size copySize = m_viewportManipulator.RenderingSize;
            Bitmap copy = new Bitmap(copySize.Width, copySize.Height);
            Graphics g = Graphics.FromImage(copy);
            
            Rectangle rDst;
            if(m_FrameServer.Metadata.Mirrored)
                rDst = new Rectangle(copySize.Width, 0, -copySize.Width, copySize.Height);
            else
                rDst = new Rectangle(0, 0, copySize.Width, copySize.Height);
            
            if(m_viewportManipulator.MayDrawUnscaled && m_FrameServer.VideoReader.CanDrawUnscaled)
                g.DrawImage(m_FrameServer.CurrentImage, rDst, m_FrameServer.ImageTransform.ZoomWindowInDecodedImage, GraphicsUnit.Pixel);
            else
                g.DrawImage(m_FrameServer.CurrentImage, rDst, m_FrameServer.ImageTransform.ZoomWindow, GraphicsUnit.Pixel);
                
            return copy;
        }
        #endregion
        
        #region VideoFilters Management
        private void EnableDisableAllPlayingControls(bool _bEnable)
        {
            // Disable playback controls and some other controls for the case
            // of a one-frame rendering. (mosaic, single image)
            
            if(m_FrameServer.Loaded && !m_FrameServer.VideoReader.CanChangeWorkingZone)
                EnableDisableWorkingZoneControls(false);
            else
                EnableDisableWorkingZoneControls(_bEnable);
            
            buttonGotoFirst.Enabled = _bEnable;
            buttonGotoLast.Enabled = _bEnable;
            buttonGotoNext.Enabled = _bEnable;
            buttonGotoPrevious.Enabled = _bEnable;
            buttonPlay.Enabled = _bEnable;
            
            lblSpeedTuner.Enabled = _bEnable;
            trkFrame.EnableDisable(_bEnable);

            trkFrame.Enabled = _bEnable;
            trkSelection.Enabled = _bEnable;
            sldrSpeed.Enabled = _bEnable;
            
            btnRafale.Enabled = _bEnable;
            btnSaveVideo.Enabled = _bEnable;
            btnDiaporama.Enabled = _bEnable;
            btnPausedVideo.Enabled = _bEnable;
            
            mnuDirectTrack.Enabled = _bEnable;
            mnuTimeOrigin.Enabled = _bEnable;
        }
        private void EnableDisableWorkingZoneControls(bool _bEnable)
        {
            btnSetHandlerLeft.Enabled = _bEnable;
            btnSetHandlerRight.Enabled = _bEnable;
            btnHandlersReset.Enabled = _bEnable;
            btn_HandlersLock.Enabled = _bEnable;
            btnTimeOrigin.Enabled = _bEnable;
            trkSelection.EnableDisable(_bEnable);
        }
        private void EnableDisableSnapshot(bool _bEnable)
        {
            btnSnapShot.Enabled = _bEnable;
        }
        private void EnableDisableDrawingTools(bool _bEnable)
        {
            foreach(ToolStripItem tsi in stripDrawingTools.Items)
            {
                tsi.Enabled = _bEnable;
            }
        }
        #endregion
        
        #region Export images and videos

        /// <summary>
        /// Export the current frame with drawings to the clipboard.
        /// </summary>
        private void CopyImageToClipboard()
        {
            if (!m_FrameServer.Loaded || m_FrameServer.CurrentImage == null)
                return;

            StopPlaying();
            OnPauseAsked();

            Bitmap outputImage = GetFlushedImage();
            Clipboard.SetImage(outputImage);
            outputImage.Dispose();
        }

        /// <summary>
        /// Export the current frame with drawings to a file.
        /// </summary>
        private void btnSnapShot_Click(object sender, EventArgs e)
        {
            if (!m_FrameServer.Loaded || m_FrameServer.CurrentImage == null)
                return;
            
            StopPlaying();
            OnPauseAsked();

            if (videoFilterIsActive)
            {
                if (m_FrameServer.Metadata.ActiveVideoFilter.CanExportImage)
                    m_FrameServer.Metadata.ActiveVideoFilter.ExportImage(this);
            }
            else
            {
                try
                {
                    SaveFileDialog dlgSave = new SaveFileDialog();
                    dlgSave.Title = ScreenManagerLang.Generic_SaveImage;
                    dlgSave.RestoreDirectory = true;
                    dlgSave.Filter = FilesystemHelper.SaveImageFilter();
                    dlgSave.FilterIndex = FilesystemHelper.GetFilterIndex(dlgSave.Filter, PreferencesManager.PlayerPreferences.ImageFormat);
                
                    if(videoFilterIsActive)
                        dlgSave.FileName = Path.GetFileNameWithoutExtension(m_FrameServer.VideoReader.FilePath);
                    else
                        dlgSave.FileName = BuildFilename(m_FrameServer.VideoReader.FilePath, m_iCurrentPosition, PreferencesManager.PlayerPreferences.TimecodeFormat);
                
                    if (dlgSave.ShowDialog() == DialogResult.OK)
                    {
                        Bitmap outputImage = GetFlushedImage();
                        ImageHelper.Save(dlgSave.FileName, outputImage);
                        outputImage.Dispose();

                        PreferencesManager.PlayerPreferences.ImageFormat = FilesystemHelper.GetImageFormat(dlgSave.FileName);
                        PreferencesManager.Save();

                        m_FrameServer.AfterSave();
                    }
                }
                catch (Exception exp)
                {
                    log.Error(exp.StackTrace);
                }
            }
        }

        private void btnSaveAnnotations_Click(object sender, EventArgs e)
        {
            if (!m_FrameServer.Loaded)
                return;

            StopPlaying();
            OnPauseAsked();

            SaveAnnotations();

            m_iFramesToDecode = 1;
            ShowNextFrame(m_iSelStart, true);
            ActivateKeyframe(m_iCurrentPosition, true);
        }

        private void btnSaveAnnotationsAs_Click(object sender, EventArgs e)
        {
            if (!m_FrameServer.Loaded)
                return;

            StopPlaying();
            OnPauseAsked();

            SaveAnnotationsAs();

            m_iFramesToDecode = 1;
            ShowNextFrame(m_iSelStart, true);
            ActivateKeyframe(m_iCurrentPosition, true);
        }

        /// <summary>
        /// Export the current video to a new file, with drawings painted on.
        /// </summary>
        private void btnSaveVideo_Click(object sender, EventArgs e)
        {
            if(!m_FrameServer.Loaded)
                return;
            
            StopPlaying();
            OnPauseAsked();

            ExportVideo();

            m_iFramesToDecode = 1;
            ShowNextFrame(m_iSelStart, true);
            ActivateKeyframe(m_iCurrentPosition, true);
        }

        /// <summary>
        /// Triggers the rafale export pipeline.
        /// Ultimately this enumerates frames and comes back to GetFlushedImage(VideoFrame, Bitmap).
        /// </summary>
        private void btnRafale_Click(object sender, EventArgs e)
        {
            //---------------------------------------------------------------------------------
            // Workflow:
            // 1. FormRafaleExport  : configure the export, calls:
            // 2. FileSaveDialog    : choose the file name, then:
            // 3. FormFramesExport   : Progress bar holder and updater, calls:
            // 4. SaveImageSequence (below): Perform the real work.
            //---------------------------------------------------------------------------------

            if (!m_FrameServer.Loaded || m_FrameServer.CurrentImage == null)
                return;

            StopPlaying();
            OnPauseAsked();

            FormRafaleExport fre = new FormRafaleExport(
                this,
                m_FrameServer.Metadata,
                m_FrameServer.VideoReader.FilePath,
                m_FrameServer.VideoReader.Info);

            fre.ShowDialog();
            fre.Dispose();
            m_FrameServer.AfterSave();

            m_iFramesToDecode = 1;
            ShowNextFrame(m_iSelStart, true);
            ActivateKeyframe(m_iCurrentPosition, true);
        }

        /// <summary>
        /// Triggers the special video export pipeline.
        /// Ultimately this enumerates frames and comes back to GetFlushedImage(VideoFrame, Bitmap).
        /// </summary>
        private void btnDiaporama_Click(object sender, EventArgs e)
        {
            if (!m_FrameServer.Loaded || m_FrameServer.CurrentImage == null)
                return;
                
            bool diaporama = sender == btnDiaporama;
            
            StopPlaying();
            OnPauseAsked();
            
            if(m_FrameServer.Metadata.Keyframes.Count < 1)
            {
                string error = diaporama ? ScreenManagerLang.Error_SavePausedVideo : ScreenManagerLang.Error_SavePausedVideo;
                MessageBox.Show(ScreenManagerLang.Error_SaveDiaporama_NoKeyframes.Replace("\\n", "\n"),
                                error,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return;
            }
            
            saveInProgress = true;
            m_FrameServer.SaveDiaporama(GetFlushedImage, diaporama);
            saveInProgress = false;
            
            m_iFramesToDecode = 1;
            ShowNextFrame(m_iSelStart, true);
            ActivateKeyframe(m_iCurrentPosition, true);
        }

        /// <summary>
        /// Save to the current KVA if it exists, ask for a filename if not.
        /// </summary>
        private void SaveAnnotations()
        {
            MetadataSerializer serializer = new MetadataSerializer();
            serializer.UserSave(m_FrameServer.Metadata, m_FrameServer.VideoReader.FilePath);
        }

        /// <summary>
        /// Save a KVA to a new file.
        /// </summary>
        private void SaveAnnotationsAs()
        {
            MetadataSerializer serializer = new MetadataSerializer();
            serializer.UserSaveAs(m_FrameServer.Metadata, m_FrameServer.VideoReader.FilePath);
        }

        /// <summary>
        /// Save the video to a new file.
        /// </summary>
        public void ExportVideo()
        {
            saveInProgress = true;
            if (videoFilterIsActive)
            {
                if (m_FrameServer.Metadata.ActiveVideoFilter.CanExportVideo)
                    m_FrameServer.Metadata.ActiveVideoFilter.ExportVideo(this);
            }
            else
            {
                m_FrameServer.SaveVideo(timeMapper.GetInterval(sldrSpeed.Value), slowMotion * 100, GetFlushedImage);
            }
            saveInProgress = false;
        }


        /// <summary>
        /// Save several images at once. Called back for rafale export.
        /// </summary>
        public void SaveImageSequence(BackgroundWorker bgWorker, string filepath, long interval, bool keyframesOnly, int total)
        {
            // This function works similarly to the video export in FrameServerPlayer.EnumerateImages.
            // The images are saved at original video size.
            int frameCount = keyframesOnly ? m_FrameServer.Metadata.Keyframes.Count : total;
            int iCurrent = 0;

            m_FrameServer.VideoReader.BeforeFrameEnumeration();

            // We do not use the cached Bitmap in keyframe.FullImage because it is saved at the display size of the time of the creation of the keyframe.
            IEnumerable<VideoFrame> frames = keyframesOnly ? m_FrameServer.VideoReader.FrameEnumerator() : m_FrameServer.VideoReader.FrameEnumerator(interval);

            foreach (VideoFrame vf in frames)
            {
                Bitmap output = null;

                try
                {
                    if (vf == null)
                    {
                        log.Error("Frame enumerator yield null.");
                        break;
                    }

                    output = new Bitmap(vf.Image.Width, vf.Image.Height, vf.Image.PixelFormat);

                    bool onKeyframe = GetFlushedImage(vf, output);
                    bool savable = onKeyframe || !keyframesOnly;

                    if (savable)
                    {
                        string filename = string.Format("{0}\\{1}{2}",
                            Path.GetDirectoryName(filepath),
                            BuildFilename(filepath, vf.Timestamp, PreferencesManager.PlayerPreferences.TimecodeFormat),
                            Path.GetExtension(filepath));

                        ImageHelper.Save(filename, output);
                    }

                    bgWorker.ReportProgress(iCurrent++, frameCount);
                }
                catch (Exception)
                {

                }
                finally
                {
                    if (output != null)
                        output.Dispose();
                }

            }

            m_FrameServer.VideoReader.AfterFrameEnumeration();
        }
        
        /// <summary>
        /// Returns the image currently on screen with all drawings flushed, including grids, magnifier, mirroring, etc.
        /// The resulting Bitmap will be at the same size as the image currently on screen.
        /// This is used to export individual images or get the image for dual video export.
        /// </summary>
        public Bitmap GetFlushedImage()
        {
            Size renderingSize = m_viewportManipulator.RenderingSize;
            Bitmap output = new Bitmap(renderingSize.Width, renderingSize.Height, PixelFormat.Format24bppRgb);
            output.SetResolution(m_FrameServer.CurrentImage.HorizontalResolution, m_FrameServer.CurrentImage.VerticalResolution);

            int keyframeIndex = m_FrameServer.Metadata.GetKeyframeIndex(m_iCurrentPosition);
            using (Graphics canvas = Graphics.FromImage(output))
                FlushOnGraphics(m_FrameServer.CurrentImage, canvas, output.Size, keyframeIndex, m_iCurrentPosition, m_FrameServer.ImageTransform);
            
            return output;
        }

        /// <summary>
        /// Paint the passed bitmap with the content of video frame passed in, plus the complete compositing pipeline.
        /// The painting is done without zoom. 
        /// The passed bitmap should have the same size as the video frame.
        /// This is used to export videos or sequence of images.
        /// Returns true if the passed frame is a keyframe.
        /// </summary>
        public bool GetFlushedImage(VideoFrame vf, Bitmap output)
        {
            if (vf.Image.Size != output.Size)
            {
                log.ErrorFormat("Exporting unscaled images: passed bitmap has the wrong size.");
                return false;
            }

            int keyframeIndex = m_FrameServer.Metadata.GetKeyframeIndex(vf.Timestamp);
            
            // Make sure the trackable drawings are on the right context.
            TrackDrawingsCommand.Execute(vf);

            using (Graphics canvas = Graphics.FromImage(output))
                FlushOnGraphics(vf.Image, canvas, output.Size, keyframeIndex, vf.Timestamp, m_FrameServer.ImageTransform.Identity);

            return keyframeIndex != -1;
        }

        /// <summary>
        /// Builds a file name with the current timecode and the extension.
        /// </summary>
        private string BuildFilename(string _FilePath, long _position, TimecodeFormat _timeCodeFormat)
        {
            TimecodeFormat tcf;
            if(_timeCodeFormat == TimecodeFormat.TimeAndFrames)
                tcf = TimecodeFormat.ClassicTime;
            else
                tcf = _timeCodeFormat;

            // Timecode string (Not relative to sync position)
            string suffix = m_FrameServer.TimeStampsToTimecode(_position, TimeType.UserOrigin, tcf, false);
            string maxSuffix = m_FrameServer.TimeStampsToTimecode(m_iSelEnd, TimeType.UserOrigin, tcf, false);

            switch (tcf)
            {
                case TimecodeFormat.Frames:
                case TimecodeFormat.Milliseconds:
                case TimecodeFormat.Microseconds:
                case TimecodeFormat.TenThousandthOfHours:
                case TimecodeFormat.HundredthOfMinutes:
                    
                    int iZerosToPad = maxSuffix.Length - suffix.Length;
                    for (int i = 0; i < iZerosToPad; i++)
                    {
                        // Add a leading zero.
                        suffix = suffix.Insert(0, "0");
                    }
                    break;
                default:
                    break;
            }

            // Reconstruct filename
            return Path.GetFileNameWithoutExtension(_FilePath) + "-" + suffix.Replace(':', '.');
        }
        #endregion
    }
}
