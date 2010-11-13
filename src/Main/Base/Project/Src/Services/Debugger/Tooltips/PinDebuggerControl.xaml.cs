﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Debugger;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Bookmarks;
using ICSharpCode.SharpDevelop.Debugging;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;

namespace Services.Debugger.Tooltips
{
	public partial class PinDebuggerControl : UserControl, IPinDebuggerControl
	{
		private const double ChildPopupOpenXOffet = 16;
		private const double ChildPopupOpenYOffet = 15;
		private const int InitialItemsCount = 12;
		private const double MINIMUM_OPACITY = 0.3;

		private DebuggerPopup childPopup;
		private LazyItemsControl<ITreeNode> lazyExpandersGrid;
		private LazyItemsControl<ITreeNode> lazyGrid;
		private LazyItemsControl<ITreeNode> lazyImagesGrid;
		private IEnumerable<ITreeNode> itemsSource;

		public PinDebuggerControl()
		{
			InitializeComponent();

			if (!DebuggerService.IsDebuggerRunning)
				Opacity = MINIMUM_OPACITY;
			PinCloseControl.Opacity = 0;

			Loaded += OnLoaded;
			this.PinCloseControl.Closed += PinCloseControl_Closed;
			this.PinCloseControl.ShowingComment += PinCloseControl_ShowingComment;
			this.PinCloseControl.PinningChanged += PinCloseControl_PinningChanged;

			BookmarkManager.Removed += BookmarkManager_Removed;

			if (DebuggerService.DebuggedProcess != null)
				DebuggerService.DebuggedProcess.Paused += DebuggerService_CurrentDebugger_DebuggedProcess_Paused;

			DebuggerService.ProcessSelected += delegate {
				Opacity = 1.0;
				DebuggerService.DebuggedProcess.Paused += DebuggerService_CurrentDebugger_DebuggedProcess_Paused;
			};
		}

		#region Properties

		public PinBookmark Mark { get; set; }

		public IEnumerable<ITreeNode> ItemsSource {
			set {
				itemsSource = value;
				var items = new VirtualizingIEnumerable<ITreeNode>(value);
				lazyExpandersGrid = new LazyItemsControl<ITreeNode>(this.ExpandersGrid, InitialItemsCount);
				lazyExpandersGrid.ItemsSource = items;

				lazyGrid = new LazyItemsControl<ITreeNode>(this.dataGrid, InitialItemsCount);
				lazyGrid.ItemsSource = items;

				lazyImagesGrid = new LazyItemsControl<ITreeNode>(this.ImagesGrid, InitialItemsCount);
				lazyImagesGrid.ItemsSource = items;
			}
		}

		/// <summary>
		/// Relative position of the pin with respect to the screen.
		/// </summary>
		public Point Location { get; set; }

		#endregion

		#region Main operations

		public void Open()
		{
			Pin();
		}

		public void Close()
		{
			Unpin();
		}

		void Pin()
		{
			var provider = WorkbenchSingleton.Workbench.ActiveContent as ITextEditorProvider;
			if (provider != null) {
				PinningBinding.GetPinlayer(provider.TextEditor).Pin(this);
			}
		}

		void Unpin()
		{
			var provider = WorkbenchSingleton.Workbench.ActiveContent as ITextEditorProvider;
			if (provider != null) {
				PinningBinding.GetPinlayer(provider.TextEditor).Unpin(this);
			}
		}

		#endregion

		#region Expand buton

		private ToggleButton expandedButton;

		/// <summary>
		/// Closes the child popup of this control, if it exists.
		/// </summary>
		void CloseChildPopups()
		{
			if (this.expandedButton != null) {
				this.expandedButton = null;
				// nice simple example of indirect recursion
				this.childPopup.CloseSelfAndChildren();
			}
		}

		void BtnExpander_Checked(object sender, RoutedEventArgs e)
		{
			var clickedButton = (ToggleButton)e.OriginalSource;
			var clickedNode = (ITreeNode)clickedButton.DataContext;
			// use device independent units, because child popup Left/Top are in independent units
			Point buttonPos = clickedButton.PointToScreen(new Point(0, 0)).TransformFromDevice(clickedButton);

			if (clickedButton.IsChecked.GetValueOrDefault(false)) {

				this.expandedButton = clickedButton;

				// open child Popup
				if (this.childPopup == null) {
					this.childPopup = new DebuggerPopup(null, false);
					this.childPopup.PlacementTarget = this;
					this.childPopup.Closed += new EventHandler(PinDebuggerControl_Closed);
					this.childPopup.Placement = PlacementMode.Absolute;
				}

				this.childPopup.IsLeaf = true;
				this.childPopup.HorizontalOffset = buttonPos.X + ChildPopupOpenXOffet;
				this.childPopup.VerticalOffset = buttonPos.Y + ChildPopupOpenYOffet;
				this.childPopup.ItemsSource = clickedNode.ChildNodes;
				this.childPopup.Open();
			} else {

			}
		}

		void PinDebuggerControl_Closed(object sender, EventArgs e)
		{
			if (expandedButton != null && expandedButton.IsChecked.GetValueOrDefault(false))
				expandedButton.IsChecked = false;
		}

		void BtnExpander_Unchecked(object sender, RoutedEventArgs e)
		{
			CloseChildPopups();
		}

		#endregion

		#region PinCloseControl

		void PinCloseControl_Closed(object sender, EventArgs e)
		{
			BookmarkManager.RemoveMark(Mark);
			CloseChildPopups();
			Unpin();
		}

		void PinCloseControl_PinningChanged(object sender, EventArgs e)
		{
			if (this.PinCloseControl.IsChecked) {
				BookmarkManager.RemoveMark(Mark);
			} else {
				if (BookmarkManager.Bookmarks.Contains(Mark))
					BookmarkManager.RemoveMark(Mark);

				BookmarkManager.AddMark(Mark);
			}
		}

		void PinCloseControl_ShowingComment(object sender, ShowingCommentEventArgs e)
		{
			ShowComment(e.ShowComment);
		}

		void AnimateCloseControl(bool show)
		{
			DoubleAnimation animation = new DoubleAnimation();
			animation.From = show ? 0 : 1;
			animation.To = show ? 1 : 0;
			animation.BeginTime = new TimeSpan(0, 0, show ? 0 : 1);
			animation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
			animation.SetValue(Storyboard.TargetProperty, this.PinCloseControl);
			animation.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath(Rectangle.OpacityProperty));

			Storyboard board = new Storyboard();
			board.Children.Add(animation);

			board.Begin(this);
		}

		#endregion

		void BookmarkManager_Removed(object sender, BookmarkEventArgs e)
		{
			// if the bookmark was removed from pressing the button, return
			if (this.PinCloseControl.IsChecked)
				return;

			if (e.Bookmark is PinBookmark) {
				var pin = (PinBookmark)e.Bookmark;
				if (pin.Location == Mark.Location && pin.FileName == Mark.FileName) {
					Close();
				}
			}
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			this.CommentTextBox.Text = Mark.Comment;
		}

		#region Comment

		void ShowComment(bool show)
		{
			if (show && BorderComment.Height != 0)
				return;
			if (!show && BorderComment.Height != 40)
				return;

			DoubleAnimation animation = new DoubleAnimation();
			animation.From = show ? 0 : 40;
			animation.To = show ? 40 : 0;

			animation.Duration = new Duration(TimeSpan.FromMilliseconds(300));
			animation.SetValue(Storyboard.TargetProperty, BorderComment);
			animation.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath(Border.HeightProperty));

			Storyboard board = new Storyboard();
			board.Children.Add(animation);
			board.Begin(this);
		}

		void CommentTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Mark.Comment = this.CommentTextBox.Text;
		}

		#endregion

		void DebuggerService_CurrentDebugger_DebuggedProcess_Paused(object sender, ProcessEventArgs e)
		{
			var observable = new List<ITreeNode>();

			foreach (var node in lazyGrid.ItemsSource) {
				var resultNode = DebuggerService.CurrentDebugger.GetNode(node.FullName);
				// HACK for updating the pins in tooltip
				observable.Add(resultNode);
			}

			// update UI
			var newSource = new VirtualizingIEnumerable<ITreeNode>(observable);
			lazyGrid.ItemsSource = newSource;
			lazyExpandersGrid.ItemsSource = newSource;
		}

		void RefreshContentImage_MouseDown(object sender, MouseButtonEventArgs e)
		{
			// refresh content
			ITreeNode node = ((Image)sender).DataContext as ITreeNode;

			if (!DebuggerService.IsDebuggerRunning)
				return;

			var resultNode = DebuggerService.CurrentDebugger.GetNode(node.FullName);
			// HACK for updating the pins in tooltip
			var observable = new List<ITreeNode>();
			var source = lazyGrid.ItemsSource;
			source.ForEach(item =>
			{
				if (item.CompareTo(node) == 0)
					observable.Add(resultNode);
				else
					observable.Add(item);
			});
			// update UI
			var newSource = new VirtualizingIEnumerable<ITreeNode>(observable);
			lazyGrid.ItemsSource = newSource;
			lazyExpandersGrid.ItemsSource = newSource;
		}

		#region Overrides

		protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
		{
			AnimateCloseControl(true);
			Opacity = 1.0;
			Cursor = Cursors.Arrow;
			base.OnMouseEnter(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			Opacity = 1.0;
			Cursor = Cursors.Arrow;
			base.OnMouseMove(e);
		}

		protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
		{
			if (DebuggerService.IsDebuggerRunning)
				Opacity = 1;
			else
				Opacity = MINIMUM_OPACITY;

			AnimateCloseControl(false);

			Cursor = Cursors.IBeam;
			base.OnMouseLeave(e);
		}

		#endregion
	}
}
