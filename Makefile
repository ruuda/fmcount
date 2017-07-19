Main.exe:
	mcs Main.cs

run: Main.exe
	mono Main.exe
