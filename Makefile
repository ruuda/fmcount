Main.exe: Main.cs
	mcs -reference:System.Json Main.cs

run: Main.exe
	mono Main.exe
