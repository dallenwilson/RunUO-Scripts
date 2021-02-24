# Warped Dragon's scripts for RunUO
A collection of scripts for the RunUO Ultima Online Server Emulator, versions 2.2 to 2.4. Beyond that, I did not test.

### UltimaMoongates/*
- UltimaMoongates_Animations.cs:
- - A collection of templates that make use of the various rising animations that the stock client never did. Moongates can rise from the ground, stay open their allotted time, and then sink gracefully back down.
- - <b>Every other file in this folder depends on this one.</b>
- - All four colours are available: Blue, Red, Black, Silver
- - Item ID's confirmed available with a Mondain's Legacy / v4.0.0 client.exe. Previous UO versions may only have the Blue available.

- UltimaMoongates_U4.cs: A replacement for PublicMoongate.cs, with an admin-level generation command. Replaces the moongates in the gate circles on Trammel and Felucca facets with gates that open, close, and change destination according to Ultima IV rules.

### Misc:
- ChatLogging.cs: Logs all speech by users to the console and to a log file. Requires two small changes in PlayerMobile.cs. A copy is available in Scripts/Mobiles/PlayerMobile.cs, but I advise against just dropping that in your server and replacing the existing one. Make the changes yourself. Use diff to find them.
