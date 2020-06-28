# AquariumController
This project enables a raspberry pi zero to do basis aquarium controll.

It is build on .NET. It use .NET Core IoT Libraries as a basis, but raspberry pi zero do at this moment not support .NET Core IoT Libraries as-is(see https://www.flexlabs.org/2019/07/running-net-core-apps-on-raspberry-pi-zero). The tools needed from .NET Core IoT Libraries are extracted and rewritten in framework 4.8.

In version 0.1 it will support (it is NOT in version 0.1, it is under development)

1) A lcD1602 display showing tempertur and an fish animation. The fish animation makes it possible to see if program is still alive)

2) Turn on/off a heater via philips hue bridge and a smart plug.

3) Turn of/off an air pump (don't know yet if it is via a smart plug or GPIO). The on/off of the air pump is controller via a condition of one or more philips hue lights. 
(The air pump is noisy and my aquarium is in the living room, sow turn it off if I relaxing in the living room)
