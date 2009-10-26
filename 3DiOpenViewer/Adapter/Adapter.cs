/*
 * Copyright (c) 2009, 3Di, Inc. (http://3di.jp/) and contributors.
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 * All rights reserved.
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace OpenViewer
{
    public class Adapter : IAdapter
    {
        #region privateElement
        private IRefController __reference;
        private RefController reference;

        private System.Threading.Thread touchThread;
        private string touchUUID = string.Empty;

        private System.Threading.Thread avatarPickedThread;
        private string avatarInformation = string.Empty;

        private System.Threading.Thread teleportEventThread;
        private class TeleportEventParam
        {
            public string RegionName { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

            public TeleportEventParam(string _regionName, int _x, int _y, int _z)
            {
                RegionName = _regionName;
                X = _x;
                Y = _y;
                Z = _z;
            }
        }
        private TeleportEventParam teleportEventParam;

        private System.Threading.Thread teleportedEventThread;
        private class TeleportedEventParam
        {
            public string AvatarUUID { get; set; }
            public string AvatarName { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

            public TeleportedEventParam(string _avatarUUID, string _avatarName, int _x, int _y, int _z)
            {
                AvatarUUID = _avatarUUID;
                AvatarName = _avatarName;
                X = _x;
                Y = _y;
                Z = _z;
            }
        }
        private TeleportedEventParam teleportedEventParam;
        #endregion

        #region General function.
        public void Initialize(IRefController _reference)
        {
            __reference = _reference;
            reference = (RefController)__reference;
        }

        public void Cleanup()
        {
            //--------------------------------------
            // Touch on event thread.
            //--------------------------------------
            if (touchThread != null)
            {
                touchThread.Abort();
                touchThread = null;
            }

            //--------------------------------------
            // Avatar pick event thread.
            //--------------------------------------
            if (avatarPickedThread != null)
            {
                avatarPickedThread.Abort();
                avatarPickedThread = null;
            }

            //--------------------------------------
            // Teleport on event thread.
            //--------------------------------------
            if (teleportEventThread != null)
            {
                teleportEventThread.Abort();
                teleportEventThread = null;
            }

            if (teleportedEventThread != null)
            {
                teleportedEventThread.Abort();
                teleportedEventThread = null;
            }
        }

        public void Update()
        {
            //--------------------------------------
            // Touch on event thread.
            //--------------------------------------
            if (string.IsNullOrEmpty(touchUUID) == false && touchThread == null)
            {
                touchThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(TouchedThred));
                touchThread.Start();
            }

            //--------------------------------------
            // Avatar pick event thread.
            //--------------------------------------
            if (string.IsNullOrEmpty(avatarInformation) == false && avatarPickedThread == null)
            {
                avatarPickedThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(AvatarPickedThredFunction));
                avatarPickedThread.Start();
            }

            //--------------------------------------
            // Teleport on event thread.
            //--------------------------------------
            if (teleportEventParam != null && teleportEventThread == null)
            {
                teleportEventThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(TeleportThread));
                teleportEventThread.Start();
            }

            if (teleportedEventParam != null && teleportedEventThread == null)
            {
                teleportedEventThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(TeleportedThread));
                teleportedEventThread.Start();
            }
        }
        #endregion

        #region Events
        #endregion

        #region 0. Debug
        public event TouchToListener OnDebugMessage;

        public void CallDebugMessage(string _message)
        {
            if (OnDebugMessage != null)
                OnDebugMessage(_message);
        }
        #endregion

        #region 1. Login / Logput
        /// <summary>
        /// Login with specified account data
        /// </summary>
        /// <param name="_firstName">firstName</param>
        /// <param name="_lastName">lastName</param>
        /// <param name="_password">password</param>
        /// <param name="_serverURL">serverURL http://login-server-url</param>
        /// <param name="_location">location  REGION_NAME/X/Y/Z</param>
        public void CallLogin(string _firstName, string _lastName, string _password, string _serverURL, string _loginLocation)
        {
            // Set param.
            reference.Viewer.FirstName = _firstName;
            reference.Viewer.LastName = _lastName;
            reference.Viewer.Password = _password;
            reference.Viewer.ServerURI = _serverURL;
            reference.Viewer.LoginLocation = _loginLocation;

            // Begin login.
            reference.Viewer.LoginRequest();
        }

        /// <summary>
        /// Logout immediately
        /// </summary>
        public void CallLogout()
        {
            // Begin logout.
            reference.Viewer.LogoutRequest();
        }

        #endregion

        #region 2. Touch
        public event TouchToListener OnTouched;

        /// <summary>
        /// Touch specified object.
        /// </summary>
        /// <param name="_uuid">Target object UUID</param>
        public void CallTouchTo(string _uuid)
        {
            uint localID = reference.Viewer.EntityManager.GetLocalIDFromUUID(_uuid);
            reference.Viewer.ProtocolManager.TouchTo(localID);

            // call OnTouched event.
            if (reference.Viewer.EntityManager.IsContain(_uuid))
                touchUUID = _uuid;
        }

        /// <summary>
        /// When a user touch specified object in In-world,
        /// this function will notify the object UUID.
        /// </summary>
        /// <param name="_uuid">Touched object UUID</param>
        public void CallTouched(string _uuid)
        {
            if (OnTouched != null)
                OnTouched(_uuid);
        }

        private void TouchedThred(object _parent)
        {
            if (touchUUID != null)
            {
                CallTouched(touchUUID);
                touchUUID = string.Empty;
            }
            touchThread = null;
        }
        #endregion

        #region 3. Sit / Stand
        /// <summary>
        /// Sit on specified SIT ball object.
        /// </summary>
        /// <param name="_uuid">Sit target object UUID</param>
        public void CallSitOn(string _uuid)
        {
            reference.Viewer.ProtocolManager.SitOn(_uuid);
        }

        /// <summary>
        /// Stand up from specified SIT ball object.
        /// </summary>
        public void CallStandUp()
        {
            reference.Viewer.AvatarManager.Standup();
        }
        #endregion

        #region 4. Text Chat
        public event OnReceiveMessageListener OnReceiveMessage;

        /// <summary>
        /// Send InstantMessage via DHTM
        /// </summary>
        /// <param name="_target_uuid">target user uuid</param>
        /// <param name="_message">message</param>
        public void CallSendIM(string _target_uuid, string _message)
        {
            reference.Viewer.ProtocolManager.SendIM(_target_uuid, _message);
        }

        /// <summary>
        /// Send text chat message via DHTM
        /// </summary>
        /// <param name="_message">Chat message</param>
        /// <param name="_range">Range of spread area
        /// 1 : whisper
        /// 2 : say
        /// 3 : shout
        /// </param>
        public void CallSendChat(string _message, int _range)
        {
            reference.Viewer.ProtocolManager.SendChat(_message, _range);
        }

        /// <summary>
        /// When a user receive text chat message in In-world,
        /// this function will notify the reseived message.
        /// </summary>
        /// <param name="_uuid">UUID of avatar</param>
        /// <param name="_avatarName">Name of avatar</param>
        /// <param name="_message">Received message</param>
        public void CallReceiveMessaged(string _uuid, string _avatarName, string _message)
        {
            if (OnReceiveMessage != null)
                OnReceiveMessage(_uuid, _avatarName, _message);
        }

        /// <summary>
        /// Get all stored message count.
        /// </summary>
        /// <returns>Lenght</returns>
        public int CallGetMessageHistoryLength()
        {
            return reference.Viewer.ChatManager.Messages.Length;
        }

        /// <summary>
        /// Get all messages from message history.
        /// </summary>
        /// <param name="_index">message's history number</param>
        /// <returns>message</returns>
        public string CallGetMessageFromHistory(int _index)
        {
            string message = string.Empty;

            if (_index < reference.Viewer.ChatManager.Messages.Length)
                message = reference.Viewer.ChatManager.Messages[_index];

            return message;
        }

        #endregion

        #region 5. Teleport
        public event TeleportToListener OnTeleport;
        public event TeleportListener OnTeleported;

        /// <summary>
        /// Teleport to specified location.
        /// </summary>
        /// <param name="_regionName">regionName</param>
        /// <param name="_x">X axsis position</param>
        /// <param name="_y">Y axsis position</param>
        /// <param name="_z">Z axsis position</param>
        public void CallTeleportTo(string _regionName, int _x, int _y, int _z)
        {
            CallTeleport(_regionName, _x, _y, _z);

            reference.Viewer.ProtocolManager.Teleport(_regionName, _x, _y, _z);
        }

        /// <summary>
        /// When a user receive someone/himself teleport started same sim in In-world,
        /// this function will notify the message.
        /// </summary>
        /// <param name="_regionName">regionName</param>
        /// <param name="_x">X axsis position</param>
        /// <param name="_y">Y axsis position</param>
        /// <param name="_z">Z axsis position</param>
        public void CallTeleport(string _regionName, int _x, int _y, int _z)
        {
            teleportEventParam = new TeleportEventParam(_regionName, _x, _y, _z);
        }

        /// <summary>
        /// When a user receive someone/himself teleported same sim in In-world,
        /// this function will notify the message.
        /// </summary>
        /// <param name="_uuid">UUID of avatar</param>
        /// <param name="_avatar">Name of avatar</param>
        /// <param name="_x">X axsis position</param>
        /// <param name="_y">Y axsis position</param>
        /// <param name="_z">Z axsis position</param>
        public void CallTeleported(string _uuid, string _avatar, int _x, int _y, int _z)
        {
            teleportedEventParam = new TeleportedEventParam(_uuid, _avatar, _x, _y, _z);
        }

        public void TeleportThread(object _obj)
        {
            if (OnTeleport != null)
                OnTeleport(teleportEventParam.RegionName, teleportEventParam.X, teleportEventParam.Y, teleportEventParam.Z);

            teleportEventParam = null;
            teleportEventThread = null;
        }

        public void TeleportedThread(object _obj)
        {
            if (OnTeleported != null)
                OnTeleported(teleportedEventParam.AvatarUUID, teleportedEventParam.AvatarName, teleportedEventParam.X, teleportedEventParam.Y, teleportedEventParam.Z);

            teleportedEventParam = null;
            teleportedEventThread = null;
        }
        #endregion

        #region 6. LSL triggered html related manupuration
        public event OpenWindowListener OnOpenWindow;

        /// <summary>
        /// Open browser window with specified uri.
        /// </summary>
        /// <param name="_target">Window target</param>
        /// <param name="_uri">Target uri</param>
        public void CallOpenWindow(string _target, string _uri)
        {
            if (OnOpenWindow != null)
                OnOpenWindow(_target, _uri);
        }
        #endregion

        #region 7. User avatar
        public event AvatarPickListener OnAvatarPicked;

        public string CallGetLoggedinAvatarUUIDList()
        {
            return reference.Viewer.AvatarManager.GetUserUUIDList();
        }

        public void CallAvatarPicked(string _avatarInformation)
        {
            // call OnTouched event.
            //if (reference.Viewer.AvatarManager.IsContain(_avatarInformation))
                avatarInformation = _avatarInformation;
        }

        public void CallAvatarCustomizeAnimation(int _index)
        {
            reference.Viewer.AvatarManager.RequestCustomizeAnimation(_index);
        }

        private void AvatarPickedThredFunction(object _parent)
        {
            if (avatarInformation != null)
            {
                if (OnAvatarPicked != null)
                    OnAvatarPicked(avatarInformation);

                avatarInformation = string.Empty;
            }
            avatarPickedThread = null;
        }

        public string CallGetUserUUID()
        {
            return reference.Viewer.ProtocolManager.AvatarConnection.GetSelfUUID.ToString();
        }

        public string CallGetUserAvatarPosition()
        {
            string positionText = string.Empty;

            if (reference.Viewer.AvatarManager.UserObject != null)
            {
                if (reference.Viewer.AvatarManager.UserObject.Prim != null)
                {
                    positionText += reference.Viewer.AvatarManager.GetUserAvatarPosition().X.ToString("0.000") + ",";
                    positionText += reference.Viewer.AvatarManager.GetUserAvatarPosition().Y.ToString("0.000") + ",";
                    positionText += reference.Viewer.AvatarManager.GetUserAvatarPosition().Z.ToString("0.000");
                }
            }

            return positionText;
        }

        public string CallGetUserAvatarUUID()
        {
            string uuid = new OpenMetaverse.UUID().ToString();
            if (reference.Viewer.AvatarManager.UserObject != null)
                uuid = reference.Viewer.AvatarManager.UserObject._3DiIrrfileUUID.ToString();

            return uuid;
        }

        public string CallGetUserAvatarName()
        {
            string userName = reference.Viewer.ProtocolManager.AvatarConnection.username;

            if (string.IsNullOrEmpty(userName))
                userName = string.Empty;

            return userName;
        }

        public void CallUserAvatarUp(bool _flag)
        {
            reference.Viewer.AvatarManager.UserPushForward(_flag);
        }

        public void CallUserAvatarDown(bool _flag)
        {
            reference.Viewer.AvatarManager.UserPushBackward(_flag);
        }

        public void CallUserAvatarLeft()
        {
            reference.Viewer.AvatarManager.UserPushLeft();
        }

        public void CallUserAvatarRight()
        {
            reference.Viewer.AvatarManager.UserPushRight();
        }
        #endregion

        #region 8. Common
        public event StateChangedListener OnStateChanged;

        public void CallStateChanged(int _state)
        {
            if (OnStateChanged != null)
                OnStateChanged(_state);
        }

        public int CallGetFPS()
        {
            return reference.Device.VideoDriver.FPS;
        }

        public int CallGetPrimitiveCount()
        {
            return reference.Device.VideoDriver.PrimitiveCountDrawn;
        }

        public int CallGetTextureCount()
        {
            return reference.Device.VideoDriver.TextureCount;
        }
        #endregion

        #region 9. Camera
        public void CallCameraLookAt(float _px, float _py, float _pz, float _tx, float _ty, float _tz)
        {
            reference.Viewer.Camera.MoveLookAt(_px, _py, _pz, _tx, _ty, _tz);
        }

        public void CallSetCameraDistance(float _distance)
        {
            _distance = Util.Clamp<float>(_distance, reference.Viewer.CameraMinDistance, reference.Viewer.CameraMaxDistance);

            reference.Viewer.CameraKeyWalkingDistance = _distance;
        }

        public string CallGetCameraDistance()
        {
            return reference.Viewer.CameraKeyWalkingDistance.ToString("0.000");
        }

        /// <summary>
        /// Get camera position.
        /// </summary>
        /// <returns>Lenght</returns>
        public string CallGetCameraPosition()
        {
            string positionText = string.Empty;
            positionText += reference.Viewer.Camera.Position.X.ToString("0.000") + ",";
            positionText += reference.Viewer.Camera.Position.Y.ToString("0.000") + ",";
            positionText += reference.Viewer.Camera.Position.Z.ToString("0.000");

            return positionText;
        }

        /// <summary>
        /// Get camera position.
        /// </summary>
        /// <returns>Lenght</returns>
        public string CallGetCameraTarget()
        {
            string positionText = string.Empty;

            if (reference.Viewer.Camera.SNCamera != null)
            {
                positionText += reference.Viewer.Camera.SNCamera.Target.X.ToString("0.000") + ",";
                positionText += reference.Viewer.Camera.SNCamera.Target.Y.ToString("0.000") + ",";
                positionText += reference.Viewer.Camera.SNCamera.Target.Z.ToString("0.000");
            }

            return positionText;
        }

        public string CallGetCameraFOV()
        {
            return reference.Viewer.CameraFOV.ToString("0.000");
        }

        public void CallSetCameraFOV(float _fov)
        {
            if (float.IsNaN(_fov) == false)
            {
                if (_fov > 0)
                    reference.Viewer.Camera.SetFOV(_fov);
            }
        }

        public void CallSetCameraFOVDegree(float _fov)
        {
            CallSetCameraFOV(OpenMetaverse.Utils.ToRadians(_fov));
        }

        public string CallGetCameraOffsetY()
        {
            return reference.Viewer.CameraOffsetY.ToString("0.000");
        }

        public void CallSetCameraOffsetY(float _offsetY)
        {
            reference.Viewer.CameraOffsetY = _offsetY;
        }

        public string CallGetCameraAngleY()
        {
            string text = string.Empty;
            text += reference.Viewer.CameraMinAngleY.ToString("0.000") + ",";
            text += reference.Viewer.CameraMaxAngleY.ToString("0.000");

            return text;
        }

        public void CallSetCameraAngleY(float _min, float _max)
        {
            reference.Viewer.CameraMinAngleY = _min;
            reference.Viewer.CameraMaxAngleY = _max;
        }
        #endregion

        #region 10. World
        public string CallGetAvatarCount()
        {
            return reference.Viewer.AvatarManager.EntitiesCount.ToString();
        }

        public string CallGetObjectCount()
        {
            return reference.Viewer.EntityManager.EntitiesCount.ToString();
        }

        public string CallGetRegionName()
        {
            return reference.Viewer.ProtocolManager.GetCurrentSimName();
        }

        public string CallGetWorldTime()
        {
            string text = reference.Viewer.WorldTime.Year.ToString() + ","
                + reference.Viewer.WorldTime.Month.ToString() + ","
                + reference.Viewer.WorldTime.Day.ToString() + ","
                + reference.Viewer.WorldTime.Hour.ToString() + ","
                + reference.Viewer.WorldTime.Minute.ToString() + ","
                + reference.Viewer.WorldTime.Second.ToString();

            return text;
        }

        public void CallSetWorldTime(string _dataTime)
        {
            reference.Viewer.SetWorldTime(_dataTime);
        }

        public void CallSetTickOn(string _flag)
        {
            if (string.IsNullOrEmpty(_flag))
                _flag = "true";

            reference.Viewer.TickOn = _flag;
        }

        public void CallSetWorldAmbientColor(string _colors)
        {
            reference.Viewer.AmbientLightColor = Util.ColorfFromStringRGB(_colors);
            reference.Viewer.RequestSetWorldAmbientLightColor();
        }
        #endregion

        #region 11. Fix directional
        public void CallSetFixDirectional(string _flag)
        {
            if (string.IsNullOrEmpty(_flag))
                _flag = "false";

            reference.Viewer.FixDirectional = _flag;
        }

        public void CallSetFixDirectionalRotation(string _radRotation)
        {
            IrrlichtNETCP.Vector3D radRotation = Util.Vector3DFromStringXYZ(_radRotation);
            reference.Viewer.DirectionalRotation = radRotation * OpenMetaverse.Utils.RAD_TO_DEG;
        }

        public void CallSetFixDirectionalDiffuseColor(string _colors)
        {
            reference.Viewer.DirectionalDiffuseColor = Util.ColorfFromStringRGB(_colors);
        }

        public void CallSetFixDirectionalAmbientColor(string _colors)
        {
            reference.Viewer.DirectionalAmbientColor = Util.ColorfFromStringRGB(_colors);
        }

        #endregion

        #region 13. Callback and Dispatch
        // Message Plugin->Plugin
        private Dictionary<string, List<MessageHandler>> message_store = new Dictionary<string, List<MessageHandler>>();

        public void RegisterMessage(string action, MessageHandler message)
        {
            if (message_store.ContainsKey(action))
            {
                message_store[action].Add(message);
            }
            else
            {
                message_store.Add(action, new List<MessageHandler>());
                message_store[action].Add(message);
            }
        }

        public object SendMessage(string action, object parameters)
         {
            if (message_store.ContainsKey(action))
            {
                foreach (MessageHandler message in message_store[action])
                {
                    message.Invoke(parameters);
                }
            }
            return (null);
        }

        // Callback JS->OV
        private Dictionary<string, List<Callback>> callback_store = new Dictionary<string, List<Callback>>();

        public void RegisterCallback(string action, Callback callback)
        {
            if (callback_store.ContainsKey(action))
            {
                callback_store[action].Add(callback);
            }
            else
            {
                callback_store.Add(action, new List<Callback>());
                callback_store[action].Add(callback);
            }
        }

        public string RunCallback(string action, string message)
        {
            if (callback_store.ContainsKey(action))
            {
                foreach (Callback callback in callback_store[action])
                {
                    return callback.Invoke(message);
                }
            }
            return (string.Empty);
        }

        // Dispatch OV->JS
        public event DispatchListener OnDispatch;

        public void Dispatch(string action, string message)
        {
            if (OnDispatch != null)
                OnDispatch(action, message);
        }
        #endregion
    }
}
