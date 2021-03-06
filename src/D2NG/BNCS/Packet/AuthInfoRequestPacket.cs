﻿using System;
using System.Linq;
using System.Text;

namespace D2NG.BNCS.Packet
{
    public class AuthInfoRequestPacket : BncsPacket
    {
        private static readonly byte[] ProtocolId = BitConverter.GetBytes(0x00);

        private const string LanguageCode = "enUS";

        private static readonly byte[] LocalIp = BitConverter.GetBytes(0x00);

        private static readonly byte[] TimeZoneBias = BitConverter.GetBytes((uint)(DateTime.UtcNow.Subtract(DateTime.Now).TotalSeconds / 60));

        private static readonly byte[] MpqLocaleId = BitConverter.GetBytes(1033);

        private static readonly byte[] UserLangId = BitConverter.GetBytes(1033);

        private const string CountryAbbr = "USA\0";

        private const string Country = "United States\0";

        public AuthInfoRequestPacket()
            : this(Version)
        {
        }

        public AuthInfoRequestPacket(int version)
            : base(
                BuildPacket(
                    Sid.AUTH_INFO,
                    ProtocolId,
                    Encoding.ASCII.GetBytes(PlatformCode).Reverse().ToArray(),
                    Encoding.ASCII.GetBytes(ProductCode).Reverse().ToArray(),
                    BitConverter.GetBytes(version),
                    Encoding.ASCII.GetBytes(LanguageCode).Reverse().ToArray(),
                    LocalIp,
                    TimeZoneBias,
                    MpqLocaleId,
                    UserLangId,
                    Encoding.ASCII.GetBytes(CountryAbbr),
                    Encoding.ASCII.GetBytes(Country)
                )
            )
        {
        }
    }
}
