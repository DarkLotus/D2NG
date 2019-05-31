﻿using D2NG.D2GS;
using D2NG.D2GS.Packet;
using Serilog;
using System.IO;
using System.Text;

namespace D2NG
{
    internal class AssignPlayerPacket
    {

        public AssignPlayerPacket(D2gsPacket packet)
        {
            var reader = new BinaryReader(new MemoryStream(packet.Raw), Encoding.ASCII);
            if (0x59 != reader.ReadByte())
            {
                throw new D2GSPacketException("Expected Packet Type Not Found");
            }
            Id = reader.ReadUInt32();
            Class = (CharacterClass)reader.ReadByte();
            Name = Packet.ReadString(reader);
            Location = new Point(reader.ReadUInt16(), reader.ReadUInt16());

            Log.Verbose($"Assigning Player:\n" +
                        $"\tName: {Name}\n" +
                        $"\tClass: {Class}\n" +
                        $"\tId: {Id}\n" +
                        $"\tLocation: {Location}\n");
        }

        public Point Location { get; }
        public uint Id { get; }
        public CharacterClass Class { get; }
        public string Name { get; }
    }
}