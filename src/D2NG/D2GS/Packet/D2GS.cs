﻿namespace D2NG.D2GS.Packet
{
    public enum D2gs
    {
        WALKTOLOCATION = 0x01,
        WALKTOENTITY = 0x02,
        RUNTOLOCATION = 0x03,
        RUNTOENTITY = 0x04,
        LEFTSKILLONLOCATION = 0x05,
        LEFTSKILLONENTITY = 0x06,
        LEFTSKILLONENTITYEX = 0x07,
        LEFTSKILLONLOCATIONEX = 0x08,
        LEFTSKILLONENTITYEX2 = 0x09,
        LEFTSKILLONENTITYEX3 = 0x0A,
        RIGHTSKILLONLOCATION = 0x0C,
        RIGHTSKILLONENTITY = 0x0D,
        RIGHTSKILLONENTITYEX = 0x0E,
        RIGHTSKILLONLOCATIONEX = 0x0F,
        RIGHTSKILLONENTITYEX2 = 0x10,
        CHARTOOBJ = 0x10,
        RIGHTSKILLONENTITYEX3 = 0x11,
        INTERACTWITHENTITY = 0x13,
        OVERHEADMESSAGE = 0x14,
        PICKUPITEM = 0x16,
        DROPITEM = 0x17,
        ITEMTOBUFFER = 0x18,
        PICKUPBUFFERITEM = 0x19,
        SMALLGOLDPICKUP = 0x19,
        ITEMTOBODY = 0x1A,
        SWAP2HANDEDITEM = 0x1B,
        PICKUPBODYITEM = 0x1C,
        SWITCHBODYITEM = 0x1D,
        SETBYTEATTR = 0x1D,
        SETWORDATTR = 0x1E,
        SWITCHINVENTORYITEM = 0x1F,
        SETDWORDATTR = 0x1F,
        USEITEM = 0x20,
        STACKITEM = 0x21,
        REMOVESTACKITEM = 0x22,
        ITEMTOBELT = 0x23,
        REMOVEBELTITEM = 0x24,
        SWITCHBELTITEM = 0x25,
        USEBELTITEM = 0x26,
        INSERTSOCKETITEM = 0x28,
        SCROLLTOTOME = 0x29,
        ITEMTOCUBE = 0x2A,
        UNSELECTOBJ = 0x2D,
        NPCINIT = 0x2F,
        NPCCANCEL = 0x30,
        NPCBUY = 0x32,
        NPCSELL = 0x33,
        NPCTRADE = 0x38,
        CHARACTERPHRASE = 0x3F,
        WAYPOINT = 0x49,
        TRADE = 0x4F,
        DROPGOLD = 0x50,
        WORLDOBJECT = 0x51,
        STARTGAME = 0x5C,
        PARTY = 0x5E,
        POTIONTOMERCENARY = 0x61,
        GAMELOGON = 0x68,
        ENTERGAMEENVIRONMENT = 0x6A,
        PING = 0x6D,
        TRADEACTION = 0x77,
        LOGONRESPONSE = 0x7A,
        UNIQUEEVENTS = 0x89,
        NEGOTIATECOMPRESSION = 0xAF,
        GAMEFLAGS = 0x01
    }
}
