﻿using System.Collections.Generic;
using Cosmos.System;
using GrapeOS.Tasking;
using GrapeGL.Graphics;

namespace GrapeOS.Graphics
{
    internal class Window : Process
    {
        private bool _wasMouseOverCloseButton, _wasMouseOverMaximizeButton, _wasMouseOverMinimizeButton;

        private int _originalX, _originalY;
        private ushort _originalWidth, _originalHeight;

        private int _dragStartX, _dragStartY, _dragStartMouseX, _dragStartMouseY;
        private bool _dragging = false;

        private MouseState _lastMouseState = MouseState.None;

        internal string Title;
        internal int X, Y;
        internal ushort Width, Height;
        internal bool Borderless = false;
        internal bool Maximized = false;
        internal bool Minimized = false;

        internal Canvas Contents;
        internal List<Control> Controls;

        internal bool Focused
        {
            get => WindowManager.Instance.FocusedWindow == this;
        }

        internal bool IsMouseOver
        {
            get => MouseManager.X > X && MouseManager.X < X + Width && MouseManager.Y > Y && MouseManager.Y < Y + Height;
        }

        internal bool IsMouseOverTitlebar
        {
            get => MouseManager.X > X && MouseManager.X < X + Width && MouseManager.Y > Y && MouseManager.Y < Y + 22;
        }

        internal bool IsMouseOverCloseButton
        {
            get => MouseManager.X > X + 4 && MouseManager.X < X + 17 && MouseManager.Y > Y + 4 && MouseManager.Y < Y + 17;
        }

        internal bool IsMouseOverMaximizeButton
        {
            get => MouseManager.X > X + Width - 33 && MouseManager.X < X + Width - 20 && MouseManager.Y > Y + 4 && MouseManager.Y < Y + 17;
        }

        internal bool IsMouseOverMinimizeButton
        {
            get => MouseManager.X > X + Width - 17 && MouseManager.X < X + Width - 4 && MouseManager.Y > Y + 4 && MouseManager.Y < Y + 17;
        }

        protected Window(int X, int Y, ushort Width, ushort Height, string Title) : base(Title)
        {
            this.Title = Title;
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;

            Contents = new Canvas(Width, Height);
            Controls = new List<Control>();
            Render();

            WindowManager.Instance.AddWindow(this);
        }

        internal virtual void Render()
        {
            // Clear the window
            Contents.Clear(new Color(0xFFDADADA));

            // Render the window border
            Contents.DrawRectangle(0, 0, Width, Height, 0, Color.Black);
            Contents.DrawLine(1, 1, Width - 2, 1, Color.White);
            Contents.DrawLine(1, 1, 1, Height - 2, Color.White);
            Contents.DrawLine(2, Height - 2, Width - 1, Height - 2, new Color(Borderless ? 0xFFC0C0C0 : 0xFFB3B3B3));
            Contents.DrawLine(Width - 2, 2, Width - 2, Height - 2, new Color(Borderless ? 0xFFC0C0C0 : 0xFFB3B3B3));

            if (Borderless)
            {
                Contents[Width - 2, 1] = new Color(0xFFF3F3F3);
                Contents[1, Height - 2] = new Color(0xFFF3F3F3);
                return;
            }

            // Render the title bar
            RenderTitlebarButtons();

            for (int i = 0; i < 6; i++) Contents.DrawLine(21, 4 + i * 2, Width - 38, 4 + i * 2, Color.White);
            for (int i = 0; i < 6; i++) Contents.DrawLine(22, 5 + i * 2, Width - 37, 5 + i * 2, new Color(0xFF969696));

            Contents.DrawFilledRectangle((Width / 2) - (Resources.Charcoal.MeasureString(Title + " ") / 2),
                4, Resources.Charcoal.MeasureString(Title + " "), 12, 0, new Color(0xFFDADADA));
            Contents.DrawString((Width / 2) - (Resources.Charcoal.MeasureString(Title) / 2) - 1,
                2, Title, Resources.Charcoal, Color.Black);

            // Render window contents area
            Contents.DrawLine(4, 20, Width - 6, 20, new Color(0xFFB3B3B3));
            Contents.DrawLine(4, 20, 4, Height - 6, new Color(0xFFB3B3B3));
            Contents.DrawLine(4, Height - 6, Width - 5, Height - 6, Color.White);
            Contents.DrawLine(Width - 5, 21, Width - 5, Height - 6, Color.White);
            Contents.DrawRectangle(5, 21, (ushort)(Width - 10), (ushort)(Height - 27), 0, Color.Black);

            Contents.DrawFilledRectangle(6, 22, (ushort)(Width - 12), (ushort)(Height - 29), 0, new Color(0xFFE7E7E7));

            // Render the controls
            foreach (Control c in Controls)
            {
                if (c == null) Controls.Remove(c);
                Contents.DrawImage(c.X + 6, c.Y + 22, c.Contents, c.RenderWithAlpha);
            }

            WindowManager.Instance.Render();
        }

        private void RenderTitlebarButtons()
        {
            Contents.DrawImage(4, 4, IsMouseOverCloseButton ? Resources.CloseButtonPressed : Resources.CloseButton);
            Contents.DrawImage(Width - 33, 4, IsMouseOverMaximizeButton ? Resources.MaximizeButtonPressed : Resources.MaximizeButton);
            Contents.DrawImage(Width - 17, 4, IsMouseOverMinimizeButton ? Resources.MinimizeButtonPressed : Resources.MinimizeButton);

            WindowManager.Instance.Render();
        }

        internal override void HandleRun()
        {
            // Handle titlebar buttons
            if (IsMouseOverCloseButton)
            {
                if (!_wasMouseOverCloseButton)
                    RenderTitlebarButtons();

                if (_lastMouseState == MouseState.Left &&
                    MouseManager.MouseState == MouseState.None)
                {
                    Dispose();
                }

                _wasMouseOverCloseButton = true;
            }
            else if (IsMouseOverMaximizeButton)
            {
                if (!_wasMouseOverMaximizeButton)
                    RenderTitlebarButtons();

                if (_lastMouseState == MouseState.Left &&
                    MouseManager.MouseState == MouseState.None)
                {
                    _originalX = X;
                    _originalY = Y;

                    Maximized = !Maximized;
                    Minimized = false;

                    // Do the resizing
                    if (Maximized)
                    {
                        X = _originalX;
                        Y = _originalY;
                        Width = _originalWidth;
                        Height = _originalHeight;
                    }
                    else
                    {
                        X = 10;
                        Y = 10;
                        Width = (ushort)(WindowManager.Instance.Screen.Width - 20);
                        Height = (ushort)(WindowManager.Instance.Screen.Height - 20);
                    }

                    Contents = new Canvas(Width, Height);
                    Render();
                }

                _wasMouseOverMaximizeButton = true;
            }
            else if (IsMouseOverMinimizeButton)
            {
                if (!_wasMouseOverMinimizeButton)
                    RenderTitlebarButtons();

                if (_lastMouseState == MouseState.Left &&
                    MouseManager.MouseState == MouseState.None)
                {
                    Maximized = false;
                    Minimized = !Minimized;

                    Height = Minimized ? _originalHeight : (ushort)22;

                    Contents = new Canvas(Width, Height);
                    Render();
                }

                _wasMouseOverMinimizeButton = true;
            }

            if ((_wasMouseOverCloseButton && !IsMouseOverCloseButton) ||
                (_wasMouseOverMaximizeButton && !IsMouseOverMaximizeButton) ||
                (_wasMouseOverMinimizeButton && !IsMouseOverMinimizeButton))
            {
                RenderTitlebarButtons();

                _wasMouseOverCloseButton = false;
                _wasMouseOverMaximizeButton = false;
                _wasMouseOverMinimizeButton = false;
            }

            // Handle dragging
            if (IsMouseOverTitlebar && !IsMouseOverCloseButton &&
                !IsMouseOverMaximizeButton && !IsMouseOverMinimizeButton &&
                _lastMouseState == MouseState.None &&
                MouseManager.MouseState == MouseState.Left)
            {
                _dragStartX = X;
                _dragStartY = Y;
                _dragStartMouseX = (int)MouseManager.X;
                _dragStartMouseY = (int)MouseManager.Y;
                _dragging = true;
            }

            if (_dragging && MouseManager.MouseState == MouseState.None)
                _dragging = false;

            if (_dragging)
            {
                X = (int)(_dragStartX + (MouseManager.X - _dragStartMouseX));
                Y = (int)(_dragStartY + (MouseManager.Y - _dragStartMouseY));

                WindowManager.Instance.Render();
            }

            // Handle the controls
            foreach (Control c in Controls)
            {
                if (c == null) Controls.Remove(c);
                c.HandleRun();
            }

            _lastMouseState = MouseManager.MouseState;
        }

        internal override void Dispose()
        {
            WindowManager.Instance.RemoveWindow(this);
            WindowManager.Instance.Render();
            base.Dispose();
        }
    }
}
