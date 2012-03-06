//
// fdr-youtube-dl.exe 
// @author: Greg Gauthier <greg@techrobatics.com>
// @version: 0.1
// @date:   2012-03-06
//
using System;
using System.IO;
using System.Collections.Generic;
using Google.YouTube;
using NDesk.Options;
using YouTubeDownloadExtension;

namespace fdr_youtube_dl
{
    class Downloader
    {
        private const string ApplicationName = "fdr-youtube-dl";
        private const string DeveloperKey = "AI39si7jpK-6CGQwlTFjLu8SBtfm5PLcN8imDHe8xWyD1Cif2aCpOPiowZTN4yTfzI3zH-D61MlnB5B_oMQDwyVWXcdGCRVQNg";
        private static string _userid = null;
        private static string _password = null;
        private static string _destination = "";

        static void DisplayShortHelp()
        {
            Console.WriteLine("error: no userid and/or password supplied.");
            Console.WriteLine("usage: fdr-youtube-dl -u <userid> -p <password> [-d <destination>]");
            Console.WriteLine("for more details: 'fdr-youtube-dl -h'");
        }

        static void DisplayLongHelp()
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
            Console.WriteLine("");
            Console.WriteLine("   -h [--help | ? ]                This help screen");
            Environment.Exit(Environment.ExitCode);
        }


        static void Main(string[] args)
        {
            var options = new OptionSet()
                .Add("u=|userid=", u => _userid = u)
                .Add("p=|password=", p => _password = p)
                .Add("d=|destination=", d => _destination = d)
                .Add("?|h|help", h => DisplayLongHelp());

            options.Parse(args);
            if (_userid == null || _password == null)
            {
                DisplayShortHelp();
                Environment.Exit(Environment.ExitCode);
            }

            //main app
            var settings = new YouTubeRequestSettings(ApplicationName, DeveloperKey, _userid, _password);
            var request = new YouTubeRequest(settings);

            var uri = new Uri("http://gdata.youtube.com/feeds/api/users/default/uploads");
            var feed = request.Get<Video>(uri);
            feed.AutoPaging = true;

            foreach (var video in feed.Entries)
            {
                Console.WriteLine(String.Format("Storing Video: [{0}] in Location: [{1}]", video.Title, _destination));
                request.Download(video, VideoQuality.Original, VideoFormat.MP4, _destination);
            }
        }
    }
}