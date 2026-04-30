using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SimpleOps.GsxRamp
{
    internal sealed class AudioInputDeviceInfo
    {
        public int Index;
        public string Name;

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class AudioOutputDeviceInfo
    {
        public string Id;
        public string Name;

        public override string ToString()
        {
            return Name;
        }
    }

    internal static class AudioDeviceCatalog
    {
        public static IList<AudioInputDeviceInfo> GetInputDevices()
        {
            var devices = new List<AudioInputDeviceInfo>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var capabilities = WaveInEvent.GetCapabilities(i);
                devices.Add(new AudioInputDeviceInfo { Index = i, Name = capabilities.ProductName });
            }

            return devices;
        }

        public static IList<AudioOutputDeviceInfo> GetOutputDevices()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                return enumerator
                    .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .Select(device => new AudioOutputDeviceInfo { Id = device.ID, Name = device.FriendlyName })
                    .ToList();
            }
        }

        public static AudioInputDeviceInfo FindInputByName(string deviceName)
        {
            return GetInputDevices().FirstOrDefault(device => string.Equals(device.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        }

        public static AudioOutputDeviceInfo FindOutputById(string deviceId)
        {
            return GetOutputDevices().FirstOrDefault(device => string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
