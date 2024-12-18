/* Copyright 2022 Christian Fortini

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Common;

namespace Insteon.Base;

/// <summary>
///  Description of the MessageFlags byte in Insteon messages
/// </summary>
internal enum MessageType : byte
{
    Direct = 0 << 5,
    DirectACK = 1 << 5,
    DirectNAK = 5 << 5,
    Broadcast = 4 << 5,
    AllLinkBroadcast = 6 << 5,
    Cleanup = 2 << 5,
    CleanupACK = 3 << 5,
    CleanupNAK = 7 << 5,
    Mask = 7 << 5
}

internal enum MessageLength : byte
{
    Standard = 0 << 4,
    Extended = 1 << 4,
    Mask = 1 << 4
}

internal enum MessageMaxHops : byte
{
    Direct = (3 << 2) + 3,
    AllLinkCleanup = (1 << 2) + 1,
    Broadcast = (3 << 2) + 3,
    AllLinkBroadcast = (3 << 2) + 3,
    Mask = 3,
}

internal enum DirectNAKErrorCodes : byte
{
    None = 0,
    LowestValid = 0xFC,
    PreNAK = 0xFC,              // Database search took too long
    IncorrectChecksum = 0xFD,
    NoLoadDetected = 0xFE,
    NotInDatabase = 0xFF
}

/// <summary>
///  Base class for all Insteon messages
/// </summary>
internal abstract class InsteonMessage : HexString
{
    internal class InvalidMessageException : Exception
    {
        internal InvalidMessageException(string message) : base(message)
        {
            Logger.Log.Debug(message);
        }

        internal InvalidMessageException(string message, Exception e) : base(message, e)
        {
            Logger.Log.Debug(message);
        }
    }

    internal InsteonMessage(HexString hexString) : base(hexString)
    {
    }

    internal InsteonID BytesAsInsteonID(int n)
    {
        return new InsteonID(Byte(n), Byte(n + 1), Byte(n + 2));
    }

    // Mainly for testing purposes
    // Helper to build the hex string for a message
    internal static HexString BuildHexString(InsteonID fromDeviceId, InsteonID toDeviceId, byte MessageFlag, byte command1, byte command2, string? data = null)
    {
        return new HexString($"{fromDeviceId.ToCommandString()}{toDeviceId.ToCommandString()}{MessageFlag:X2}{command1:X2}{command2:X2}{data}");
    }
}

/// <summary>
///  Base class for all direct Insteon messages
///  Bytes 1-3: "From" device id
///  Bytes 4-6: "To: device id
///  Byte 7: flags
///  Byte 8: Cmd1 code
///  Byte 9: Cmd2 code or DirectNAKCode if flags indicate NAK message
/// </summary>
internal abstract class InsteonDirectMessage : InsteonMessage
{
    internal InsteonDirectMessage(HexString hexString) : base(hexString)
    {
    }

    public override bool Equals(object? obj)
    {
        if (obj is InsteonDirectMessage other)
            return other != null &&
                   BytesAsString(1, 6) == other.BytesAsString(1, 6) &&
                   (MessageFlags & (byte)MessageType.Mask) == (other.MessageFlags & (byte)MessageType.Mask) &&
                   Command1 == other.Command1 &&
                   Command2 == other.Command2;
        else
            return false;
    }

    public override int GetHashCode()
    {
        return FromDeviceId.ToInt() + ToDeviceId.ToInt();
    }

    internal InsteonID FromDeviceId => BytesAsInsteonID(1);
    internal InsteonID ToDeviceId => BytesAsInsteonID(4);
    internal byte MessageFlags => Byte(7);
    internal byte Command1 => Byte(8);
    internal byte Command2 => Byte(9);
    internal DirectNAKErrorCodes DirectNAKErrorCode => (DirectNAKErrorCodes)Command2;

    internal bool IsDirect => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.Direct;
    internal bool IsDirectACK => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.DirectACK;
    internal bool IsDirectNAK => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.DirectNAK;
    internal bool IsBroadcast => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.Broadcast;
    internal bool IsAllLinkBroadcast => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.AllLinkBroadcast;
    internal bool IsCleanup => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.Cleanup;
    internal bool IsCleanupACK => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.CleanupACK;
    internal bool IsCleanupNAK => (MessageFlags & (byte)MessageType.Mask) == (byte)MessageType.CleanupNAK;
    internal bool IsStandardLength => (MessageFlags & (byte)MessageLength.Mask) == (byte)MessageLength.Standard;
    internal bool IsExtendedLength => (MessageFlags & (byte)MessageLength.Mask) == (byte)MessageLength.Extended;
}

/// <summary>
///  Direct, standard size Insteon message
/// </summary>
internal class InsteonStandardMessage : InsteonDirectMessage
{
    internal const int Length = 9;

    internal InsteonStandardMessage(HexString hexString) : base(hexString)
    {
        if (!IsStandardLength)
        {
            throw new InvalidMessageException("Invalid standard message: has extended length flag set");
        }
    }
}

/// <summary>
///  Direct, extended size Insteon message
/// </summary>
internal class InsteonExtendedMessage : InsteonDirectMessage
{
    internal const int Length = 23;
    internal const int DataLength = 14;

    internal InsteonExtendedMessage(HexString hexString) : base(hexString)
    {
        // Apparently, we sometimes receive extended messages with the Extended flag not set
        /*
        if (!IsExtendedLength)
        {
            throw new InvalidMessageException("Invalid extended message: has standard message flag set");
        }*/
    }

    internal InsteonExtendedMessage(InsteonExtendedMessage message) : base(message)
    {
    }

    internal byte DataByte(int n)
    {
        if (n < 1 || n > 14)
        {
            throw new Exception("Data bounds are 1 to 14");
        }
        return Byte(9 + n);
    }

    internal InsteonID DataBytesAsInsteonID(int n)
    {
        return new InsteonID(DataByte(n), DataByte(n + 1), DataByte(n + 2));
    }

    internal virtual bool IsValid()
    {
        return true;
    }

    internal virtual bool IsLast()
    {
        return false;
    }
}

internal class InsteonSetButtonPressedBroadcastMessage : InsteonMessage
{
    internal InsteonSetButtonPressedBroadcastMessage(HexString hexString) : base(hexString)
    {
    }

    internal DeviceKind.CategoryId DeviceCategory => (DeviceKind.CategoryId)Byte(4);
    internal int DeviceSubCategory => Byte(5);
    internal int DeviceRevision => Byte(6);
    internal bool SetButtonPressedResponder => Byte(8) == 0x01;
    internal bool SetButtonPressedController => Byte(8) == 0x02;
}

public enum LinkingAction : byte
{
    CreateResponderLink = 0,
    CreateControllerLink = 1,
    CreateAutoLink = 3,
    DeleteLink = 0xff
}

internal class InsteonAllLinkingCompletedMessage : InsteonMessage
{
    internal const int Length = 8;

    internal InsteonAllLinkingCompletedMessage(HexString hexString) : base(hexString)
    {
    }

    // For testing purposes
    internal InsteonAllLinkingCompletedMessage(LinkingAction action, byte goup, InsteonID fromDeviceId, DeviceKind.CategoryId deviceCategory, byte deviceSubCategory, byte deviceFirmwareRevision) 
        : base(new HexString($"{(byte)action:X2}{(byte)goup:X2}{fromDeviceId.ToCommandString()}{(byte)deviceCategory:X2}{(byte)deviceSubCategory:X2}{(byte)deviceFirmwareRevision:X2}"))
    {
    }

    internal LinkingAction Action => (LinkingAction)Byte(1);
    internal byte Group => Byte(2);
    internal InsteonID FromDeviceId => BytesAsInsteonID(3);
    internal DeviceKind.CategoryId DeviceCategory => (DeviceKind.CategoryId)Byte(6);
    internal byte DeviceSubCategory => Byte(7);
    internal byte DeviceFirmwareRevision => Byte(8);
}
