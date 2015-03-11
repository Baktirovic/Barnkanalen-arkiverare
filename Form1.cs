using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using System.Xml;
using System.Diagnostics;
using System.Drawing.Drawing2D;


namespace Barnkanalen_arkiverare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();   
        }

        private Moviedatabase movieDatabase = new Moviedatabase();
        private const string SvtProgramList = "http://www.svt.se/barnkanalen/programao/";
        private const string svtElementClassList = "svtLineElement-Item";

       
        private string encodeToUtf8(string inputHTML)
        { 
            byte[] bytes = Encoding.Default.GetBytes(inputHTML);
            return Encoding.UTF8.GetString(bytes);
        }
        private void getTVGuideFromSvt(object sender, EventArgs e)
        {
            getSVTTVGuide();
        }
        private void getSVTTVGuide()
        {

            Moviefile svtList = new Moviefile();
            WebBrowser TVGuideFromSvt = new WebBrowser();
            TVGuideFromSvt.ScriptErrorsSuppressed = true;
            TVGuideFromSvt.DocumentText = svtList.client_GetWebFileContent(SvtProgramList);
            TVGuideFromSvt.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(TVGuideFromSvt_DocumentCompleted);
  
        }
        private void TVGuideFromSvt_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser programAOPage = (WebBrowser)sender; 

            if (programAOPage.Document != null)
            {
                HtmlElementCollection elems = programAOPage.Document.GetElementsByTagName("div");
                svtProgressBar.Minimum = 0;
                svtProgressBar.Maximum = elems.Count;
                svtProgressBar.Value = 0;

                foreach (HtmlElement elem in elems)
                {
                    svtProgressBar.Value++;
                    svtProgressLabel.Text = "Laddar ned element " + svtProgressBar.Value + " av " + svtProgressBar.Maximum;
                    String nameStr = elem.GetAttribute("className");
                    if (nameStr != null && nameStr.Length != 0)
                    { 
                        String contentStr = elem.GetAttribute("className");
                        if (contentStr == svtElementClassList)
                        {
                            HtmlElementCollection channelName = elem.GetElementsByTagName("h4");
                            HtmlElementCollection channelPage = elem.GetElementsByTagName("a");
                            HtmlElementCollection channelImage = elem.GetElementsByTagName("img");


                            FlowLayoutPanel catalougePanel = new FlowLayoutPanel();
                            catalougePanel.Width = 130;
                            catalougePanel.Height = 140;
                            catalougePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                             
                            Button catalougeButton = new Button();
                            catalougeButton.Click += new EventHandler(channelButtonClick);
                            catalougeButton.Name = channelName[0].InnerText; 

                            catalougeButton.Tag = channelPage[0].GetAttribute("href");
                            catalougeButton.Image = GetImageFromUrl(channelImage[0].GetAttribute("src"));
                            catalougeButton.Height = 100;
                            catalougeButton.Width = 120;

                            Label catalougeNameLabel = new Label();
                            catalougeNameLabel.Text = channelName[0].InnerText;
                            catalougeNameLabel.Width = 120;
                            catalougeNameLabel.Height = 40; 
                            catalougePanel.Controls.Add(catalougeButton);
                            catalougePanel.Controls.Add(catalougeNameLabel);
                            flowLayoutPanel1.Controls.Add(catalougePanel); 
                        }
                        svtProgressLabel.Text = "Klar, klicka på bilden för att se programlistan";
                    }
                }
            }
            else {
                 svtProgressLabel.Text = "Problem med uppkopplingen till SVT";
            } 

        }
        
        private void channelButtonClick(object sender, EventArgs e)
        {
            Button channelButton = (Button)sender; 
            Label catalougeNameLabel = (Label)((FlowLayoutPanel)(channelButton.Parent)).Controls[1];
            get_ChannelEpisodeList(channelButton.Tag.ToString(), channelButton.Image, catalougeNameLabel.Text);
        }
        

        private void download_Click(object sender, EventArgs e)
        {
            Button episodeDownloadButton = (Button)sender;
            string episodeDownloadLink = @"http://www.svt.se/" + episodeDownloadButton.Tag.ToString();
            Panel episodePanel = (Panel)episodeDownloadButton.Parent;
            ProgressBar downloadProgressBar = (ProgressBar)episodePanel.Controls[2];
            episodeDownloadButton.Visible = false;
            downloadProgressBar.Style = ProgressBarStyle.Continuous;
            downloadProgressBar.Visible = true;
            downloadProgressBar.Minimum = 0;
            downloadProgressBar.Maximum = 100;
            PictureBox pbbox = (PictureBox)episodePanel.Controls[0];
            PictureBox catalougeImage = (PictureBox)flowLayoutPanel2.Controls[0];

            Moviefile cartoon = new Moviefile(episodeDownloadLink, ImageToBase64(pbbox.Image, ImageFormat.Jpeg), ((Label)flowLayoutPanel2.Controls[1]).Text, ImageToBase64(catalougeImage.Image, ImageFormat.Jpeg));
            cartoon.progressBar = new ProgressBar();
            cartoon.progressBar = downloadProgressBar; 

            if (File.Exists(cartoon.Episode.fullPathFileNameDownloaded))
                File.Delete(cartoon.Episode.fullPathFileNameDownloaded);
         
            download_movie(cartoon);  
        }

        private void cartoon_Error(object sender, downloadError e)
        {
            textBox1.Text = ((downloadError)e).message;
        }
        private void cartoon_DownloadProgressChanged(object sender, EventArgs e)
        {
            Moviefile movie = ((Moviefile)sender);
            textBox1.Text = "Totalt nedladdat: " + movie.Episode.numberOfFileChunksDownloaded.ToString() + " av " + movie.Episode.episodeFileChunkCount.ToString();

            if (movie.Episode.numberOfFileChunksDownloaded == movie.Episode.episodeFileChunkCount - 1)
            {
                movieDatabase.set_NewMovieEpisode(movie.Catalouge.CatalougeName, movie.Catalouge.CatalougeImage, Path.GetFileNameWithoutExtension(movie.Episode.fullPathFileNameDownloaded), movie.Episode.fullPathFileNameDownloaded, movie.Episode.episodeImage);
                try
                {
                    FlowLayoutPanel thisEpisodePanel = (FlowLayoutPanel)(movie.progressBar.Parent);
                    thisEpisodePanel.BackColor = Color.LightGreen;
                }
                catch (Exception es) { }

            }
        }

        private void download_movie(Moviefile cartoon)     
        {
            if (cartoon.Episode.fullPathFileNameDownloaded == null)
            {
                svtProgressLabel.Text = "Problem med nedladdningen!";
                Button downloadButton = (Button)cartoon.progressBar.Parent.Controls[2];
                downloadButton.Visible = true;
                cartoon.progressBar.Visible = false;
            }
            else
            {
                cartoon.DownloadProgressChanged += cartoon_DownloadProgressChanged;
               // cartoon.movieDownloadError += cartoon_Error; 
                cartoon.download_movie();
            }
        }
        private void get_ChannelEpisodeList(string location, Image catalougeImage, string catalougeName)
        {
            flowLayoutPanel2.Controls.Clear(); 


            PictureBox catalougePic = new PictureBox();
            catalougePic.Image = catalougeImage;
            catalougePic.SizeMode = PictureBoxSizeMode.Zoom;
            catalougePic.Height = 140;
            catalougePic.Width = 200;
            catalougePic.BackColor = Color.Black;
            flowLayoutPanel2.Controls.Add(catalougePic);

            Label catalougeNameLabel = new Label();
            catalougeNameLabel.Text = catalougeName;
            catalougeNameLabel.Height = 20; catalougeNameLabel.Width = 220;
            flowLayoutPanel2.Controls.Add(catalougeNameLabel);
           
            Moviefile   episodesLister = new Moviefile();
            WebBrowser svtListEpisodesFromChannel = new WebBrowser();
            svtListEpisodesFromChannel.Tag = catalougeName;
            svtListEpisodesFromChannel.ScriptErrorsSuppressed = true;
            svtListEpisodesFromChannel.DocumentText = episodesLister.client_GetWebFileContent(location);
            svtListEpisodesFromChannel.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(svtListEpisodesFromChannel_DocumentCompleted);
     
        }
        private void svtListEpisodesFromChannel_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

            WebBrowser svtListEpisodesFromChannel = (WebBrowser)sender;
            string catalougeName = ((Label)flowLayoutPanel2.Controls[1]).Text;
            if (svtListEpisodesFromChannel.DocumentText != null)
            {   
                HtmlElementCollection noscript = svtListEpisodesFromChannel.Document.GetElementsByTagName("noscript");
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                if (noscript.Count > 0)
                {
                    doc.LoadHtml(System.Net.WebUtility.HtmlDecode(noscript[0].InnerHtml));
                
                    foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//div[@class='bpEpisodeListNoScript-ListItem']"))
                    {
                        HtmlAgilityPack.HtmlDocument episodeListDivElement = new HtmlAgilityPack.HtmlDocument();
                        episodeListDivElement.LoadHtml(link.InnerHtml);

                        FlowLayoutPanel episodePanel = new FlowLayoutPanel();
                        episodePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                        episodePanel.Left = 20;
                        episodePanel.Width = 200;
                        episodePanel.Height = 160;

                        HtmlNode HTMLimage = link.SelectSingleNode(".//img");
                        string alt = "";
                        if (HTMLimage != null)
                        { 
                            alt = WebUtility.HtmlDecode(HTMLimage.GetAttributeValue("alt", ""));
                            PictureBox pic = new PictureBox();
                            pic.Image = GetImageFromUrl(HTMLimage.GetAttributeValue("src", ""));
                            pic.Height = 80;
                            pic.Width = 190; 
                            episodePanel.Controls.Add(pic); 
                        }

                        Label episodeNameLabel = new Label();
                        episodeNameLabel.Text = alt;
                        episodeNameLabel.Height = 20;
                        episodeNameLabel.Width = 200;
                        episodePanel.Controls.Add(episodeNameLabel);

                        HtmlNode HTMLlink = link.SelectSingleNode(".//a");

                        Button downloadEpisodeButton = new Button();
                        downloadEpisodeButton.Text = "Ladda ner";
                        downloadEpisodeButton.Click += new EventHandler(download_Click);
                        downloadEpisodeButton.Tag = HTMLlink.GetAttributeValue("href", "");
                        downloadEpisodeButton.Height = 20;
                        downloadEpisodeButton.Width = 180;

                        ProgressBar downloadEpisodeProgressBar = new ProgressBar();
                        downloadEpisodeProgressBar.Visible = false;

                        episodePanel.Controls.Add(downloadEpisodeProgressBar);
                        episodePanel.Controls.Add(downloadEpisodeButton); 
                    
                        try
                        {
                            Moviedatabase mb = new Moviedatabase();
                            if (mb.get_episode(svtListEpisodesFromChannel.Tag.ToString(),catalougeName + " - "+ alt) != null)
                            {
                                episodePanel.BackColor = Color.LightGreen;
                                Label lm = new Label();
                                lm.Text = "Finns i arkivet";
                                episodePanel.Controls.Add(lm); 
                            }
                        }
                        catch (Exception es)
                        { }
                        flowLayoutPanel2.Controls.Add(episodePanel);
                    }
                }
              }
              else {
                  MessageBox.Show("Inga episoder");
              }
                 
        }
        public static Image GetImageFromUrl(string url)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);

            using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                using (Stream stream = httpWebReponse.GetResponseStream())
                {
                    return Image.FromStream(stream);
                }
            }
        }
        public string ImageToBase64(Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }
        public Image Base64ToImage(string base64String)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0,
              imageBytes.Length);

            // Convert byte[] to Image
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        } 
       
        private void open_movie_Click(object sender, EventArgs e)
        {
            Button episode = (Button)sender;
            Process.Start(episode.Tag.ToString());
        }
        private void loadEpisodeListFromLibrary(string catalougeName)
        {
            flowLayoutPanel4.Controls.Clear();
            Moviedatabase mb = new Moviedatabase();

            List<movieEpisode> me = new List<movieEpisode>();
            me = mb.get_episodeList(catalougeName);
            Label catalougeNameLabel = new Label();
            catalougeNameLabel.Text = catalougeName;
            catalougeNameLabel.Width = 360;
            catalougeNameLabel.Height = 30;
            catalougeNameLabel.TextAlign = ContentAlignment.MiddleCenter;
            flowLayoutPanel4.Controls.Add(catalougeNameLabel);

            foreach (movieEpisode mc in me)
            {
                try
                {
                    FlowLayoutPanel episodeLibraryPanel = new FlowLayoutPanel();
                    episodeLibraryPanel.Width = 120;
                    episodeLibraryPanel.Height = 170;

                    Button imageButton = new Button();
                    imageButton.Image = Base64ToImage(mc.episodeImage);
                    imageButton.Click += open_movie_Click;
                    imageButton.Height = 120;
                    imageButton.Width = 120;
                    imageButton.Tag = mc.FilePath;

                    Label episodeNameLabel = new Label();
                    episodeNameLabel.Text = mc.EpisodName;
                    episodeNameLabel.Width = 120;
                    episodeNameLabel.Height = 20;

                    Button playButton = new Button();
                    playButton.Click += open_movie_Click;
                    playButton.Height = 20;
                    playButton.Width = 50;
                    playButton.Tag = mc.FilePath;
                    playButton.Text = "Spela";

                    Button deleteEpisode = new Button();
                    deleteEpisode.Click += removeEpisodeeButton_Click;
                    deleteEpisode.Height = 20;
                    deleteEpisode.Width = 50;
                    deleteEpisode.Tag = mc.EpisodName;
                    deleteEpisode.Text = "Ta bort";

                    episodeLibraryPanel.Controls.Add(imageButton);
                    episodeLibraryPanel.Controls.Add(episodeNameLabel);
                    episodeLibraryPanel.Controls.Add(playButton); 
                    episodeLibraryPanel.Controls.Add(deleteEpisode); 
                    flowLayoutPanel4.Controls.Add(episodeLibraryPanel);


                }
                catch (Exception es)
                { }
            }

        }
        private void library_Click(object sender, EventArgs e)
        {
            Button CatalougeButton = (Button)sender;
            string theTagIsLost = ((Label)((FlowLayoutPanel)CatalougeButton.Parent).Controls[1]).Text;
            loadEpisodeListFromLibrary(theTagIsLost);
            
        }
        
        private void removeEpisodeeButton_Click(object sender, EventArgs e)
        {
            Button episodeButton = (Button)sender;
            string episodeName = ((Label)((FlowLayoutPanel)episodeButton.Parent).Controls[1]).Text;
            string catalougeName = ((Label)(flowLayoutPanel4.Controls[0])).Text;

            DialogResult dialogResult = MessageBox.Show("Ta bort episod:" + episodeName, "Ta bort!", MessageBoxButtons.YesNo);
            if(dialogResult == DialogResult.Yes)
            {
                Moviedatabase mb = new Moviedatabase();
                mb.delete_Episode(catalougeName, episodeName);

            }
            loadEpisodeListFromLibrary(catalougeName);
        }
        private void removeCatalougeButton_Click(object sender, EventArgs e)
        {
            Button CatalougeButton = (Button)sender;
            string theTagIsLost = ((Label)((FlowLayoutPanel)CatalougeButton.Parent).Controls[1]).Text;
            Moviedatabase mb = new Moviedatabase();

            List<movieEpisode> me = new List<movieEpisode>();
            me = mb.get_episodeList(theTagIsLost);
            DialogResult dialogResult = MessageBox.Show("Ta bort katalog:" + theTagIsLost,"Ta bort!" ,MessageBoxButtons.YesNo);
            if(dialogResult == DialogResult.Yes)
            {
                if(me.Count > 0)
                {
                    MessageBox.Show("Ta bort alla episoder först!");
                }else
                {
                    mb.delete_MovieCatalogue(theTagIsLost);
                    loadLibraryCatalougeList();

                }
            }
        }
        private void loadLibraryCatalougeList()
        {
            flowLayoutPanel3.Controls.Clear();
            Moviedatabase mb = new Moviedatabase();

            List<movieCatalouge> me = new List<movieCatalouge>();
            me = mb.get_all_Catalouges();
            foreach (movieCatalouge mc in me)
            {
                try
                {
                    FlowLayoutPanel libraryCatalougePanel = new FlowLayoutPanel();
                    libraryCatalougePanel.Width = 130;
                    libraryCatalougePanel.Height = 140;
                    libraryCatalougePanel.BorderStyle = BorderStyle.FixedSingle;

                    Button b = new Button();
                    b.Image = Base64ToImage(mc.CatalougeImage);
                    b.Click += library_Click;
                    b.Height = 80;
                    b.Width = 120;
                    b.Tag = mc.CatalougeName;

                    Label libraryCatalougeName = new Label();
                    libraryCatalougeName.Text = mc.CatalougeName;
                    libraryCatalougeName.Height = 20;
                    libraryCatalougeName.Width = 110;

                    libraryCatalougePanel.Controls.Add(b);
                    libraryCatalougePanel.Controls.Add(libraryCatalougeName);

                    Button showCatalougeButton = new Button();
                    showCatalougeButton.Tag = libraryCatalougeName;
                    showCatalougeButton.Click += library_Click;
                    showCatalougeButton.Text = "Visa alla";
                    showCatalougeButton.Width = 55;
                    showCatalougeButton.Height = 20;
                    libraryCatalougePanel.Controls.Add(showCatalougeButton);

                    Button removeCatalougeButton = new Button();
                    removeCatalougeButton.Tag = libraryCatalougeName;
                    removeCatalougeButton.Click += removeCatalougeButton_Click;
                    removeCatalougeButton.Text = "Ta bort";
                    removeCatalougeButton.Width = 55;
                    removeCatalougeButton.Height = 20;
                    libraryCatalougePanel.Controls.Add(removeCatalougeButton);

                    flowLayoutPanel3.Controls.Add(libraryCatalougePanel);
                }
                catch (Exception es)
                { }

            }
        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabPage2"])
            {
                loadLibraryCatalougeList();
               
            }
          
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            svtProgressLabel.Text = "Hämtar data från SVT";
            getSVTTVGuide();
        }


       
    }
}
