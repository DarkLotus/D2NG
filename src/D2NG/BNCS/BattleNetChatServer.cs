﻿using D2NG.BNCS.Packet;
using Serilog;
using Stateless;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using D2NG.BNCS.Event;
using D2NG.BNCS.Login;
using System.Threading;
using Polly;

namespace D2NG
{
    public class BattleNetChatServer
    {
        private BncsConnection Connection { get; } = new BncsConnection();

        protected ConcurrentDictionary<byte, Action<BncsPacketReceivedEvent>> PacketReceivedEventHandlers { get; } = new ConcurrentDictionary<byte, Action<BncsPacketReceivedEvent>>();

        protected ConcurrentDictionary<byte, Action<BncsPacketSentEvent>> PacketSentEventHandlers { get; } = new ConcurrentDictionary<byte, Action<BncsPacketSentEvent>>();
        public ConcurrentDictionary<Sid, ConcurrentQueue<BncsPacket>> ReceivedQueue { get; set; }

        private const int MaxQueueSize = 100;

        private readonly StateMachine<State, Trigger> _machine = new StateMachine<State, Trigger>(State.NotConnected);

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> _connectTrigger;

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string, string> _loginTrigger;

        private CdKey _classicKey;

        private CdKey _expansionKey;

        private readonly uint _clientToken;

        private readonly String DefaultChannel = "Diablo II";
        private uint _serverToken;
        private string _username;

        enum State
        {
            NotConnected,
            Connected,
            Verified,
            KeysAuthorized,
            UserAuthenticated,
            Chatting,
            InChat,
        }
        enum Trigger
        {
            Connect,
            Disconnect,
            VerifyClient,
            AuthorizeKeys,
            Login,
            EnterChat
        }

        public BattleNetChatServer()
        {
            _clientToken = (uint)Environment.TickCount;

            _connectTrigger = _machine.SetTriggerParameters<String>(Trigger.Connect);

            _loginTrigger = _machine.SetTriggerParameters<String, String>(Trigger.Login);

            _machine.Configure(State.NotConnected)
                .Permit(Trigger.Connect, State.Connected);

            _machine.Configure(State.Connected)
                .OnEntryFrom<String>(_connectTrigger, OnConnect)
                .Permit(Trigger.VerifyClient, State.Verified)
                .Permit(Trigger.Disconnect, State.NotConnected);

            _machine.Configure(State.Verified)
                .SubstateOf(State.Connected)
                .OnEntryFrom(Trigger.VerifyClient, OnVerifyClient)
                .Permit(Trigger.AuthorizeKeys, State.KeysAuthorized)
                .Permit(Trigger.Disconnect, State.NotConnected);

            _machine.Configure(State.KeysAuthorized)
                .SubstateOf(State.Connected)
                .SubstateOf(State.Verified)
                .OnEntryFrom(Trigger.AuthorizeKeys, OnAuthorizeKeys)
                .Permit(Trigger.Login, State.UserAuthenticated)
                .Permit(Trigger.Disconnect, State.NotConnected);

            _machine.Configure(State.UserAuthenticated)
                .SubstateOf(State.Connected)
                .SubstateOf(State.Verified)
                .SubstateOf(State.KeysAuthorized)
                .OnEntryFrom(_loginTrigger, (username, password) => OnLogin(username, password))
                .Permit(Trigger.EnterChat, State.InChat)
                .Permit(Trigger.Disconnect, State.NotConnected);

            _machine.Configure(State.InChat)
                .SubstateOf(State.Connected)
                .SubstateOf(State.Verified)
                .SubstateOf(State.KeysAuthorized)
                .SubstateOf(State.UserAuthenticated)
                .OnEntryFrom(Trigger.EnterChat, OnEnterChat)
                .Permit(Trigger.Disconnect, State.NotConnected);

            Connection.PacketReceived += (obj, eventArgs) => {
                Log.Debug("[{0}] Received Packet {1}", GetType(), BitConverter.ToString(eventArgs.Packet.Raw));
                PacketReceivedEventHandlers.GetValueOrDefault(eventArgs.Packet.Type, null)?.Invoke(eventArgs);

                var sid = (Sid)eventArgs.Packet.Type;
                ReceivedQueue.GetOrAdd(sid, new ConcurrentQueue<BncsPacket>())
                    .Enqueue(eventArgs.Packet);

                if (ReceivedQueue[sid].Count > MaxQueueSize)
                {
                    ReceivedQueue[sid].TryDequeue(out _);
                }
            };

            Connection.PacketSent += (obj, eventArgs) => {
                Log.Debug("[{0}] Sent Packet {1}", GetType(), BitConverter.ToString(eventArgs.Packet));
                PacketSentEventHandlers.GetValueOrDefault(eventArgs.Type, null)?.Invoke(eventArgs);
            };

            OnReceivedPacketEvent(0x25, obj => Connection.WritePacket(obj.Packet.Raw));
        }


        public void ConnectTo(string realm, string classicKey, string expansionKey)
        {
            _machine.Fire(_connectTrigger, realm);
            _machine.Fire(Trigger.VerifyClient);
            if (classicKey.Length == 16)
            {
                _classicKey = new CdKeyBsha1(classicKey);
                _expansionKey = new CdKeyBsha1(expansionKey);
            }
            else
            {
                _classicKey = new CdKeySha1(classicKey);
                _expansionKey = new CdKeySha1(expansionKey);
            }

            _machine.Fire(Trigger.AuthorizeKeys);
        }

        public void EnterChat()
        {
            _machine.Fire(Trigger.EnterChat);
        }

        public void Listen()
        {
            while (_machine.IsInState(State.Connected))
            {
                _ = Connection.ReadPacket();
            }
        }

        public void Login(string username, string password)
        {
            _machine.Fire(_loginTrigger, username, password);
        }

        private byte[] WaitForPacket(Sid sid)
        {
            Policy.Handle<PacketNotFoundException>()
                .WaitAndRetry(new[] {
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(2000)
                })
                .Execute(() => CheckForPacket(sid));

            BncsPacket packet;
            ReceivedQueue.GetOrAdd(sid, new ConcurrentQueue<BncsPacket>())
                .TryDequeue(out packet);

            return packet.Raw;
        }

        private void CheckForPacket(Sid sid)
        {
            
            if (!ReceivedQueue.ContainsKey(sid) || ReceivedQueue[sid].IsEmpty)
            {
                throw new PacketNotFoundException("No packet in queue");
            }
        }

        private void OnConnect(String realm)
        {
            ReceivedQueue = new ConcurrentDictionary<Sid, ConcurrentQueue<BncsPacket>>();
            Connection.Connect(realm);
            var listener = new Thread(Listen);
            listener.Start();
        }

        private void OnEnterChat()
        {
            Connection.WritePacket(new EnterChatRequestPacket(_username));
            Connection.WritePacket(new JoinChannelRequestPacket(DefaultChannel));
            _ = WaitForPacket(Sid.ENTERCHAT);
        }

        private void OnLogin(string username, string password)
        {
            _username = username;
            var packet = new LogonRequestPacket(_clientToken, _serverToken, username, password);
            Connection.WritePacket(packet);

            var response = WaitForPacket(Sid.LOGONRESPONSE2);
            _ = new LogonResponsePacket(response);
        }

        private void OnVerifyClient()
        {
            Connection.WritePacket(new AuthInfoRequestPacket());
        }

        private void OnAuthorizeKeys()
        {
            var packet = new AuthInfoResponsePacket(WaitForPacket(Sid.AUTH_INFO));
            _serverToken = packet.ServerToken;
            
            Log.Debug("[{0}] Server token: {1} Logon Type: {2}", GetType(), _serverToken, packet.LogonType);

            var result = CheckRevisionV4.CheckRevision(packet.FormulaString);
            Log.Debug("[{0}] CheckRevision: {1}", GetType(), result);
            Connection.WritePacket(new AuthCheckRequestPacket(
                _clientToken,
                packet.ServerToken,
                result,
                _classicKey,
                _expansionKey));

            var authCheckResponse = new AuthCheckResponsePacket(WaitForPacket(Sid.AUTH_CHECK));

            Log.Debug("{0:X}", authCheckResponse);
        }

        public void OnReceivedPacketEvent(byte type, Action<BncsPacketReceivedEvent> handler)
        {
            if (PacketReceivedEventHandlers.ContainsKey(type))
            {
                PacketReceivedEventHandlers[type] += handler;
            }
            else
            {
                PacketReceivedEventHandlers.GetOrAdd(type, handler);
            }
        }

        public void OnSentPacketEvent(byte type, Action<BncsPacketSentEvent> handler)
        {
            if (PacketSentEventHandlers.ContainsKey(type))
            {
                PacketSentEventHandlers[type] += handler;
            }
            else
            {
                PacketSentEventHandlers.GetOrAdd(type, handler);
            }
        }

    }
}
