﻿using Serilog;
using System.IO;
using System.Text;

namespace D2NG.BNCS.Packet
{
    public class AuthInfoResponsePacket : BncsPacket
    {
        public AuthInfoResponsePacket(BncsPacket bncsPacket) : this(bncsPacket.Raw)
        {
        }

        public AuthInfoResponsePacket(byte[] packet) : base(packet)
        {
            var reader = new BinaryReader(new MemoryStream(packet), Encoding.ASCII);
            if (PrefixByte != reader.ReadByte())
            {
                throw new BncsPacketException("Not a valid BNCS Packet");
            }
            if ((byte)Sid.AUTH_INFO != reader.ReadByte())
            {
                throw new BncsPacketException("Expected type was not found");
            }
            if (packet.Length != reader.ReadUInt16())
            {
                throw new BncsPacketException("Packet length does not match");
            }

            LogonType = reader.ReadUInt32();
            ServerToken = reader.ReadUInt32();
            _ = reader.ReadInt32();
            MpqFileTime = reader.ReadUInt64();

            reader.Close();
            var offset = 24;

            MpqFileName = ReadNullTerminatedString(Encoding.ASCII.GetString(Raw), ref offset);
            FormulaString = ReadNullTerminatedString(Encoding.GetEncoding("ISO-8859-1").GetString(Raw), ref offset);

            Log.Verbose($"{this.ToString()}");
        }

        public uint LogonType { get; }
        public uint ServerToken { get; }
        public ulong MpqFileTime { get; }
        public string FormulaString { get; }
        public string MpqFileName { get; }

        private static string ReadNullTerminatedString(string packet, ref int offset)
        {
            int zero = packet.IndexOf('\0', offset);
            string output;
            if (zero == -1)
            {
                zero = packet.Length;
                output = packet.Substring(offset, zero - offset);
                offset = 0;
            }
            else
            {
                output = packet.Substring(offset, zero - offset);
                offset = zero + 1;
            }
            return output;
        }

        public override string ToString()
        {
            return $"{GetType()}:\n" +
                   $"\tType: {Type}\n" +
                   $"\t{nameof(LogonType)}: {LogonType},\n" +
                   $"\t{nameof(ServerToken)}: {ServerToken},\n" +
                   $"\t{nameof(MpqFileTime)}: {MpqFileTime},\n" +
                   $"\t{nameof(FormulaString)}: {FormulaString},\n" +
                   $"\t{nameof(MpqFileName)}: {MpqFileName}\n";
        }
    }
}