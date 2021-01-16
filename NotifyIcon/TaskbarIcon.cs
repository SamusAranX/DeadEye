// hardcodet.net NotifyIcon for WPF
// Copyright (c) 2009 - 2013 Philipp Sumi
// Contact and Information: http://www.hardcodet.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the Code Project Open License (CPOL);
// either version 1.0 of the License, or (at your option) any later
// version.
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE

using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using DeadEye.NotifyIcon.Interop;
using Point = DeadEye.NotifyIcon.Interop.Point;

namespace DeadEye.NotifyIcon {
    /// <summary>
    /// A WPF proxy to for a taskbar icon (NotifyIcon) that sits in the system's
    /// taskbar notification area ("system tray").
    /// </summary>
    public partial class TaskbarIcon : FrameworkElement, IDisposable {
        private readonly object lockObject = new object();

        #region Members

        /// <summary>
        /// Represents the current icon data.
        /// </summary>
        private NotifyIconData iconData;

        /// <summary>
        /// Receives messages from the taskbar icon.
        /// </summary>
        private readonly WindowMessageSink messageSink;

        /// <summary>
        /// An action that is being invoked if the
        /// <see cref="singleClickTimer"/> fires.
        /// </summary>
        private Action singleClickTimerAction;

        /// <summary>
        /// A timer that is used to differentiate between single
        /// and double clicks.
        /// </summary>
        private readonly Timer singleClickTimer;

        /// <summary>
        /// The time we should wait for a double click.
        /// </summary>
        private int DoubleClickWaitTime => this.NoLeftClickDelay ? 0 : WinApi.GetDoubleClickTime();

        /// <summary>
        /// A timer that is used to close open balloon tooltips.
        /// </summary>
        private readonly Timer balloonCloseTimer;

        /// <summary>
        /// Indicates whether the taskbar icon has been created or not.
        /// </summary>
        public bool IsTaskbarIconCreated { get; private set; }

        /// <summary>
        /// Indicates whether custom tooltips are supported, which depends
        /// on the OS. Windows Vista or higher is required in order to
        /// support this feature.
        /// </summary>
        public bool SupportsCustomToolTips => this.messageSink.Version == NotifyIconVersion.Vista;

        /// <summary>
        /// Checks whether a non-tooltip popup is currently opened.
        /// </summary>
        private bool IsPopupOpen {
            get {
                var popup = this.TrayPopupResolved;
                var menu = this.ContextMenu;
                var balloon = this.CustomBalloon;

                return popup != null && popup.IsOpen ||
                       menu != null && menu.IsOpen ||
                       balloon != null && balloon.IsOpen;
            }
        }

        #endregion

        #region Construction

        /// <summary>
        /// Initializes the taskbar icon and registers a message listener
        /// in order to receive events from the taskbar area.
        /// </summary>
        public TaskbarIcon() {
            // using dummy sink in design mode
            this.messageSink = Util.IsDesignMode
                ? WindowMessageSink.CreateEmpty()
                : new WindowMessageSink(NotifyIconVersion.Win95);

            // init icon data structure
            this.iconData = NotifyIconData.CreateDefault(this.messageSink.MessageWindowHandle);

            // create the taskbar icon
            this.CreateTaskbarIcon();

            // register event listeners
            this.messageSink.MouseEventReceived += this.OnMouseEvent;
            this.messageSink.TaskbarCreated += this.OnTaskbarCreated;
            this.messageSink.ChangeToolTipStateRequest += this.OnToolTipChange;
            this.messageSink.BalloonToolTipChanged += this.OnBalloonToolTipChanged;

            // init single click / balloon timers
            this.singleClickTimer = new Timer(this.DoSingleClickAction);
            this.balloonCloseTimer = new Timer(this.CloseBalloonCallback);

            // register listener in order to get notified when the application closes
            if (Application.Current != null) {
                Application.Current.Exit += this.OnExit;
            }
        }

        #endregion

        #region Custom Balloons
        /// <summary>
        /// A delegate to handle customer popup positions.
        /// </summary>
        public delegate Point GetCustomPopupPosition();

        /// <summary>
        /// Specify a custom popup position
        /// </summary>
        public GetCustomPopupPosition CustomPopupPosition { get; set; }

        /// <summary>
        /// Returns the location of the system tray
        /// </summary>
        /// <returns>Point</returns>
        public Point GetPopupTrayPosition() => TrayInfo.GetTrayLocation();

        /// <summary>
        /// Shows a custom control as a tooltip in the tray location.
        /// </summary>
        /// <param name="balloon"></param>
        /// <param name="animation">An optional animation for the popup.</param>
        /// <param name="timeout">The time after which the popup is being closed.
        /// Submit null in order to keep the balloon open indefinitely
        /// </param>
        /// <exception cref="ArgumentNullException">If <paramref name="balloon"/>
        /// is a null reference.</exception>
        public void ShowCustomBalloon(UIElement balloon, PopupAnimation animation, int? timeout) {
            var dispatcher = this.GetDispatcher();
            if (!dispatcher.CheckAccess()) {
                var action = new Action(() => this.ShowCustomBalloon(balloon, animation, timeout));
                dispatcher.Invoke(DispatcherPriority.Normal, action);
                return;
            }

            if (balloon == null) throw new ArgumentNullException(nameof(balloon));
            if (timeout.HasValue && timeout < 500) {
                var msg = "Invalid timeout of {0} milliseconds. Timeout must be at least 500 ms";
                msg = string.Format(msg, timeout);
                throw new ArgumentOutOfRangeException(nameof(timeout), msg);
            }

            this.EnsureNotDisposed();

            // make sure we don't have an open balloon
            lock (this.lockObject) {
                this.CloseBalloon();
            }

            // create an invisible popup that hosts the UIElement
            var popup = new Popup {
                AllowsTransparency = true
            };

            // provide the popup with the taskbar icon's data context
            this.UpdateDataContext(popup, null, this.DataContext);

            // don't animate by default - developers can use attached events or override
            popup.PopupAnimation = animation;

            // in case the balloon is cleaned up through routed events, the
            // control didn't remove the balloon from its parent popup when
            // if was closed the last time - just make sure it doesn't have
            // a parent that is a popup
            var parent = LogicalTreeHelper.GetParent(balloon) as Popup;
            if (parent != null) parent.Child = null;

            if (parent != null) {
                var msg = "Cannot display control [{0}] in a new balloon popup - that control already has a parent. You may consider creating new balloons every time you want to show one.";
                msg = string.Format(msg, balloon);
                throw new InvalidOperationException(msg);
            }

            popup.Child = balloon;

            //don't set the PlacementTarget as it causes the popup to become hidden if the
            //TaskbarIcon's parent is hidden, too...
            //popup.PlacementTarget = this;

            popup.Placement = PlacementMode.AbsolutePoint;
            popup.StaysOpen = true;

            var position = this.CustomPopupPosition != null ? this.CustomPopupPosition() : this.GetPopupTrayPosition();
            popup.HorizontalOffset = position.X - 1;
            popup.VerticalOffset = position.Y - 1;

            //store reference
            lock (this.lockObject) {
	            this.SetCustomBalloon(popup);
            }

            // assign this instance as an attached property
            TaskbarIcon.SetParentTaskbarIcon(balloon, this);

            // fire attached event
            TaskbarIcon.RaiseBalloonShowingEvent(balloon, this);

            // display item
            popup.IsOpen = true;

            if (timeout.HasValue) {
                // register timer to close the popup
                this.balloonCloseTimer.Change(timeout.Value, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Resets the closing timeout, which effectively
        /// keeps a displayed balloon message open until
        /// it is either closed programmatically through
        /// <see cref="CloseBalloon"/> or due to a new
        /// message being displayed.
        /// </summary>
        public void ResetBalloonCloseTimer() {
            if (this.IsDisposed) return;

            lock (this.lockObject) {
                //reset timer in any case
                this.balloonCloseTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Closes the current <see cref="TaskbarIcon.CustomBalloon"/>, if the
        /// property is set.
        /// </summary>
        public void CloseBalloon() {
            if (this.IsDisposed) return;

            var dispatcher = this.GetDispatcher();
            if (!dispatcher.CheckAccess()) {
                Action action = this.CloseBalloon;
                dispatcher.Invoke(DispatcherPriority.Normal, action);
                return;
            }

            lock (this.lockObject) {
                // reset timer in any case
                this.balloonCloseTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // reset old popup, if we still have one
                var popup = this.CustomBalloon;
                if (popup == null) {
                    return;
                }

                var element = popup.Child;

                // announce closing
                var eventArgs = TaskbarIcon.RaiseBalloonClosingEvent(element, this);
                if (!eventArgs.Handled) {
                    // if the event was handled, clear the reference to the popup,
                    // but don't close it - the handling code has to manage this stuff now

                    // close the popup
                    popup.IsOpen = false;

                    // remove the reference of the popup to the balloon in case we want to reuse
                    // the balloon (then added to a new popup)
                    popup.Child = null;

                    // reset attached property
                    if (element != null) TaskbarIcon.SetParentTaskbarIcon(element, null);
                }

                // remove custom balloon anyway
                this.SetCustomBalloon(null);
            }
        }

        /// <summary>
        /// Timer-invoke event which closes the currently open balloon and
        /// resets the <see cref="TaskbarIcon.CustomBalloon"/> dependency property.
        /// </summary>
        private void CloseBalloonCallback(object state) {
            if (this.IsDisposed) return;

            // switch to UI thread
            Action action = this.CloseBalloon;
            this.GetDispatcher().Invoke(action);
        }

        #endregion

        #region Process Incoming Mouse Events

        /// <summary>
        /// Processes mouse events, which are bubbled
        /// through the class' routed events, trigger
        /// certain actions (e.g. show a popup), or
        /// both.
        /// </summary>
        /// <param name="me">Event flag.</param>
        private void OnMouseEvent(MouseEvent me) {
            if (this.IsDisposed) return;

            switch (me) {
                case MouseEvent.MouseMove:
	                this.RaiseTrayMouseMoveEvent();
                    // immediately return - there's nothing left to evaluate
                    return;
                case MouseEvent.IconRightMouseDown:
	                this.RaiseTrayRightMouseDownEvent();
                    break;
                case MouseEvent.IconLeftMouseDown:
	                this.RaiseTrayLeftMouseDownEvent();
                    break;
                case MouseEvent.IconRightMouseUp:
	                this.RaiseTrayRightMouseUpEvent();
                    break;
                case MouseEvent.IconLeftMouseUp:
	                this.RaiseTrayLeftMouseUpEvent();
                    break;
                case MouseEvent.IconMiddleMouseDown:
	                this.RaiseTrayMiddleMouseDownEvent();
                    break;
                case MouseEvent.IconMiddleMouseUp:
	                this.RaiseTrayMiddleMouseUpEvent();
                    break;
                case MouseEvent.IconDoubleClick:
                    // cancel single click timer
                    this.singleClickTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    // bubble event
                    this.RaiseTrayMouseDoubleClickEvent();
                    break;
                case MouseEvent.BalloonToolTipClicked:
	                this.RaiseTrayBalloonTipClickedEvent();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(me), "Missing handler for mouse event flag: " + me);
            }

            // get mouse coordinates
            var cursorPosition = new Point();
            if (this.messageSink.Version == NotifyIconVersion.Vista) {
                // physical cursor position is supported for Vista and above
                WinApi.GetPhysicalCursorPos(ref cursorPosition);
            } else {
                WinApi.GetCursorPos(ref cursorPosition);
            }

            cursorPosition = TrayInfo.GetDeviceCoordinates(cursorPosition);

            var isLeftClickCommandInvoked = false;

            // show popup, if requested
            if (me.IsMatch(PopupActivation)) {
                if (me == MouseEvent.IconLeftMouseUp) {
                    // show popup once we are sure it's not a double click
                    this.singleClickTimerAction = () => {
                        this.LeftClickCommand.ExecuteIfEnabled(this.LeftClickCommandParameter, this.LeftClickCommandTarget ?? this);
                        this.ShowTrayPopup(cursorPosition);
                    };
                    this.singleClickTimer.Change(this.DoubleClickWaitTime, Timeout.Infinite);
                    isLeftClickCommandInvoked = true;
                } else {
                    // show popup immediately
                    this.ShowTrayPopup(cursorPosition);
                }
            }

            // show context menu, if requested
            if (me.IsMatch(MenuActivation)) {
                if (me == MouseEvent.IconLeftMouseUp) {
                    // show context menu once we are sure it's not a double click
                    this.singleClickTimerAction = () => {
                        this.LeftClickCommand.ExecuteIfEnabled(this.LeftClickCommandParameter, this.LeftClickCommandTarget ?? this);
                        this.ShowContextMenu(cursorPosition);
                    };
                    this.singleClickTimer.Change(this.DoubleClickWaitTime, Timeout.Infinite);
                    isLeftClickCommandInvoked = true;
                } else {
                    // show context menu immediately
                    this.ShowContextMenu(cursorPosition);
                }
            }

            // make sure the left click command is invoked on mouse clicks
            if (me == MouseEvent.IconLeftMouseUp && !isLeftClickCommandInvoked) {
                // show context menu once we are sure it's not a double click
                this.singleClickTimerAction =
                    () => {
                        this.LeftClickCommand.ExecuteIfEnabled(this.LeftClickCommandParameter, this.LeftClickCommandTarget ?? this);
                    };
                this.singleClickTimer.Change(this.DoubleClickWaitTime, Timeout.Infinite);
            }
        }

        #endregion

        #region ToolTips

        /// <summary>
        /// Displays a custom tooltip, if available. This method is only
        /// invoked for Windows Vista and above.
        /// </summary>
        /// <param name="visible">Whether to show or hide the tooltip.</param>
        private void OnToolTipChange(bool visible) {
            // if we don't have a tooltip, there's nothing to do here...
            if (this.TrayToolTipResolved == null) return;

            if (visible) {
                if (this.IsPopupOpen) {
                    // ignore if we are already displaying something down there
                    return;
                }

                var args = this.RaisePreviewTrayToolTipOpenEvent();
                if (args.Handled) return;

                this.TrayToolTipResolved.IsOpen = true;

                // raise attached event first
                if (this.TrayToolTip != null) TaskbarIcon.RaiseToolTipOpenedEvent(this.TrayToolTip);

                // bubble routed event
                this.RaiseTrayToolTipOpenEvent();
            } else {
                var args = this.RaisePreviewTrayToolTipCloseEvent();
                if (args.Handled) return;

                // raise attached event first
                if (this.TrayToolTip != null) TaskbarIcon.RaiseToolTipCloseEvent(this.TrayToolTip);

                this.TrayToolTipResolved.IsOpen = false;

                // bubble event
                this.RaiseTrayToolTipCloseEvent();
            }
        }

        /// <summary>
        /// Creates a <see cref="ToolTip"/> control that either
        /// wraps the currently set <see cref="TaskbarIcon.TrayToolTip"/>
        /// control or the <see cref="TaskbarIcon.ToolTipText"/> string.<br/>
        /// If <see cref="TaskbarIcon.TrayToolTip"/> itself is already
        /// a <see cref="ToolTip"/> instance, it will be used directly.
        /// </summary>
        /// <remarks>We use a <see cref="ToolTip"/> rather than
        /// <see cref="Popup"/> because there was no way to prevent a
        /// popup from causing cyclic open/close commands if it was
        /// placed under the mouse. ToolTip internally uses a Popup of
        /// its own, but takes advance of Popup's internal <see cref="UIElement.IsHitTestVisible"/>
        /// property which prevents this issue.</remarks>
        private void CreateCustomToolTip() {
            // check if the item itself is a tooltip
            var tt = TrayToolTip as ToolTip;

            if (tt == null && this.TrayToolTip != null) {
                // create an invisible wrapper tooltip that hosts the UIElement
                tt = new ToolTip {
                    Placement = PlacementMode.Mouse,
                    // do *not* set the placement target, as it causes the popup to become hidden if the
                    // TaskbarIcon's parent is hidden, too. At runtime, the parent can be resolved through
                    // the ParentTaskbarIcon attached dependency property:
                    // PlacementTarget = this;

                    // make sure the tooltip is invisible
                    HasDropShadow = false,
                    BorderThickness = new Thickness(0),
                    Background = System.Windows.Media.Brushes.Transparent,
                    // setting the 
                    StaysOpen = true,
                    Content = this.TrayToolTip
                };
            } else if (tt == null && !string.IsNullOrEmpty(ToolTipText)) {
                // create a simple tooltip for the ToolTipText string
                tt = new ToolTip {
                    Content = this.ToolTipText
                };
            }

            // the tooltip explicitly gets the DataContext of this instance.
            // If there is no DataContext, the TaskbarIcon assigns itself
            if (tt != null) {
	            this.UpdateDataContext(tt, null, this.DataContext);
            }

            // store a reference to the used tooltip
            this.SetTrayToolTipResolved(tt);
        }

        /// <summary>
        /// Sets tooltip settings for the class depending on defined
        /// dependency properties and OS support.
        /// </summary>
        private void WriteToolTipSettings() {
            const IconDataMembers flags = IconDataMembers.Tip;
            this.iconData.ToolTipText = this.ToolTipText;

            if (this.messageSink.Version == NotifyIconVersion.Vista) {
                // we need to set a tooltip text to get tooltip events from the
                // taskbar icon
                if (string.IsNullOrEmpty(this.iconData.ToolTipText) && this.TrayToolTipResolved != null) {
                    // if we have not tooltip text but a custom tooltip, we
                    // need to set a dummy value (we're displaying the ToolTip control, not the string)
                    this.iconData.ToolTipText = "ToolTip";
                }
            }

            // update the tooltip text
            Util.WriteIconData(ref this.iconData, NotifyCommand.Modify, flags);
        }

        #endregion

        #region Custom Popup

        /// <summary>
        /// Creates a <see cref="ToolTip"/> control that either
        /// wraps the currently set <see cref="TaskbarIcon.TrayToolTip"/>
        /// control or the <see cref="TaskbarIcon.ToolTipText"/> string.<br/>
        /// If <see cref="TaskbarIcon.TrayToolTip"/> itself is already
        /// a <see cref="ToolTip"/> instance, it will be used directly.
        /// </summary>
        /// <remarks>We use a <see cref="ToolTip"/> rather than
        /// <see cref="Popup"/> because there was no way to prevent a
        /// popup from causing cyclic open/close commands if it was
        /// placed under the mouse. ToolTip internally uses a Popup of
        /// its own, but takes advance of Popup's internal <see cref="UIElement.IsHitTestVisible"/>
        /// property which prevents this issue.</remarks>
        private void CreatePopup() {
            // check if the item itself is a popup
            var popup = this.TrayPopup as Popup;

            if (popup == null && this.TrayPopup != null) {
                // create an invisible popup that hosts the UIElement
                popup = new Popup {
                    AllowsTransparency = true,
                    // don't animate by default - developers can use attached events or override
                    PopupAnimation = PopupAnimation.None,
                    // the CreateRootPopup method outputs binding errors in the debug window because
                    // it tries to bind to "Popup-specific" properties in case they are provided by the child.
                    // We don't need that so just assign the control as the child.
                    Child = this.TrayPopup,
                    // do *not* set the placement target, as it causes the popup to become hidden if the
                    // TaskbarIcon's parent is hidden, too. At runtime, the parent can be resolved through
                    // the ParentTaskbarIcon attached dependency property:
                    // PlacementTarget = this;

                    Placement = PlacementMode.AbsolutePoint,
                    StaysOpen = false
                };
            }

            // the popup explicitly gets the DataContext of this instance.
            // If there is no DataContext, the TaskbarIcon assigns itself
            if (popup != null) {
	            this.UpdateDataContext(popup, null, this.DataContext);
            }

            // store a reference to the used tooltip
            this.SetTrayPopupResolved(popup);
        }

        /// <summary>
        /// Displays the <see cref="TaskbarIcon.TrayPopup"/> control if it was set.
        /// </summary>
        private void ShowTrayPopup(Point cursorPosition) {
            if (this.IsDisposed) return;

            // raise preview event no matter whether popup is currently set
            // or not (enables client to set it on demand)
            var args = this.RaisePreviewTrayPopupOpenEvent();
            if (args.Handled) return;

            if (this.TrayPopup == null) {
                return;
            }

            // use absolute position, but place the popup centered above the icon
            this.TrayPopupResolved.Placement = PlacementMode.AbsolutePoint;
            this.TrayPopupResolved.HorizontalOffset = cursorPosition.X;
            this.TrayPopupResolved.VerticalOffset = cursorPosition.Y;

            // open popup
            this.TrayPopupResolved.IsOpen = true;

            var handle = IntPtr.Zero;
            if (this.TrayPopupResolved.Child != null) {
                // try to get a handle on the popup itself (via its child)
                var source = (HwndSource)PresentationSource.FromVisual(this.TrayPopupResolved.Child);
                if (source != null) handle = source.Handle;
            }

            // if we don't have a handle for the popup, fall back to the message sink
            if (handle == IntPtr.Zero) handle = this.messageSink.MessageWindowHandle;

            // activate either popup or message sink to track deactivation.
            // otherwise, the popup does not close if the user clicks somewhere else
            WinApi.SetForegroundWindow(handle);

            // raise attached event - item should never be null unless developers
            // changed the CustomPopup directly...
            if (this.TrayPopup != null) TaskbarIcon.RaisePopupOpenedEvent(this.TrayPopup);

            // bubble routed event
            this.RaiseTrayPopupOpenEvent();
        }

        #endregion

        #region Context Menu

        /// <summary>
        /// Displays the <see cref="ContextMenu"/> if it was set.
        /// </summary>
        private void ShowContextMenu(Point cursorPosition) {
            if (this.IsDisposed) return;

            // raise preview event no matter whether context menu is currently set
            // or not (enables client to set it on demand)
            var args = this.RaisePreviewTrayContextMenuOpenEvent();
            if (args.Handled) return;

            if (this.ContextMenu == null) {
                return;
            }

            // use absolute positioning. We need to set the coordinates, or a delayed opening
            // (e.g. when left-clicked) opens the context menu at the wrong place if the mouse
            // is moved!
            this.ContextMenu.Placement = PlacementMode.AbsolutePoint;
            this.ContextMenu.HorizontalOffset = cursorPosition.X;
            this.ContextMenu.VerticalOffset = cursorPosition.Y;
            this.ContextMenu.IsOpen = true;

            var handle = IntPtr.Zero;

            // try to get a handle on the context itself
            var source = (HwndSource)PresentationSource.FromVisual(this.ContextMenu);
            if (source != null) {
                handle = source.Handle;
            }

            // if we don't have a handle for the popup, fall back to the message sink
            if (handle == IntPtr.Zero) handle = this.messageSink.MessageWindowHandle;

            // activate the context menu or the message window to track deactivation - otherwise, the context menu
            // does not close if the user clicks somewhere else. With the message window
            // fallback, the context menu can't receive keyboard events - should not happen though
            WinApi.SetForegroundWindow(handle);

            // bubble event
            this.RaiseTrayContextMenuOpenEvent();
        }

        #endregion

        #region Balloon Tips

        /// <summary>
        /// Bubbles events if a balloon ToolTip was displayed
        /// or removed.
        /// </summary>
        /// <param name="visible">Whether the ToolTip was just displayed
        /// or removed.</param>
        private void OnBalloonToolTipChanged(bool visible) {
            if (visible) {
	            this.RaiseTrayBalloonTipShownEvent();
            } else {
	            this.RaiseTrayBalloonTipClosedEvent();
            }
        }

        /// <summary>
        /// Displays a balloon tip with the specified title,
        /// text, and icon in the taskbar for the specified time period.
        /// </summary>
        /// <param name="title">The title to display on the balloon tip.</param>
        /// <param name="message">The text to display on the balloon tip.</param>
        /// <param name="symbol">A symbol that indicates the severity.</param>
        public void ShowBalloonTip(string title, string message, BalloonIcon symbol) {
            lock (this.lockObject) {
                this.ShowBalloonTip(title, message, symbol.GetBalloonFlag(), IntPtr.Zero);
            }
        }

        /// <summary>
        /// Displays a balloon tip with the specified title,
        /// text, and a custom icon in the taskbar for the specified time period.
        /// </summary>
        /// <param name="title">The title to display on the balloon tip.</param>
        /// <param name="message">The text to display on the balloon tip.</param>
        /// <param name="customIcon">A custom icon.</param>
        /// <param name="largeIcon">True to allow large icons (Windows Vista and later).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="customIcon"/>
        /// is a null reference.</exception>
        public void ShowBalloonTip(string title, string message, Icon customIcon, bool largeIcon = false) {
            if (customIcon == null) throw new ArgumentNullException(nameof(customIcon));

            lock (this.lockObject) {
                var flags = BalloonFlags.User;

                if (largeIcon) {
                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                    flags |= BalloonFlags.LargeIcon;
                }

                this.ShowBalloonTip(title, message, flags, customIcon.Handle);
            }
        }

        /// <summary>
        /// Invokes <see cref="WinApi.Shell_NotifyIcon"/> in order to display
        /// a given balloon ToolTip.
        /// </summary>
        /// <param name="title">The title to display on the balloon tip.</param>
        /// <param name="message">The text to display on the balloon tip.</param>
        /// <param name="flags">Indicates what icon to use.</param>
        /// <param name="balloonIconHandle">A handle to a custom icon, if any, or
        /// <see cref="IntPtr.Zero"/>.</param>
        private void ShowBalloonTip(string title, string message, BalloonFlags flags, IntPtr balloonIconHandle) {
            this.EnsureNotDisposed();

            this.iconData.BalloonText = message ?? string.Empty;
            this.iconData.BalloonTitle = title ?? string.Empty;

            this.iconData.BalloonFlags = flags;
            this.iconData.CustomBalloonIconHandle = balloonIconHandle;
            Util.WriteIconData(ref this.iconData, NotifyCommand.Modify, IconDataMembers.Info | IconDataMembers.Icon);
        }

        /// <summary>
        /// Hides a balloon ToolTip, if any is displayed.
        /// </summary>
        public void HideBalloonTip() {
            this.EnsureNotDisposed();

            // reset balloon by just setting the info to an empty string
            this.iconData.BalloonText = this.iconData.BalloonTitle = string.Empty;
            Util.WriteIconData(ref this.iconData, NotifyCommand.Modify, IconDataMembers.Info);
        }

        #endregion

        #region Single Click Timer event

        /// <summary>
        /// Performs a delayed action if the user requested an action
        /// based on a single click of the left mouse.<br/>
        /// This method is invoked by the <see cref="singleClickTimer"/>.
        /// </summary>
        private void DoSingleClickAction(object state) {
            if (this.IsDisposed) return;

            // run action
            var action = this.singleClickTimerAction;
            if (action != null) {
                // cleanup action
                this.singleClickTimerAction = null;

                // switch to UI thread
                this.GetDispatcher().Invoke(action);
            }
        }

        #endregion

        #region Set Version (API)

        /// <summary>
        /// Sets the version flag for the <see cref="iconData"/>.
        /// </summary>
        private void SetVersion() {
            this.iconData.VersionOrTimeout = (uint)NotifyIconVersion.Vista;
            var status = WinApi.Shell_NotifyIcon(NotifyCommand.SetVersion, ref this.iconData);

            if (!status) {
                this.iconData.VersionOrTimeout = (uint)NotifyIconVersion.Win2000;
                status = Util.WriteIconData(ref this.iconData, NotifyCommand.SetVersion);
            }

            if (!status) {
                this.iconData.VersionOrTimeout = (uint)NotifyIconVersion.Win95;
                status = Util.WriteIconData(ref this.iconData, NotifyCommand.SetVersion);
            }

            if (!status) {
                Debug.Fail("Could not set version");
            }
        }

        #endregion

        #region Create / Remove Taskbar Icon

        /// <summary>
        /// Recreates the taskbar icon if the whole taskbar was
        /// recreated (e.g. because Explorer was shut down).
        /// </summary>
        private void OnTaskbarCreated() {
            this.IsTaskbarIconCreated = false;
            this.CreateTaskbarIcon();
        }

        /// <summary>
        /// Creates the taskbar icon. This message is invoked during initialization,
        /// if the taskbar is restarted, and whenever the icon is displayed.
        /// </summary>
        private void CreateTaskbarIcon() {
            lock (this.lockObject) {
                if (this.IsTaskbarIconCreated) {
                    return;
                }

                const IconDataMembers members = IconDataMembers.Message
                                                | IconDataMembers.Icon
                                                | IconDataMembers.Tip;

                //write initial configuration
                var status = Util.WriteIconData(ref this.iconData, NotifyCommand.Add, members);
                if (!status) {
                    // couldn't create the icon - we can assume this is because explorer is not running (yet!)
                    // -> try a bit later again rather than throwing an exception. Typically, if the windows
                    // shell is being loaded later, this method is being re-invoked from OnTaskbarCreated
                    // (we could also retry after a delay, but that's currently YAGNI)
                    return;
                }

                //set to most recent version
                this.SetVersion();
                this.messageSink.Version = (NotifyIconVersion)this.iconData.VersionOrTimeout;

                this.IsTaskbarIconCreated = true;
            }
        }

        /// <summary>
        /// Closes the taskbar icon if required.
        /// </summary>
        private void RemoveTaskbarIcon() {
            lock (this.lockObject) {
                // make sure we didn't schedule a creation

                if (!this.IsTaskbarIconCreated) {
                    return;
                }

                Util.WriteIconData(ref this.iconData, NotifyCommand.Delete, IconDataMembers.Message);
                this.IsTaskbarIconCreated = false;
            }
        }

        #endregion


        #region Dispose / Exit

        /// <summary>
        /// Set to true as soon as <c>Dispose</c> has been invoked.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Checks if the object has been disposed and
        /// raises a <see cref="ObjectDisposedException"/> in case
        /// the <see cref="IsDisposed"/> flag is true.
        /// </summary>
        private void EnsureNotDisposed() {
            if (this.IsDisposed) throw new ObjectDisposedException(this.Name ?? this.GetType().FullName);
        }

        /// <summary>
        /// Disposes the class if the application exits.
        /// </summary>
        private void OnExit(object sender, EventArgs e) {
            this.Dispose();
        }

        /// <summary>
        /// This destructor will run only if the <see cref="Dispose()"/>
        /// method does not get called. This gives this base class the
        /// opportunity to finalize.
        /// <para>
        /// Important: Do not provide destructor in types derived from this class.
        /// </para>
        /// </summary>
        ~TaskbarIcon() {
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <remarks>This method is not virtual by design. Derived classes
        /// should override <see cref="Dispose(bool)"/>.
        /// </remarks>
        public void Dispose() {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the tray and releases all resources.
        /// </summary>
        /// <summary>
        /// <c>Dispose(bool disposing)</c> executes in two distinct scenarios.
        /// If disposing equals <c>true</c>, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// </summary>
        /// <param name="disposing">If disposing equals <c>false</c>, the method
        /// has been called by the runtime from inside the finalizer and you
        /// should not reference other objects. Only unmanaged resources can
        /// be disposed.</param>
        /// <remarks>Check the <see cref="IsDisposed"/> property to determine whether
        /// the method has already been called.</remarks>
        private void Dispose(bool disposing) {
            // don't do anything if the component is already disposed
            if (this.IsDisposed || !disposing) return;

            lock (this.lockObject) {
                this.IsDisposed = true;

                // de-register application event listener
                if (Application.Current != null) {
                    Application.Current.Exit -= this.OnExit;
                }

                // stop timers
                this.singleClickTimer.Dispose();
                this.balloonCloseTimer.Dispose();

                // dispose message sink
                this.messageSink.Dispose();

                // remove icon
                this.RemoveTaskbarIcon();
            }
        }

        #endregion
    }
}