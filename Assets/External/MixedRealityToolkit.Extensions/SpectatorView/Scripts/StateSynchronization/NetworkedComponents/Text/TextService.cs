﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    internal class TextService : ComponentBroadcasterService<TextService, TextObserver>
    {
        public static readonly ShortID ID = new ShortID("UTX");

        public override ShortID GetID() { return ID; }

        private FontAssetCache fontAssets;

        protected override void Awake()
        {
            base.Awake();

            fontAssets = FontAssetCache.LoadAssetCache<FontAssetCache>();
        }

        private void Start()
        {
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<TextBroadcaster>(typeof(Text)));
        }

        public Guid GetFontId(Font font)
        {
            return fontAssets?.GetAssetId(font) ?? Guid.Empty;
        }

        public Font GetFont(Guid guid)
        {
            return fontAssets?.GetAsset(guid);
        }
    }
}
