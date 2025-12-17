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
using Insteon.Base;
using Insteon.Commands;
using Insteon.Model;
using System.Text;
using static Insteon.Commands.CreateScheduleCommand;
using static Insteon.Model.AllLinkRecord;

namespace ViewModel.Console;

public interface ICommandProcessor
{
    Task ProcessAsync(string command);
}

public sealed class CommandProcessor : ICommandProcessor
{
    enum CommandCategory { Console, IM, Device }
    static (CommandCategory CommandCategory, string commandName, Func<string[], Task> commandMethod, string commandHelp)[] commandEntries =
    {
        ( CommandCategory.Console, "Help", ProcessHelp, "- Lists all commands" ),
        ( CommandCategory.Console, "Clear", null!, "- Clears the console"),
        ( CommandCategory.Console, "Ouput", null!, "verbose|info|warning|error|critical"),

        // Custom command 
        ( CommandCategory.IM, CustomHubCommand.Name, ProcessCustomHubCommand, CustomHubCommand.Help ),

        // Hub (IM) commands
        ( CommandCategory.IM, GetIMInfoCommand.Name, ProcessCommand<GetIMInfoCommandFactory>, GetIMInfoCommand.Help ),
        ( CommandCategory.IM, SendAllLinkCommand.Name, ProcessSendAllLinkCommand, SendAllLinkCommand.Help ),
        ( CommandCategory.IM, ResetIMCommand.Name, ProcessCommand<ResetIMCommandFactory>, ResetIMCommand.Help ),
        ( CommandCategory.IM, GetIMFirstAllLinkRecordCommand.Name, ProcessCommand<GetIMFirstAllLinkRecordCommandFactory>, GetIMFirstAllLinkRecordCommand.Help),
        ( CommandCategory.IM, GetIMNextAllLinkRecordCommand.Name, ProcessCommand<GetIMNextAllLinkRecordCommandFactory>, GetIMNextAllLinkRecordCommand.Help),
        ( CommandCategory.IM, GetIMDatabaseCommand.Name, ProcessCommand<GetIMDatabaseCommandFactory>, GetIMDatabaseCommand.Help),

        // Hub (IM) commands - Linking
        ( CommandCategory.IM, TriggerGroupCommand.Name, ProcessTriggerGroupCommand, TriggerGroupCommand.Help ),
        ( CommandCategory.IM, StartIMAllLinkingCommand.Name, ProcessStartIMAllLinkingCommand, StartIMAllLinkingCommand.Help ),
        ( CommandCategory.IM, CancelIMAllLinkingCommand.Name, ProcessCancelIMAllLinkingCommand, CancelIMAllLinkingCommand.Help ),
        ( CommandCategory.IM, ManageIMAllLinkRecordCommand.Name, ProcessManageAllLinkRecordCommand, ManageIMAllLinkRecordCommand.Help ),

        // HubConfig commands
        ( CommandCategory.IM, CreateScheduleCommand.Name, ProcessCreateScheduleCommand, CreateScheduleCommand.Help ),

        // Device commands
        ( CommandCategory.Device, LightOnCommand.Name, ProcessLightOnCommand, LightOnCommand.Help),
        ( CommandCategory.Device, FastLightOnCommand.Name, ProcessFastLightOnCommand, LightOnCommand.Help),
        ( CommandCategory.Device, LightOffCommand.Name, ProcessLightOffCommand, LightOffCommand.Help),
        ( CommandCategory.Device, FastLightOffCommand.Name, ProcessFastLightOffCommand, LightOffCommand.Help),
        ( CommandCategory.Device, BrighterCommand.Name, ProcessDeviceCommand<BrighterCommandFactory>, BrighterCommand.Help ),
        ( CommandCategory.Device, DimmerCommand.Name, ProcessDeviceCommand<DimmerCommandFactory>, DimmerCommand.Help ),

        ( CommandCategory.Device, GetOnLevelCommand.Name, ProcessDeviceCommand<GetOnLevelCommandFactory>, GetOnLevelCommand.Help),
        ( CommandCategory.Device, GetDBDeltaCommand.Name, ProcessDeviceCommand<GetDBDeltaCommandFactory>, GetDBDeltaCommand.Help),
        ( CommandCategory.Device, GetInsteonEngineVersionCommand.Name, ProcessDeviceCommand<GetInsteonEngineVersionCommandFactory>, GetInsteonEngineVersionCommand.Help),
        ( CommandCategory.Device, GetProductDataCommand.Name, ProcessDeviceCommand<GetProductDataCommandFactory>, GetProductDataCommand.Help),
        ( CommandCategory.Device, GetDeviceTextStringCommand.Name, ProcessDeviceCommand<GetDeviceTextStringCommandFactory>, GetDeviceTextStringCommand.Help),
        ( CommandCategory.Device, PingCommand.Name, ProcessDeviceCommand<PingCommandFactory>, PingCommand.Help),

        ( CommandCategory.Device, GetOperatingFlagsCommand.Name, ProcessDeviceCommand<GetOperatingFlagsCommandFactory>, GetOperatingFlagsCommand.Help ),
        ( CommandCategory.Device, SetOperatingFlagCommand.Name,  ProcessSetOperatingFlagCommand, SetOperatingFlagCommand.Help ),

        ( CommandCategory.Device, GetOpFlags2Command.Name, ProcessDeviceCommand<GetOpFlags2CommandFactory>, GetOpFlags2Command.Help ),
        ( CommandCategory.Device, SetOpFlag2Command.Name,  ProcessSetOpFlag2Command, SetOpFlag2Command.Help ),

        ( CommandCategory.Device, GetPropertiesForGroupCommand.Name, ProcessGetGroupPropCommand, GetPropertiesForGroupCommand.Help ),
        ( CommandCategory.Device, SetFollowMaskForGroupCommand.Name, ProcessSetFollowMaskForGroupCommand, SetFollowMaskForGroupCommand.Help ),
        ( CommandCategory.Device, SetFollowOffMaskForGroupCommand.Name, ProcessSetFollowOffMaskForGroupCommand, SetFollowOffMaskForGroupCommand.Help ),
        ( CommandCategory.Device, SetOnLevelForGroupCommand.Name, ProcessSetOnLevelForGroupCommand, SetOnLevelForGroupCommand.Help ),
        ( CommandCategory.Device, SetRampRateForGroupCommand.Name, ProcessSetRampRateForGroupCommand, SetRampRateForGroupCommand.Help ),
        ( CommandCategory.Device, SetLEDBrightnessCommand.Name, ProcessSetLEDBrightnessCommand, SetLEDBrightnessCommand.Help ),

        ( CommandCategory.Device, GetDeviceDatabaseCommand.Name, ProcessGetDeviceDatabaseCommand, GetDeviceDatabaseCommand.Help ),
        ( CommandCategory.Device, GetDeviceLinkRecordCommand.Name, ProcessGetDeviceLinkRecordCommand, GetDeviceLinkRecordCommand.Help ),
        ( CommandCategory.Device, SetDeviceLinkRecordCommand.Name, ProcessSetDeviceLinkRecordCommand, SetDeviceLinkRecordCommand.Help ),

        ( CommandCategory.Device, EnterLinkingModeCommand.Name, ProcessEnterLinkingModeCommand, EnterLinkingModeCommand.Help ),
        ( CommandCategory.Device, EnterUnlinkingModeCommand.Name, ProcessEnterUnlinkingModeCommand, EnterUnlinkingModeCommand.Help ),

        ( CommandCategory.Device, AssignToAllLinkGroupCommand.Name, ProcessAssignToAllLinkGroupCommand, AssignToAllLinkGroupCommand.Help ),
        ( CommandCategory.Device, DeleteFromAllLinkGroupCommand.Name, ProcessDeleteFromAllLinkGroupCommand, DeleteFromAllLinkGroupCommand.Help ),

        ( CommandCategory.Device, SetTriggerGroupBitMaskCommand.Name, ProcessSetTriggetGroupBitMaskCommand, SetTriggerGroupBitMaskCommand.Help ),
        ( CommandCategory.Device, SetOnOffBitMaskCommand.Name, ProcessSetOnOffBitMaskCommand, SetOnOffBitMaskCommand.Help ),
        ( CommandCategory.Device, SetNonToggleMaskCommand.Name, ProcessSetNonToggleMaskCommand, SetNonToggleMaskCommand.Help ),
        ( CommandCategory.Device, SetLEDBitMaskCommand.Name, ProcessSetLEDBitMaskCommand, SetLEDBitMaskCommand.Help ),
    };

    public async Task ProcessAsync(string command)
    {
        command = command.Trim();
        string[] tokens = command.Split(' ');
        bool wasCommandRun = false;

        try
        {
            foreach (var cmdEntry in commandEntries)
            {
                if (tokens[0].Equals(cmdEntry.commandName, StringComparison.OrdinalIgnoreCase))
                {
                    await cmdEntry.commandMethod(tokens);
                    wasCommandRun = true;
                    break;
                }
            }

            if (!wasCommandRun)
            {
                throw new Exception("Unknown command: " + tokens[0]);
            }
        }
        catch (Exception e)
        {
            Logger.Log.CommandEcho(command);
            Logger.Log.CommandOutput(e.Message);
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async Task ProcessHelp(string[] tokens)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        Logger.Log.CommandEcho("Help ");

        StringBuilder commandOutput = new StringBuilder();

        commandOutput.AppendLine("Console commands:");
        foreach (var cmdEntry in commandEntries)
        {
            if (cmdEntry.CommandCategory == CommandCategory.Console)
            {
                commandOutput.AppendLine("  " + cmdEntry.commandName + " " + cmdEntry.commandHelp);
            }
        }

        commandOutput.AppendLine("IM commands:");
        foreach (var cmdEntry in commandEntries)
        {
            if (cmdEntry.CommandCategory == CommandCategory.IM)
            {
                commandOutput.AppendLine("  " + cmdEntry.commandName + " " + cmdEntry.commandHelp);
            }
        }

        commandOutput.AppendLine("Device commands:");
        foreach (var cmdEntry in commandEntries)
        {
            if (cmdEntry.CommandCategory == CommandCategory.Device)
            {
                commandOutput.AppendLine("  " + cmdEntry.commandName + " " + cmdEntry.commandHelp);
            }
        }

        Logger.Log.CommandOutput(commandOutput.ToString());
    }

    private interface ICommandFactory
    {
        Command CreateInstance();
    }

    private interface IDeviceCommandFactory
    {
        Command CreateInstance(InsteonID deviceID);
    }

    /// <summary>
    ///  Generic class to process a command for the Hub (IM)
    /// </summary>
    /// <typeparam name="CommandFactory">The factory of the class that handles the command</typeparam>
    /// <param name="tokens">Tokenized command string from user input</param>
    private static async Task ProcessCommand<CommandFactory>(string[] tokens)
        where CommandFactory : ICommandFactory, new()
    {
        if (tokens.Count() >= 1)
        {
            var deviceCommand = new CommandFactory().CreateInstance();
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(tokens[0]);
        }
    }

    private class ResetIMCommandFactory : ICommandFactory
    {
        public Command CreateInstance() { return new ResetIMCommand(Settings.Holder.House.Gateway); }
    }

    private class GetIMFirstAllLinkRecordCommandFactory : ICommandFactory
    {
        public Command CreateInstance() { return new GetIMFirstAllLinkRecordCommand(Settings.Holder.House.Gateway); }
    }

    private class GetIMNextAllLinkRecordCommandFactory : ICommandFactory
    {
        public Command CreateInstance() { return new GetIMNextAllLinkRecordCommand(Settings.Holder.House.Gateway); }
    }

    private class GetIMDatabaseCommandFactory : ICommandFactory
    {
        public Command CreateInstance() { return new GetIMDatabaseCommand(Settings.Holder.House.Gateway); }
    }

    private class GetIMInfoCommandFactory : ICommandFactory
    {
        public Command CreateInstance() { return new GetIMInfoCommand(Settings.Holder.House.Gateway); }
    }

    /// <summary>
    ///  Generic class to process a command for a given device on the network
    /// </summary>
    /// <typeparam name="DeviceCommandFactory">The factory of the class that handles the command</typeparam>
    /// <param name="tokens">Tokenized comamnd string from user input</param>
    private static async Task ProcessDeviceCommand<DeviceCommandFactory>(string[] tokens)
        where DeviceCommandFactory : IDeviceCommandFactory, new()
    {
        if (tokens.Count() > 1)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);

            var deviceCommand = new DeviceCommandFactory().CreateInstance(deviceID);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(tokens[0] + " <DeviceID>");
        }
    }

    private class BrighterCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new BrighterCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class DimmerCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new DimmerCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class GetOnLevelCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new GetOnLevelCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class GetDBDeltaCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new GetDBDeltaCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class GetInsteonEngineVersionCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new GetInsteonEngineVersionCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class GetProductDataCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new GetProductDataCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class GetDeviceTextStringCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new GetDeviceTextStringCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class PingCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new PingCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class GetOperatingFlagsCommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new GetOperatingFlagsCommand(Settings.Holder.House.Gateway, deviceID); }
    }

    private class GetOpFlags2CommandFactory : IDeviceCommandFactory
    {
        public Command CreateInstance(InsteonID deviceID) { return new GetOpFlags2Command(Settings.Holder.House.Gateway, deviceID); }
    }

    private static async Task ProcessLightOnCommand(string[] tokens)
    { 
        if (tokens.Count() > 1)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);

            byte level = 255;
            if (tokens.Count() > 2)
            {
                try
                {
                    level = ParseByteNumber(tokens[2]);
                }
                catch(Exception)
                {
                    throw new Exception("level must be between 0 and 255");
                }
            }

            var deviceCommand = new LightOnCommand(Settings.Holder.House.Gateway, deviceID, (byte)level);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(LightOnCommand.Name + LightOnCommand.Help);
        }
    }

    private static async Task ProcessLightOffCommand(string[] tokens)
    {
        if (tokens.Count() > 1)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);

            var deviceCommand = new LightOffCommand(Settings.Holder.House.Gateway, deviceID);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(LightOffCommand.Name + LightOffCommand.Help);
        }
    }

    private static async Task ProcessFastLightOnCommand(string[] tokens)
    {
        if (tokens.Count() > 1)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);

            byte level = 255;
            if (tokens.Count() > 2)
            {
                try
                {
                    level = ParseByteNumber(tokens[2]);
                }
                catch (Exception)
                {
                    throw new Exception("level must be between 0 and 255");
                }
            }

            var deviceCommand = new FastLightOnCommand(Settings.Holder.House.Gateway, deviceID, (byte)level);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(FastLightOnCommand.Name + FastLightOnCommand.Help);
        }
    }

    private static async Task ProcessFastLightOffCommand(string[] tokens)
    {
        if (tokens.Count() > 1)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);

            var deviceCommand = new FastLightOffCommand(Settings.Holder.House.Gateway, deviceID);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(FastLightOffCommand.Name + FastLightOffCommand.Help);
        }
    }

    private static async Task ProcessGetGroupPropCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);

            var deviceCommand = new GetPropertiesForGroupCommand(Settings.Holder.House.Gateway, deviceID, group);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(GetPropertiesForGroupCommand.Name + " " + GetPropertiesForGroupCommand.Help);
        }
    }

    private static async Task ProcessSetOperatingFlagCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte code = ParseByteNumber(tokens[2]);

            var deviceCommand = new SetOperatingFlagCommand(Settings.Holder.House.Gateway, deviceID, code);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetOperatingFlagCommand.Name + " " + SetOperatingFlagCommand.Help);
        }
    }

    private static async Task ProcessSetOpFlag2Command(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte code = ParseByteNumber(tokens[2]);

            var deviceCommand = new SetOpFlag2Command(Settings.Holder.House.Gateway, deviceID, code);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetOpFlag2Command.Name + " " + SetOpFlag2Command.Help);
        }
    }

    private static async Task ProcessSetFollowMaskForGroupCommand(string[] tokens)
    {
        if (tokens.Count() >= 4)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);
            byte followMask = ParseByteNumber(tokens[3]);

            var deviceCommand = new SetFollowMaskForGroupCommand(Settings.Holder.House.Gateway, deviceID, group, followMask);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetFollowMaskForGroupCommand.Name + " " + SetFollowMaskForGroupCommand.Help);
        }
    }

    private static async Task ProcessSetFollowOffMaskForGroupCommand(string[] tokens)
    {
        if (tokens.Count() >= 4)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);
            byte followOffMask = ParseByteNumber(tokens[3]);

            var deviceCommand = new SetFollowOffMaskForGroupCommand(Settings.Holder.House.Gateway, deviceID, group, followOffMask);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetFollowOffMaskForGroupCommand.Name + " " + SetFollowOffMaskForGroupCommand.Help);
        }
    }

    private static async Task ProcessSetOnLevelForGroupCommand(string[] tokens)
    {
        if (tokens.Count() >= 4)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);
            byte onLevel = ParseByteNumber(tokens[3]);

            var deviceCommand = new SetOnLevelForGroupCommand(Settings.Holder.House.Gateway, deviceID, group, onLevel);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetOnLevelForGroupCommand.Name + " " + SetOnLevelForGroupCommand.Help);
        }
    }

    private static async Task ProcessSetRampRateForGroupCommand(string[] tokens)
    {
        if (tokens.Count() >= 4)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);
            byte rampRate = ParseByteNumber(tokens[3]);

            var deviceCommand = new SetRampRateForGroupCommand(Settings.Holder.House.Gateway, deviceID, group, rampRate);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetRampRateForGroupCommand.Name + " " + SetRampRateForGroupCommand.Help);
        }
    }

    private static async Task ProcessSetLEDBrightnessCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte brightness = ParseByteNumber(tokens[2]);

            var deviceCommand = new SetLEDBrightnessCommand(Settings.Holder.House.Gateway, deviceID, brightness);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetLEDBrightnessCommand.Name + " " + SetLEDBrightnessCommand.Help);
        }
    }

    public static async Task ProcessSetTriggetGroupBitMaskCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            var deviceID = new InsteonID(tokens[1]);
            var triggerGroupMask = ParseByteNumber(tokens[2]);

            var deviceCommand = new SetTriggerGroupBitMaskCommand(Settings.Holder.House.Gateway, deviceID, triggerGroupMask);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetTriggerGroupBitMaskCommand.Name + " " + SetTriggerGroupBitMaskCommand.Help);
        }
    }

    public static async Task ProcessSetOnOffBitMaskCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            var deviceID = new InsteonID(tokens[1]);
            var triggerGroupMask = ParseByteNumber(tokens[2]);

            var deviceCommand = new SetOnOffBitMaskCommand(Settings.Holder.House.Gateway, deviceID, triggerGroupMask);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetOnOffBitMaskCommand.Name + " " + SetOnOffBitMaskCommand.Help);
        }
    }

    public static async Task ProcessSetNonToggleMaskCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            var deviceID = new InsteonID(tokens[1]);
            var nonToggleMask = ParseByteNumber(tokens[2]);

            var deviceCommand = new SetNonToggleMaskCommand(Settings.Holder.House.Gateway, deviceID, nonToggleMask);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetNonToggleMaskCommand.Name + " " + SetNonToggleMaskCommand.Help);
        }
    }

    public static async Task ProcessSetLEDBitMaskCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            var deviceID = new InsteonID(tokens[1]);
            var nonToggleMask = ParseByteNumber(tokens[2]);

            var deviceCommand = new SetLEDBitMaskCommand(Settings.Holder.House.Gateway, deviceID, nonToggleMask);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(SetLEDBitMaskCommand.Name + " " + SetLEDBitMaskCommand.Help);
        }
    }

    private static async Task ProcessGetDeviceLinkRecordCommand(string[] tokens)
    {
        if (tokens.Count() >= 2)
        {
            string s = tokens[1];
            InsteonID deviceID = new InsteonID(s);

            int seq = -1;
            if (tokens.Count() >= 3)
            {
                seq = ParseByteNumber(tokens[2]);
            }

            int engineVersion = 2;
            if (tokens.Count() >= 4)
            {
                engineVersion = ParseByteNumber(tokens[3]);
            }

            if (seq >= 0)
            {
                var deviceCommand = new GetDeviceLinkRecordCommand(Settings.Holder.House.Gateway, deviceID, seq, engineVersion);
                await deviceCommand.TryRunAsync();
            }
        }
        else
        {
            throw new Exception(GetDeviceLinkRecordCommand.Name + " " + GetDeviceLinkRecordCommand.Help);
        }
    }

    private static async Task ProcessSetDeviceLinkRecordCommand(string[] tokens)
    {
        if (tokens.Count() >= 9)
        {
            string s = tokens[1];
            InsteonID deviceID = new InsteonID(s);

            if (tokens.Count() >= 3)
            {
                int seq = ParseByteNumber(tokens[2]);

                if (seq >= 0)
                {
                    AllLinkRecord allLinkRecord = new AllLinkRecord
                    {
                        DestID = new InsteonID(tokens[3]),
                        Group = ParseByteNumber(tokens[4]),
                        Flags = ParseFlags(tokens[5]),
                        Data1 = ParseByteNumber(tokens[6]),
                        Data2 = ParseByteNumber(tokens[7]),
                        Data3 = ParseByteNumber(tokens[8])
                    };

                    //allLinkRecord.Flags = ParseNumber(tokens[5]);

                    var deviceCommand = new SetDeviceLinkRecordCommand(Settings.Holder.House.Gateway, deviceID, seq, allLinkRecord);
                    await deviceCommand.TryRunAsync();
                }
            }
        }
        else
        {
            throw new Exception(SetDeviceLinkRecordCommand.Name + " " + SetDeviceLinkRecordCommand.Help);
        }
    }

    private static async Task ProcessGetDeviceDatabaseCommand(string[] tokens)
    {
        if (tokens.Count() >= 2)
        {
            string s = tokens[1];
            InsteonID deviceID = new InsteonID(s);

            int engineVersion = 2;
            if (tokens.Count() >= 3)
            {
                engineVersion = ParseByteNumber(tokens[2]);
            }

            var deviceCommand = new GetDeviceDatabaseCommand(Settings.Holder.House.Gateway, deviceID, engineVersion);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(GetDeviceDatabaseCommand.Name + " " + GetDeviceDatabaseCommand.Help);
        }
    }

    private static async Task ProcessTriggerGroupCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);

            byte level = 0;
            bool usePassedLevel = false;
            if (tokens.Count() >= 4)
            {
                level = ParseByteNumber(tokens[3]);
                usePassedLevel = true;
            }

            var deviceCommand = new TriggerGroupCommand(Settings.Holder.House.Gateway, deviceID, group, level, usePassedLevel, false);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            throw new Exception(TriggerGroupCommand.Name + " " + TriggerGroupCommand.Help);
        }
    }

    private static async Task ProcessSendAllLinkCommand(string[] tokens)
    {
        bool success = false;

        if (tokens.Count() >= 3)
        {
            success = true;
            byte group = ParseByteNumber(tokens[1]);
            byte cmd = 0;

            if (tokens[2].Equals(LightOnCommand.Name, StringComparison.OrdinalIgnoreCase))
                cmd = LightOnCommand.CommandCode;
            else if (tokens[2].Equals(LightOffCommand.Name, StringComparison.OrdinalIgnoreCase))
                cmd = LightOffCommand.CommandCode;
            else if (tokens[2].Equals(FastLightOnCommand.Name, StringComparison.OrdinalIgnoreCase))
                cmd = FastLightOnCommand.CommandCode;
            else if (tokens[2].Equals(FastLightOffCommand.Name, StringComparison.OrdinalIgnoreCase))
                cmd = FastLightOffCommand.CommandCode;
            else if (tokens[2].Equals(BrighterCommand.Name, StringComparison.OrdinalIgnoreCase))
                cmd = BrighterCommand.CommandCode;
            else if (tokens[2].Equals(DimmerCommand.Name, StringComparison.OrdinalIgnoreCase))
                cmd = DimmerCommand.CommandCode;
            else if (tokens[2].Equals(GetOnLevelCommand.Name, StringComparison.OrdinalIgnoreCase))
                cmd = GetOnLevelCommand.CommandCode;
            else
                success = false;

            byte level = 0;
            if (tokens.Count() >= 4)
            {
                level = ParseByteNumber(tokens[3]);
            }

            var hubCommand = new SendAllLinkCommand(Settings.Holder.House.Gateway, group, cmd, level);
            await hubCommand.TryRunAsync();
        }

        if (!success)
        {
            throw new Exception(SendAllLinkCommand.Name + " " + SendAllLinkCommand.Help);
        }
    }

    private static async Task ProcessEnterLinkingModeCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);

            var command = new EnterLinkingModeCommand(Settings.Holder.House.Gateway, deviceID, group);
            await command.TryRunAsync();
        }
        else
        {
            throw new Exception(EnterLinkingModeCommand.Name + " " + EnterLinkingModeCommand.Help);
        }
    }

    private static async Task ProcessEnterUnlinkingModeCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);

            var command = new EnterUnlinkingModeCommand(Settings.Holder.House.Gateway, deviceID, group);
            await command.TryRunAsync();
        }
        else
        {
            throw new Exception(EnterUnlinkingModeCommand.Name + " " + EnterUnlinkingModeCommand.Help);
        }
    }

    private static async Task ProcessAssignToAllLinkGroupCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);

            var command = new AssignToAllLinkGroupCommand(Settings.Holder.House.Gateway, deviceID, group);
            await command.TryRunAsync();
        }
        else
        {
            throw new Exception(AssignToAllLinkGroupCommand.Name + " " + AssignToAllLinkGroupCommand.Help);
        }
    }

    private static async Task ProcessDeleteFromAllLinkGroupCommand(string[] tokens)
    {
        if (tokens.Count() >= 3)
        {
            InsteonID deviceID = new InsteonID(tokens[1]);
            byte group = ParseByteNumber(tokens[2]);

            var command = new DeleteFromAllLinkGroupCommand(Settings.Holder.House.Gateway, deviceID, group);
            await command.TryRunAsync();
        }
        else
        {
            throw new Exception(DeleteFromAllLinkGroupCommand.Name + " " + DeleteFromAllLinkGroupCommand.Help);
        }
    }

    private static async Task ProcessStartIMAllLinkingCommand(string[] tokens)
    {
        bool syntaxError = false;
        bool hasDevice = false;
        InsteonID deviceID = new InsteonID();
        byte group = 0;
        LinkingAction action = LinkingAction.CreateAutoLink;

        int index = 1;
        if (tokens.Count() >= index + 1)
        {
            try
            {
                // DeviceID is optional
                deviceID = new InsteonID(tokens[index]);
                hasDevice = true;
                index++;
            }
            catch (InvalidInsteonIDException)
            { }

            if (tokens.Count() >= index + 1)
            {
                group = ParseByteNumber(tokens[index]);
                index++;

                if (tokens.Count() >= index + 1)
                {
                    action = StartIMAllLinkingCommand.LinkingActionFromString(tokens[index]);
                }
            }
        }

        if (hasDevice)
        {
            var deviceCommand = new StartIMAllLinkingCommand(Settings.Holder.House.Gateway, deviceID, group, action);
            await deviceCommand.TryRunAsync();
        }
        else
        {
            var deviceCommand = new StartIMAllLinkingCommand(Settings.Holder.House.Gateway, group, action);
            await deviceCommand.TryRunAsync();
        }

        if (syntaxError)
        {
            throw new Exception(StartIMAllLinkingCommand.Name + " " + StartIMAllLinkingCommand.Help);
        }
    }


    private static async Task ProcessCancelIMAllLinkingCommand(string[] tokens)
    {
        // If the StartIMLinking command is running, cancel it, 
        // otherwise just queue and run the CancelIMLinking command

        if (Command.Running != null && Command.Running.GetType() == typeof(StartIMAllLinkingCommand))
        {
            await Command.Running.Cancel();
        }

        var deviceCommand = new CancelIMAllLinkingCommand(Settings.Holder.House.Gateway);
        await deviceCommand.TryRunAsync();
    }

    private static async Task ProcessManageAllLinkRecordCommand(string[] tokens)
    {
        if (tokens.Count() >= 2)
        {
            AllLinkRecord? allLinkRecord = null;
            var controlCode = ManageIMAllLinkRecordCommand.ControlCodeFromString(tokens[1]);
            switch (controlCode)
            {
                case ManageIMAllLinkRecordCommand.ControlCodes.Invalid:
                    throw new Exception("Invalid Control Code: '" + tokens[1] + "'");

                case ManageIMAllLinkRecordCommand.ControlCodes.FindFirst:
                case ManageIMAllLinkRecordCommand.ControlCodes.FindNext:
                    if (tokens.Count() == 4)
                    {
                        allLinkRecord = new AllLinkRecord
                        {
                            DestID = new InsteonID(tokens[2]),
                            Group = ParseByteNumber(tokens[3]),
                            Flags = 0,
                        };
                    }
                    else if (tokens.Count() >= 5)
                    {
                        allLinkRecord = new AllLinkRecord
                        {
                            DestID = new InsteonID(tokens[2]),
                            Group = ParseByteNumber(tokens[3]),
                            Flags = ParseFlags(tokens[4]),
                        };
                    }
                    break;

                default:
                    if (tokens.Count() >= 8)
                    {
                        allLinkRecord = new AllLinkRecord
                        {
                            DestID = new InsteonID(tokens[2]),
                            Group = ParseByteNumber(tokens[3]),
                            Flags = ParseFlags(tokens[4]),
                            Data1 = ParseByteNumber(tokens[5]),
                            Data2 = ParseByteNumber(tokens[6]),
                            Data3 = ParseByteNumber(tokens[7]),
                        };
                    }
                    break;
            }
            
            if (allLinkRecord != null)
            {
                var command = new ManageIMAllLinkRecordCommand(Settings.Holder.House.Gateway, controlCode, allLinkRecord);
                await command.TryRunAsync();
                return;
            }
        }

        throw new Exception(ManageIMAllLinkRecordCommand.Name + " " + ManageIMAllLinkRecordCommand.Help);
    }

    private static async Task ProcessCreateScheduleCommand(string[] tokens)
    {
        int idx = 0;
        byte group = 0;
        TimeEventType startTimeType = TimeEventType.Time;
        DateTime startTime = DateTime.Parse("00:00");
        bool amStart = false;
        bool pmStart = false;
        TimeEventType endTimeType = TimeEventType.Time;
        DateTime endTime = DateTime.Parse("00:00");
        bool amEnd = false;
        bool pmEnd = false;
        bool monday = false;
        bool tuesday = false;
        bool wednesday = false;
        bool thursday = false;
        bool friday = false;
        bool saturday = false;
        bool sunday = false;

        if (++idx < tokens.Length)
        {
            group = ParseByteNumber(tokens[idx]);
        }

        if (++idx < tokens.Length)
        {
            if (tokens[idx].Equals("sunrise", StringComparison.OrdinalIgnoreCase))
            {
                startTimeType = TimeEventType.Sunrise;
            }
            else if (tokens[idx].Equals("sunset", StringComparison.OrdinalIgnoreCase))
            {
                startTimeType = TimeEventType.Sunset;
            }
            else
            {
                startTimeType = TimeEventType.Time;
                startTime = DateTime.Parse(tokens[idx]);
                if (startTime < DateTime.Parse("12:00pm"))
                {
                    amStart = true;
                    pmStart = false;
                }
                else
                {
                    amStart = false;
                    pmStart = true;
                }
            }
        }

        if (++idx < tokens.Length)
        {
            if (tokens[idx].Equals("sunrise", StringComparison.OrdinalIgnoreCase))
            {
                endTimeType = TimeEventType.Sunrise;
            }
            else if (tokens[idx].Equals("sunset", StringComparison.OrdinalIgnoreCase))
            {
                endTimeType = TimeEventType.Sunset;

            }
            else
            {
                endTimeType = TimeEventType.Time;
                endTime = DateTime.Parse(tokens[idx]);
                if (endTime < DateTime.Parse("12:00pm"))
                {
                    amEnd = true;
                    pmEnd = false;
                }
                else
                {
                    amEnd = false;
                    pmEnd = true;
                }
            }
        }

        bool isWeekday = true;
        while (isWeekday && ++idx < tokens.Length)
        {
            switch (tokens[idx].ToLowerInvariant())
            {
                case "mon":
                    monday = true;
                    break;

                case "tues":
                    tuesday = true;
                    break;

                case "wed":
                    wednesday = true;
                    break;

                case "thurs":
                    thursday = true;
                    break;

                case "fri":
                    friday = true;
                    break;

                case "sat":
                    saturday = true;
                    break;

                case "sunday":
                    sunday = true;
                    break;

                default:
                    isWeekday = false;
                    break;
            }
        }

        var command = new CreateScheduleCommand(Settings.Holder.House.Gateway,
            group: group, name: $"Sch{group}", show: true,
            startTimeType, startTime, amStart, pmStart, 
            endTimeType, endTime, amEnd, pmEnd,
            monday, tuesday, wednesday, thursday, friday, saturday, sunday);

        await command.TryRunAsync();
    }

    private static async Task ProcessCustomHubCommand(string[] tokens)
    {
        if (tokens.Length >= 3)
        {
            int commandType = int.Parse(tokens[1]);
            if (commandType < 0 || commandType > 3)
                throw new FormatException($"'First parameter should be between 0 and 3");

            string commandString = tokens[2];

            var command = new CustomHubCommand(Settings.Holder.House.Gateway, commandType, commandString);
            await command.TryRunAsync(maxAttempts: 1);
            return;
        }

        throw new Exception(CustomHubCommand.Name + " " + CustomHubCommand.Help);
    }

    public static byte ParseByteNumber(string input)
    {
        if (input.StartsWith("0x") || input.StartsWith("0X"))
        {
            // Parse as hexadecimal
            try
            {
                return byte.Parse(input.Substring(2), System.Globalization.NumberStyles.HexNumber);
            }
            catch (FormatException e)
            {
                throw new FormatException($"'{input}' should be an hex number", e);
            }
        }
        else if (input.EndsWith('%'))
        {
            // Parse as percentage (0% - 100%) and map to 0-255
            try
            {
                var percentText = input.TrimEnd('%');
                var percent = double.Parse(percentText);

                // Clamp to [0, 1D]
                if (percent < 0) percent = 0;
                if (percent >= 100) percent = 100;

                // Scale to 0-255 with conventional rounding
                var scaled = (int)Math.Round(percent * 255.0 / 100, MidpointRounding.AwayFromZero);
                return (byte)scaled;
            }
            catch (FormatException e)
            {
                throw new FormatException($"'{input}' should be a percentage (0 - 100%)", e);
            }
        }
        else
        {
            // Parse as decimal
            try
            {
                return byte.Parse(input);
            }
            catch (FormatException e)
            {
                throw new FormatException($"'{input}' should be a decimal number", e);
            }
        }
    }
}
