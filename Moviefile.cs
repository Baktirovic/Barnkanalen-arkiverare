using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Barnkanalen_arkiverare
{
    class Moviefile
    {
        //Download progress changed
        public event EventHandler<ProgressEventArgs> DownloadProgressChanged;
        //Download errors
   //     public event EventHandler<downloadError> movieDownloadError;
        //Movie Catalouge 
        public movieCatalouge Catalouge { get; set; }
        //Current Episode Downloaded
        public movieEpisode Episode { get; set; }
        //Movie download progressbar
        public ProgressBar progressBar { get; set; }
        //Download Webclient
        public WebClient download_client { get; set; }

        public string client_GetWebFileContent(string url)
        {
            String client_ReturnString = "";
            try
            {
                WebClient client_WebClient = new WebClient();
                Stream client_Stream = client_WebClient.OpenRead(url);
                StreamReader client_StreamReader = new StreamReader(client_Stream);
                client_ReturnString = client_StreamReader.ReadToEnd();
            }catch(Exception es)
            {
               /* var movieDowonloadErrorArge = new downloadError("Url kan inte nås! : "+ url +'\n'+es.Message);
                this.movieDownloadError(this, movieDowonloadErrorArge); 
            
                */
            }
            return client_ReturnString;
        
        }
        public Moviefile()
        {
            
        }
        public Moviefile(string link, string image, string titlecatoluge, string catalougePicture)
        {
            this.Episode = new movieEpisode();
            this.Catalouge = new movieCatalouge();
            this.Episode.episodeImage = image;
            this.Catalouge.CatalougeName = titlecatoluge;
            this.Catalouge.CatalougeImage = catalougePicture;
            string flashvars = ""; 

            WebBrowser episodePageHTMLBrowser = new WebBrowser();
            episodePageHTMLBrowser.ScriptErrorsSuppressed = true; 
            string barnKanalenEpisodeHTML = client_GetWebFileContent(link);

            episodePageHTMLBrowser.DocumentText = barnKanalenEpisodeHTML;
            string objectElementFromHTMLPage = "";
            if (barnKanalenEpisodeHTML.Length > 100)
            {
                objectElementFromHTMLPage = barnKanalenEpisodeHTML.Substring(barnKanalenEpisodeHTML.IndexOf("<object"), (barnKanalenEpisodeHTML.IndexOf("</object>") - barnKanalenEpisodeHTML.IndexOf("<object")));
            }
            if(objectElementFromHTMLPage.Length > 50)
            { 
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(objectElementFromHTMLPage);
                doc.OptionFixNestedTags = true;

                HtmlNode col = doc.DocumentNode.ChildNodes[0];
                foreach (HtmlNode swfParameter in col.ChildNodes)
                {
                    try
                    { 
                        if (swfParameter.GetAttributeValue("name", "") == "flashvars")
                        {
                            flashvars = swfParameter.GetAttributeValue("value", "");
                            flashvars = flashvars.Replace("json=", "");
                        }

                    }
                    catch (Exception es)
                    {

                    }
                }
                if (flashvars.Length > 0)
                {

                    JObject flashVarsSwfParameterString = JObject.Parse(flashvars);
                    JObject videoReferenceFromSwfParamter = JObject.Parse(flashVarsSwfParameterString["video"]["videoReferences"].ToArray().GetValue(1).ToString());
                    JToken linkToVideoFileFromJson = videoReferenceFromSwfParamter["url"];
                    String episodeNameFromJson = ((JToken)flashVarsSwfParameterString["context"]["title"]).ToString().Replace(":", " ").Trim();
                    String episodeQuailtyListTextFile = client_GetWebFileContent(linkToVideoFileFromJson.ToString());

                    string[] episodeFilesByQuality = episodeQuailtyListTextFile.Split('\n');
                    string HQLinkToEpisode = "";

                    foreach (string line in episodeFilesByQuality)
                    {
                        if (line.StartsWith("http"))
                            HQLinkToEpisode = line;
                    }

                    String videoChunksTextFile = client_GetWebFileContent(HQLinkToEpisode);

                    string[] videoChunkLinks = videoChunksTextFile.Split('\n');

                    this.Episode.filenames = new List<string>();
                    this.Episode.urls = new List<string>();
                    this.Episode.EpisodName = this.Catalouge.CatalougeName + " - " + episodeNameFromJson;
                    this.Episode.fullPathFileNameDownloaded = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + @"\" + this.Episode.EpisodName + ".mp4";
                   
                    int i = 0;
                    foreach (string videoChunkLink in videoChunkLinks)
                    {
                        if (videoChunkLink.StartsWith("http"))
                        {
                            this.Episode.urls.Add(videoChunkLink);
                            this.Episode.filenames.Add(this.Episode.fullPathFileNameDownloaded + "_tmp_" + i.ToString());
                            i++;
                        }
                    }
                    this.Episode.episodeFileChunkCount = i;
                    this.Episode.numberOfFileChunksDownloaded = 0;
                    this.Episode.currentChunk = 0;
                }
                else
                {

                   /* var movieDowonloadErrorArge = new downloadError("Kan inte hitta filmsenvenser!");
                    this.movieDownloadError(this, movieDowonloadErrorArge); 
                
                    */
                }
            }
        }

        public void download_movie()
        {
            if (this.Episode.fullPathFileNameDownloaded != null)
            {
                download_client = new WebClient();
                download_client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                download_client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                download_client.DownloadFileAsync(new Uri(this.Episode.urls[this.Episode.currentChunk]), this.Episode.fullPathFileNameDownloaded + "_tmp_" + this.Episode.currentChunk.ToString());
            }
        }
        public void getNextVideoChunk()
        {
            this.Episode.currentChunk = this.Episode.currentChunk + 1;
            if (this.Episode.currentChunk < this.Episode.episodeFileChunkCount)
            {
                download_movie();
            }else
            {
               
                var eventArgs = new ProgressEventArgs(this.progressBar.Value); 
                this.DownloadProgressChanged(this, eventArgs);
                this.progressBar.Value = 100;
                this.progressBar.Visible = false;
            } 
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var eventArgs = new ProgressEventArgs(this.progressBar.Value); 
            this.DownloadProgressChanged(this, eventArgs); 
          
        }
        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {

                FileMode f_mode = FileMode.Create;

                if (File.Exists(this.Episode.fullPathFileNameDownloaded))
                    f_mode = FileMode.Append;

                try
                {
                    byte[] bytees = File.ReadAllBytes(this.Episode.filenames[this.Episode.currentChunk]);
                    FileStream stream = new FileStream(this.Episode.fullPathFileNameDownloaded, f_mode, FileAccess.Write);
                    if (File.Exists(this.Episode.fullPathFileNameDownloaded))
                        stream.Seek(0, SeekOrigin.End);

                    stream.Write(bytees, 0, bytees.Length);
                    stream.Close();
                }catch(Exception es)
                {
                   /* var movieDowonloadErrorArge = new downloadError("Kan inte skriva över filens" + this.Episode.fullPathFileNameDownloaded);
                    this.movieDownloadError(this, movieDowonloadErrorArge); 
                
                    */
                }
                File.Delete(this.Episode.filenames[this.Episode.currentChunk]);
                this.Episode.numberOfFileChunksDownloaded++;
                double percentage = (this.Episode.numberOfFileChunksDownloaded / this.Episode.episodeFileChunkCount) * 100.0;
                this.progressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
                getNextVideoChunk();
            
        }
        	  

    }
}
