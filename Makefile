Main.exe: Main.fs
	fsharpc --reference:System.Json Main.fs

# Main.exe: Main.cs
#	mcs -reference:System.Json Main.cs

run: Main.exe
	mono Main.exe
