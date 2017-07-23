open System

type Config = {
  ApiKey: string;
  Username: string;
}

type Scrobble = {
  ArtistName: string;
  ArtistMbid: string;
  AlbumName: string;
  AlbumMbid: string;
  TrackName: string;
  TrackMbid: string;
  Time: DateTime;
  Loved: bool;
}

[<EntryPoint>]
let main argv =
  printfn "Hi"
  0
