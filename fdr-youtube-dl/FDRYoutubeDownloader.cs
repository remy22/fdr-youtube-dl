//
// fdr-youtube-dl.exe 
// @author: Greg Gauthier <greg@techrobatics.com>
// @version: 0.1
// @date:   2012-03-06
//
using System;
using System.IO;
using System.Collections.Generic;
using Google.GData.Client;
using Google.YouTube;
using NDesk.Options;
using YouTubeDownloadExtension;
using System.Configuration;


namespace fdr_youtube_dl
{
    public class Downloader
    {
        public const string ApplicationName = "fdr-youtube-dl";
        public static string DevKey = ConfigurationManager.AppSettings["DeveloperKey"];

        public const string SingleUri = "http://gdata.youtube.com/feeds/api/videos/";
        public const string MultiUri = "http://gdata.youtube.com/feeds/api/users/default/uploads";

        public static YouTubeRequest Requestor = null;

        public static string Userid = null;
        public static string Password = null;
        public static string Destination = null;
        public static string VideoId = null;

        public static void SetOptions(IEnumerable<string> args)
        {
            var options = new OptionSet()
                .Add("u=|userid=", u => Userid = u)
                .Add("p=|password=", p => Password = p)
                .Add("d=|destination=", d => Destination = d)
                .Add("v=|videoid=", v => VideoId = v)
                .Add("?|h|help", h => Helper.DisplayLongHelp());

            options.Parse(args);
            if (Userid == null | Password == null)
            {
                Helper.DisplayShortHelp();
                Environment.Exit(Environment.ExitCode);
            }            
        }

        public static void CreateRequestor()
        {
            Requestor = new YouTubeRequest(new YouTubeRequestSettings(ApplicationName, DevKey, Userid, Password));
        }

        public static Feed<Video> GetVideoList(bool autopage=true)
        {
            var feed = Requestor.Get<Video>(new Uri(MultiUri));
            feed.AutoPaging = autopage;
            return feed;
        }

        public static Video GetVideo(string videoId=null)
        {
            if (videoId == null)
            {
                Logger.Message("Cannot create individual request object without a video id.",true);
                Environment.Exit(1);
            }
            try
            {
                return Requestor.Retrieve<Video>(new Uri(SingleUri + videoId));                                          
            }
            catch (Exception ex)
            {
                Logger.Message(String.Format("Unable to acquire video. {0}",ex.Message),true);
                return null;
            }
        }
    
        public static void DownloadVideo(string videoId)
        {
            if (Destination == null)
            {
                Destination = ".";
            }
            try
            {
                var vid = GetVideo(videoId);
                var vidId = vid.Id.Split(':')[3];
                Logger.Message(String.Format("Retrieving: [{0}]:[{1}] to location: [{2}]", vidId, vid.Title, Destination));
                try
                {
                    Requestor.Download(vid, VideoQuality.Original, VideoFormat.MP4, Destination);
                }
                catch (Exception ex)
                {
                    Logger.Message(String.Format("Retrieval Failed: [{0}]:[{1}] - {2}", vidId, vid.Title, ex.Message), true);
                }
            }
            catch (Exception ex)
            {
                Logger.Message(String.Format("Unable to acquire video. {0}", ex.Message), true);                
            }
        }

        public static void DownloadFeed(Feed<Video> feed)
        {
            if (Destination == null)
            {
                Destination = ".";
            }
            try
            {
                foreach (var video in feed.Entries)
                {
                    if (video.Private)
                    {
                        //I think the YouTubeExtensions is parsing the xml incorrectly, when certain fields are set 
                        //because this is what I get back for a video id, when the private flag is set:
                        //
                        //http://gdata.youtube.com/feeds/api/videos/tag:youtube.com,2008:video:VWDqjeFe5GY
                        var videoId1 = video.Id.Split(':')[3];
                        Logger.Message(String.Format("Retrieval failed. [{0}]:[{1}] - Private video.", videoId1, video.Title),true);

                        DownloadVideo(videoId1);
                    }
                    else
                    {
                        Logger.Message(String.Format("Retrieving: [{0}] to location: [{1}]", video.Title, Destination));
                        try
                        {
                            Requestor.Download(video, VideoQuality.Original, VideoFormat.MP4, Destination);
                        }
                        catch (Exception ex)
                        {
                            var videoId = video.Id.Split(':')[3];

                            Logger.Message(String.Format("Retrieval failed. [{0}]:[{1}] - {2}", videoId, video.Title, ex.Message),true);
                            Logger.Message(String.Format("Alternate retrieval attempt for id: [{0}]:[{1}]", videoId,video.Title));
                            try
                            {
                                DownloadVideo(videoId);
                            }
                            catch (Exception ex2)
                            {
                                Logger.Message(
                                    String.Format("Alternate retrieval failed. Id: [{0}]:[{1}] - {2}", videoId, video.Title, ex2.Message),true);
                            }
                        }
                    }
                }
            }
            catch (InvalidCredentialsException)
            {
                Logger.Message("Invalid userid or password. Cannot retrieve data.",true);
                Environment.Exit(Environment.ExitCode);
            }
        }
       
        static void Main(string[] args)
        {
            SetOptions(args);
            CreateRequestor();
            if (VideoId == null)
            {
                var list = GetVideoList();
                DownloadFeed(list);                
            }
            else
            {
                DownloadVideo(VideoId);
            }
        }

    }

    public static class Logger
    {
        private static readonly string LogFilename = string.Format(@"{0}\download.log", Environment.CurrentDirectory);

        public static void Message(string msg,bool err=false)
        {
            msg = string.Format("{0:G}: {1}{2}", DateTime.Now, msg, Environment.NewLine);
            if (!err)
            {
                Console.Write(msg);                
            }
            else
            {
                Console.Error.Write(msg);
            }
            File.AppendAllText(LogFilename, msg);
        }
    }

    public static class Helper
    {
        public static void DisplayShortHelp()
        {
            Console.WriteLine("error: no userid and/or password supplied.");
            Console.WriteLine("usage: fdr-youtube-dl -u <userid> -p <password> [-d <destination>] [-v <videoid>]");
            Console.WriteLine("for more details: 'fdr-youtube-dl -h'");
        }

        public static void DisplayLongHelp()
        {
            Console.WriteLine("fdr-youtube-dl version 0.1");
            Console.WriteLine("");
            Console.WriteLine("Retrieve and store all youtube videos for the specified account.");
            //Console.WriteLine("Optionally convert all WEBM to MP4 (not yet implemented)");
            Console.WriteLine("");
            Console.WriteLine("usage: fdr-youtube-dl [options]");
            Console.WriteLine("");
            Console.WriteLine("REQUIRED Options:");
            Console.WriteLine("   -u [--userid] <userid>          The YouTube user's userid");
            Console.WriteLine("   -p [--password] <password>      The YouTube user's password");
            Console.WriteLine("");
            Console.WriteLine("Additional Options:");
            Console.WriteLine("   -d [--destination] <path>       The path/directory into which to store the videos");
            Console.WriteLine("   -v [--videoid] <videoid>        A single YouTube video id to be downloaded");
            Console.WriteLine("");
            Console.WriteLine("   -h [--help | ? ]                This help screen");
            Console.WriteLine("");
            Console.WriteLine("If you include the -v option, the downloader will only attempt to download the");
            Console.WriteLine("video you specify in the option.");
            Environment.Exit(Environment.ExitCode);
        }
    }
}