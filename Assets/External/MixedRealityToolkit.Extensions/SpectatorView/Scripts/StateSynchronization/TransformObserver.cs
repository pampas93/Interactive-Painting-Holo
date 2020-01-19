﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Extensions.Experimental.Socketer;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class TransformObserver : ComponentObserver<Transform>
    {
        private const string temporaryGameObjectName = "TEMP-0B111914-CE43-43B0-A7FB-7D546CE69FEE";

        /// <summary>
        /// Unique id for the transform
        /// </summary>
        public short Id { get; set; }

        /// <summary>
        /// Reads a network message and updates local state data
        /// </summary>
        /// <param name="sendingEndpoint">Sender endpoint</param>
        /// <param name="message">Received payload</param>
        public override void Read(SocketEndpoint sendingEndpoint, BinaryReader message)
        {
            short id = message.ReadInt16();

            TransformBroadcasterChangeType changeType = (TransformBroadcasterChangeType)message.ReadByte();

            GameObject mirror = null;
            RectTransform rectTransform = null;

            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.RectTransform))
            {
                mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                rectTransform = mirror.GetComponent<RectTransform>();
                if (rectTransform == null)
                    rectTransform = mirror.AddComponent<RectTransform>();
            }

            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Name))
            {
                mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                mirror.name = message.ReadString();
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Layer))
            {
                mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                mirror.layer = message.ReadInt32();
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Parent))
            {
                mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                short parentId = message.ReadInt16();
                int siblingIndex = message.ReadInt32();
                Transform newParent;
                if (parentId == TransformBroadcaster.NullTransformId)
                {
                    newParent = StateSynchronizationSceneManager.Instance.RootTransform;
                }
                else
                {
                    newParent = StateSynchronizationSceneManager.Instance.GetOrCreateMirror(parentId).transform;
                }

                if (siblingIndex < newParent.childCount)
                {
                    Transform existingChildAtIndex = newParent.GetChild(siblingIndex);
                    if (existingChildAtIndex.gameObject.name == temporaryGameObjectName)
                    {
                        Destroy(existingChildAtIndex.gameObject);
                    }
                }
                else
                {
                    for (int i = newParent.childCount; i < siblingIndex; i++)
                    {
                        GameObject temp = new GameObject(temporaryGameObjectName);
                        temp.SetActive(false);
                        temp.transform.SetParent(newParent);
                        temp.transform.SetSiblingIndex(i);
                    }
                }

                mirror.transform.SetParent(newParent, worldPositionStays: false);
                mirror.transform.SetSiblingIndex(siblingIndex);
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Position))
            {
                mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                mirror.transform.localPosition = message.ReadVector3();
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Rotation))
            {
                mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                mirror.transform.localRotation = message.ReadQuaternion();
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Scale))
            {
                mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                mirror.transform.localScale = message.ReadVector3();
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.IsActive))
            {
                bool isActive = message.ReadBoolean();
                if (isActive)
                {
                    mirror = mirror ?? StateSynchronizationSceneManager.Instance.GetOrCreateMirror(id);
                    mirror.SetActive(true);
                }
                else
                {
                    mirror = StateSynchronizationSceneManager.Instance.FindGameObjectWithId(id);
                    if (mirror != null)
                    {
                        mirror.SetActive(false);
                    }
                }
            }

            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.RectTransform))
            {
                RectTransformBroadcaster.Read(rectTransform, message);
                ComponentExtensions.EnsureComponent<RectTransformObserver>(mirror).CaptureRectTransform();
            }
        }

        /// <summary>
        /// Reads a network message and updates local state data using interpolation
        /// </summary>
        /// <param name="message">Received payload</param>
        /// <param name="lerpVal">interpolation value</param>
        public void LerpRead(BinaryReader message, float lerpVal)
        {
            short id = message.ReadInt16();

            TransformBroadcasterChangeType changeType = (TransformBroadcasterChangeType)message.ReadByte();
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Name) ||
                TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Layer) ||
                TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Parent) ||
                TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.IsActive))
            {
                return;
            }

            GameObject mirror = StateSynchronizationSceneManager.Instance.FindGameObjectWithId(id);
            if (mirror == null)
            {
                return;
            }


            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Position))
            {
                mirror.transform.localPosition = Vector3.Lerp(mirror.transform.localPosition, message.ReadVector3(), lerpVal);
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Rotation))
            {
                mirror.transform.localRotation = Quaternion.Slerp(mirror.transform.localRotation, message.ReadQuaternion(), lerpVal);
            }
            if (TransformBroadcaster.HasFlag(changeType, TransformBroadcasterChangeType.Scale))
            {
                mirror.transform.localScale = Vector3.Lerp(mirror.transform.localScale, message.ReadVector3(), lerpVal);
            }
        }
    }
}