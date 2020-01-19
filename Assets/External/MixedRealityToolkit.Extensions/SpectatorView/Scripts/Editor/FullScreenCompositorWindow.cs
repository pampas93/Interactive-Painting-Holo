﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Editor
{
    [Description("Compositor Preview")]
    public class FullScreenCompositorWindow : CompositorWindowBase<FullScreenCompositorWindow>
    {
        public int TextureRenderMode { get; set; }

        public static void ShowFullscreen()
        {
            ShowWindow();

#if UNITY_EDITOR_WIN
            // Unity does not provide an API to maximize a floating utility window, so
            // go directly to Win32 to do the work.
            IntPtr hwnd = WindowsInterop.GetForegroundWindow();
            WindowsInterop.ShowWindow(hwnd, WindowsInterop.SW_SHOWMAXIMIZED);
#endif
        }

        private void OnGUI()
        {
            CompositeTextureGUI(TextureRenderMode);
        }

#if UNITY_EDITOR_WIN
        private class WindowsInterop
        {
            [DllImport("user32")]
            public static extern int ShowWindow(IntPtr hwnd, int swFlags);

            [DllImport("user32")]
            public static extern IntPtr GetForegroundWindow();

            public const int SW_SHOWMAXIMIZED = 3;
        }
#endif
    }
}