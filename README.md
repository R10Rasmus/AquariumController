# AquariumController
This project enables a raspberry pi zero to do basis aquarium controll.

It is composed of a backend (AquariumController) and a fronend (InfoPages). Both use framework 4.8 and Mono (https://www.mono-project.com/).

It run on the latest Mono version, see https://www.mono-project.com/download/stable/#download-lin-raspbian

# AquariumController (Backend)
It is build on .NET. It use .NET Core IoT Libraries as a basis, but raspberry pi zero do at this moment not support .NET Core IoT Libraries as-is(see https://www.flexlabs.org/2019/07/running-net-core-apps-on-raspberry-pi-zero). The tools needed from .NET Core IoT Libraries are extracted and rewritten in framework 4.8.

# InfoPages (Fronend)

It is an ASP.NET MVC project. It run on the Apach server via Mono, see this guide https://medium.com/@shrimpy/configure-apache2-mod-mono-to-run-asp-net-mvc5-application-on-ubuntu-14-04-314a700522b9.

If it do not work (it is using mono-2 insted of mono-4) then create a symbolic link to the new installation of mono. 

# Version 0.1
Version 0.1 support:

1) A lcD1602 display showing tempertur, pH and an fish animation. The fish animation makes it possible to see if program is still alive

2) Turn on/off a heater via philips hue bridge and a smart plug.

3) Turn of/off an air pump (a usb air pump controlled via GPIO ). The on/off of the air pump is controller via a timer. 
(The air pump is noisy and my aquarium is in the living room, sow turn it off if I relaxing in the living room)

4) Use a MySQL db to handle settings

5) Log data (fx. temperature an pH to db), set save interval via db.

6) Read temperature via ds18b20 temperature sensor

7) Read pH value via uFire ISE (https://www.ufire.co/ph/) (it use I2C)

8) Make a web app. show data
8.1) show an settings panel
8.2) Show a page per channel (tempertur and pH)
8.3) Show a dashboard (with an graph for each data channel)
8.4) zoom on an graf


