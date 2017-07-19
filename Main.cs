using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;

class Config
{
  public readonly string apiKey;
  public readonly string username;

  public Config(string username, string apiKey)
  {
    this.apiKey = apiKey;
    this.username = username;
  }

  public Uri GetUri(int limit, int page)
  {
    var builder = new UriBuilder("https://", "ws.audioscrobbler.com/2.0");
    var query = new []
    {
      "method=user.getrecenttracks",
      "user=" + this.username,
      "api_key=" + this.apiKey,
      "format=json",
      "extended=1",
      "limit=" + limit.ToString(),
      "page=" + page.ToString(),
    };
    builder.Query = String.Join("&", query);
    return builder.Uri;
  }
}

class Scrobble
{
  public readonly string artistName;
  public readonly string artistMbid;
  public readonly string albumName;
  public readonly string albumMbid;
  public readonly string trackName;
  public readonly string trackMbid;
  public readonly DateTime time;
  public readonly bool loved;

  public Scrobble(string arn, string arid, string aln, string alid, string tn, string tid, DateTime time, bool loved)
  {
    this.artistName = arn;
    this.artistMbid = arid;
    this.albumName = aln;
    this.albumMbid = alid;
    this.trackName = tn;
    this.trackMbid = tid;
    this.time = time;
    this.loved = loved;
  }
}

class Page
{
  public readonly int totalPages;
  public readonly Scrobble[] scrobbles;

  public Page(int totalPages, Scrobble[] scrobbles)
  {
    this.totalPages = totalPages;
    this.scrobbles = scrobbles;
  }
}

static class FmCount
{
  static string LocalDir
  {
    get
    {
      var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
      return Path.Combine(homeDir, ".local", "share", "fmcount");
    }
  }

  static Config ReadConfig()
  {
    var configFile = Path.Combine(LocalDir, "config");
    var config = File.ReadAllLines(configFile)
      .Select(line => line.Split(new [] { '=' }, 2))
      .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

    return new Config(config["username"], config["apikey"]);
  }

  static Page Probe(Config config)
  {
    var wc = new WebClient();
    return Parse(wc.DownloadString(config.GetUri(10, 1)));
  }

  static Page Parse(string pageJson)
  {
    var json = JsonValue.Parse(pageJson)["recenttracks"];
    var attr = json["@attr"];
    var tracks = json["track"];

    // It is json, yet Last.fm encodes integers as strings.
    var totalPages = Int32.Parse(attr["totalPages"]);
    var scrobbles = new List<Scrobble>();

    foreach (JsonValue track in tracks)
    {
      var arn = track["artist"]["name"];
      var arid = track["artist"]["mbid"];
      var aln = track["album"]["#text"];
      var alid = track["album"]["mbid"];
      var tn = track["name"];
      var tid = track["mbid"];
      var time = FromUnixTime(track["date"]["uts"]);
      var loved = track["loved"] == "1";
      scrobbles.Add(new Scrobble(arn, arid, aln, alid, tn, tid, time, loved));
    }

    return new Page(totalPages, scrobbles.ToArray());
  }

  static DateTime FromUnixTime(string secondsString)
  {
    var seconds = Double.Parse(secondsString);
    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    return epoch.AddSeconds(seconds);
  }

  static int Main(string[] argv)
  {
    Console.WriteLine(LocalDir);
    var config = ReadConfig();
    var firstPage = Probe(config);
    Console.WriteLine(firstPage.ToString());
    return 0;
  }
}
