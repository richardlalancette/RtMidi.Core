using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImageMagick;
using RtMidi.Core.Devices;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;
using Serilog;
using ImageMagick;

namespace RtMidi.Core.Samples
{
    public class Program
    {
        private IMidiOutputDevice _outputDevice;

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Debug()
                .CreateLogger();

            using (MidiDeviceManager.Default)
            {
                var p = new Program();
            }
        }

        public Program()
        {
            // List all available MIDI API's
            foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                Console.WriteLine($"Available API: {api}");

            // Listen to all available midi devices
            void ControlChangeHandler(IMidiInputDevice sender, in ControlChangeMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] ControlChange: Channel:{msg.Channel} Control:{msg.Control} Value:{msg.Value}");
            }

            void ChannelPressureMessageHandler(IMidiInputDevice sender, in ChannelPressureMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] Channel Pressure Message: Channel:{msg.Channel} Pressure:{msg.Pressure}");
            }

            void NoteOnHandler(IMidiInputDevice sender, in NoteOnMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] Note On - Channel:{msg.Channel} Key:{msg.Key} Velocity:{msg.Velocity}");
            }

            void NoteOffHandler(IMidiInputDevice sender, in NoteOffMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] Note Off - Channel:{msg.Channel} Key:{msg.Key} Velocity:{msg.Velocity}");
            }

            void NrpnHandler(IMidiInputDevice sender, in NrpnMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] Nrpn - Channel:{msg.Channel} Parameter:{msg.Parameter} Value:{msg.Value}");
            }

            void ProgramChangeHandler(IMidiInputDevice sender, in ProgramChangeMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] Program Change - Channel:{msg.Channel} Program:{msg.Program}");
            }

            void SysExHandler(IMidiInputDevice sender, in SysExMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] Sys Ex Message - Data:{msg.Data}");
            }

            var devices = new List<IMidiInputDevice>();
            try
            {
                foreach (var inputDeviceInfo in MidiDeviceManager.Default.InputDevices)
                {
                    Console.WriteLine($"Opening {inputDeviceInfo.Name}");

                    var inputDevice = inputDeviceInfo.CreateDevice();
                    devices.Add(inputDevice);

                    inputDevice.ControlChange += ControlChangeHandler;
                    inputDevice.ChannelPressure += ChannelPressureMessageHandler;
                    inputDevice.Nrpn += NrpnHandler;
                    inputDevice.NoteOn += NoteOnHandler;
                    inputDevice.NoteOff += NoteOffHandler;
                    inputDevice.ProgramChange += ProgramChangeHandler;
                    inputDevice.SysEx += SysExHandler;
                    inputDevice.Open();
                }

                foreach (var outputDeviceInfo in MidiDeviceManager.Default.OutputDevices)
                {
                    _outputDevice = outputDeviceInfo.CreateDevice();
                    _outputDevice.Open();
                }

                Console.WriteLine("Press 'q' key to stop...");
                Random rand1 = new Random();

                // MagickImageCollection collection = new MagickImageCollection("images/eyeball.gif");
                MagickImageCollection collection = new MagickImageCollection("images/mario.gif");
                // MagickImageCollection collection = new MagickImageCollection("images/icon.gif");
                collection.Coalesce();

                int i = 0;
                
                while (true)
                {
                    byte ledIndex = 81;
                    
                    var pixelCollection = collection[i].GetPixels();
                    i++;
                    if (i >= collection.Count)
                    {
                        i = 0;
                    }

                    Thread.Sleep(50);
                    
                    foreach (var pixel in pixelCollection)
                    {
                        var color = pixelCollection[pixel.X, pixel.Y].ToColor();
                        byte red = (byte) (color.R / 4);
                        byte green = (byte) (color.G / 4);
                        byte blue = (byte) (color.B / 4);
                        byte[] data = new byte[] {240, 0, 32, 41, 2, 24, 11, (byte) (ledIndex), red, green, blue, 247};
                        ledIndex++;
                        _outputDevice.Send(new SysExMessage(data));

                        // Handle the last button of the row
                        if (pixel.X == 7)
                        {
                            data = new byte[] {240, 0, 32, 41, 2, 24, 11, (byte) (ledIndex), 0, 0, 0, 247};
                            _outputDevice.Send(new SysExMessage(data));

                            ledIndex -= 18;
                            // ledIndex ++;
                        }
                    }
                }
            }
            finally
            {
                foreach (var device in devices)
                {
                    device.ControlChange -= ControlChangeHandler;
                    device.ChannelPressure -= ChannelPressureMessageHandler;
                    device.Nrpn -= NrpnHandler;
                    device.NoteOn -= NoteOnHandler;
                    device.NoteOff -= NoteOffHandler;
                    device.ProgramChange -= ProgramChangeHandler;
                    device.SysEx -= SysExHandler;
                    device.Dispose();
                }
            }
        }
    }
}