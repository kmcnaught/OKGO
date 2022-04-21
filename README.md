# OK, Game On!

OK, Game On! (working title) or “OKGO” is a Windows application offering eye control access to many games via keyboard, mouse and gamepad emulation. It is based on the excellent [Optikey](https://github.com/OptiKey/OptiKey/) and [EyeMine](https://github.com/SpecialEffect/EyeMine/) projects.

# Setup / install

OKGO uses the .Net 4.6 Framework and is designed to run on Windows 8 / 8.1 / 10 / 11. 

Before running OKGO, you must separately install the latest version of ViGemBus from https://github.com/ViGEm/ViGEmBus/releases (run ViGEmBusSetup_x86.msi or ViGEmBusSetup_x64.msi depending if you have a 32-bit or 64-bit PC)

# Tips and tricks

Ideally you should not have any other USB controllers connected to your PC - OKGO will create a virtual one that you can see in the “Game Controllers” list in Windows, and many games don’t like more than one controller attached

Some games may be sensitive to launch order - e.g. they require OKGO to be running before the game is launched (or maybe vice versa). Feedback is very welcome if you take notes on this.

Sometimes it may be necessary to set yout game to use “fullscreen windowed” or “windowed” + maximised, other games work fine in fullscreen mode.

If the game isn’t responding to OKGO, make sure the game has focus by clicking on the game with a mouse.

# Included keyboards

OK Game On comes bundled with starter interfaces for a few different games as well as generic interfaces to try out the controls. If you’ve tried the controls and want to build a custom interface, get in touch: kirsty.mcnaught@gmail.com

# License

All source code is licensed under the GNU GENERAL PUBLIC LICENSE (Version 3, 29th June 2007)

Details of third party licenses is in [ThirdPartyLicenses.md](ThirdPartyLicenses.md)

