﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using D2NG.MCP.Packet;

namespace D2NG.MCP
{
    public class RealmServer
    {
        private McpConnection Connection { get; } = new McpConnection();

        protected ConcurrentDictionary<Mcp, Action<McpPacket>> PacketReceivedEventHandlers { get; } = new ConcurrentDictionary<Mcp, Action<McpPacket>>();
        protected ConcurrentDictionary<Mcp, Action<McpPacket>> PacketSentEventHandlers { get; } = new ConcurrentDictionary<Mcp, Action<McpPacket>>();

        private readonly McpEvent CharLogonEvent = new McpEvent();
        private readonly McpEvent ListCharactersEvent = new McpEvent();
        private readonly McpEvent StartupEvent = new McpEvent();

        internal RealmServer()
        {
            Connection.PacketReceived += (obj, eventArgs) => PacketReceivedEventHandlers.GetValueOrDefault((Mcp)eventArgs.Type, null)?.Invoke(eventArgs);
            Connection.PacketSent += (obj, eventArgs) => PacketSentEventHandlers.GetValueOrDefault((Mcp)eventArgs.Type, null)?.Invoke(eventArgs);

            OnReceivedPacketEvent(Mcp.STARTUP, packet => StartupEvent.Set(packet));
            OnReceivedPacketEvent(Mcp.CHARLIST2, packet => ListCharactersEvent.Set(packet));
            OnReceivedPacketEvent(Mcp.CHARLOGON, packet => CharLogonEvent.Set(packet));
        }

        internal void Connect(IPAddress ip, short port)
        {
            Connection.Connect(ip, port);
            var listener = new Thread(Listen);
            listener.Start();
        }

        private void Listen()
        {
            while (Connection.Connected)
            {
                _ = Connection.ReadPacket();
            }
        }

        internal void CharLogon(Character character)
        {
            CharLogonEvent.Reset();
            var packet = new CharLogonRequestPacket(character.Name);
            Connection.WritePacket(packet);
            var response = new CharLogonResponsePacket(CharLogonEvent.WaitForPacket());
            if ( response.Result != 0x00)
            {
                throw new CharLogonException($"Failed to log on as {character.Name} - {response.Result}");
            }
        }

        internal void Logon(uint mcpCookie, uint mcpStatus, List<byte> mcpChunk, string mcpUniqueName)
        {
            StartupEvent.Reset();
            var packet = new McpStartupRequestPacket(mcpCookie, mcpStatus, mcpChunk, mcpUniqueName);
            Connection.WritePacket(packet);
            var response = StartupEvent.WaitForPacket();
            _ = new McpStartupResponsePacket(response.Raw);
        }

        public List<Character> ListCharacters()
        {
            ListCharactersEvent.Reset();
            Connection.WritePacket(new ListCharactersClientPacket());
            var packet = ListCharactersEvent.WaitForPacket();
            var response = new ListCharactersServerPacket(packet.Raw);
            return response.Characters;
        }

        public void OnReceivedPacketEvent(Mcp type, Action<McpPacket> handler)
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

        public void OnSentPacketEvent(Mcp type, Action<McpPacket> handler)
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