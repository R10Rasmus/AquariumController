﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using UnitsNet;

namespace Iot.Device.OneWire
{
    public partial class OneWireThermometerDevice : OneWireDevice
    {

        private Temperature ReadTemperatureInternal()
        {
            var data = File.ReadAllText(Path.Combine(OneWireBus.SysfsDevicesPath, BusId, DeviceId, "w1_slave"));
            return ParseTemperature(data);
        }

        private static Temperature ParseTemperature(string data)
        {
            // Expected data format:
            // 7f 01 4b 46 7f ff 01 10 33 : crc=33 YES
            // 7f 01 4b 46 7f ff 01 10 33 t=23937
            if (!data.Contains("YES"))
            {
                throw new InvalidOperationException("Unable to read temperature from device.");
            }

            var tempIdx = data.LastIndexOf("t=");
            if (tempIdx == -1 || tempIdx + 2 >= data.Length || !int.TryParse(data.AsSpan(tempIdx + 2).ToString(), out var temp))
            {
                throw new InvalidOperationException("Invalid sensor data format.");
            }

            return Temperature.FromDegreesCelsius(temp * 0.001);
        }
    }
}
