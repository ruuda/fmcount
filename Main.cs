using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;

static class UnixTime
{
  private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

  public static DateTime Parse(string secondsString)
  {
    var seconds = Double.Parse(secondsString);
    return epoch.AddSeconds(seconds);
  }

  public static string Format(DateTime dt)
  {
    var seconds = (dt - epoch).TotalSeconds;
    return ((Int64)seconds).ToString();
  }
}

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
    return GetUri(limit, page, null, null);
  }

  public Uri GetUri(int limit, int page, DateTime? begin, DateTime? end)
  {
    var builder = new UriBuilder("https://", "ws.audioscrobbler.com/2.0");
    var query = new List<String>()
    {
      "method=user.getrecenttracks",
      "user=" + this.username,
      "api_key=" + this.apiKey,
      "format=json",
      "extended=1",
      "limit=" + limit.ToString(),
      "page=" + page.ToString(),
    };
    if (begin != null) query.Add("from=" + UnixTime.Format(begin.Value));
    if (end != null) query.Add("to=" + UnixTime.Format(end.Value));
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

static class Fmcount
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
      var time = UnixTime.Parse(track["date"]["uts"]);
      var loved = track["loved"] == "1";
      scrobbles.Add(new Scrobble(arn, arid, aln, alid, tn, tid, time, loved));
    }

    return new Page(totalPages, scrobbles.ToArray());
  }

  public static IEnumerable<DateTime> EnumerateMonths(DateTime start, DateTime end)
  {
    var current = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    while (current < end)
    {
      yield return current;

      // Note: AddMonths does not simply add 3600 * 24 * 30 seconds, it bumps
      // the month counter.
      current = current.AddMonths(1);
    }
  }

  static int Main(string[] argv)
  {
    Console.WriteLine(LocalDir);
    var config = ReadConfig();

    var now = DateTime.UtcNow;

    var wc = new WebClient();
    var firstPage = Parse(wc.DownloadString(config.GetUri(10, 1)));
    var lastPage = Parse(wc.DownloadString(config.GetUri(10, firstPage.totalPages)));

    var firstScrobbleTime = lastPage.scrobbles.Last().time;
    Console.WriteLine("First scrobble time: {0:s}", firstScrobbleTime);

    foreach (var month in EnumerateMonths(firstScrobbleTime, now))
    {
      Console.WriteLine("Would obtain month: {0:s}", month);
    }

    return 0;
  }
}
