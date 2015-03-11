using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Barnkanalen_arkiverare
{
    class Moviedatabase
    {
        string databaseFilename = Directory.GetCurrentDirectory() + "/" + "Library.xml";
        XmlDocument xml = new XmlDocument(); 
        public Moviedatabase()
        { 
            if (File.Exists(databaseFilename))
            {

                xml.Load( databaseFilename); 
            }else
            {
                new XDocument(new XElement("xml", new XElement("Catalogue", ""))).Save(databaseFilename);
                xml.Load( databaseFilename); 
            }
        }
        public void set_NewMovieCatalogue(string catalogueName, string imageName)
        {
            if (!Catalouge_exists(catalogueName))
            { 
                XmlNode root = xml.DocumentElement;
                XmlElement newCatalogue = xml.CreateElement("Catalogue");
                newCatalogue.SetAttribute("name", catalogueName);
                newCatalogue.SetAttribute("image",imageName);
                root.AppendChild(newCatalogue);
            }
        }
        private XmlNode get_movieCatalogue(string catalogueName)
        {
            return xml.DocumentElement.SelectSingleNode("//Catalogue[@name='" + catalogueName + "']");
        }
        public void delete_MovieCatalogue(string catalogueName)
        {
            if(Catalouge_exists(catalogueName))
            {
                XmlNode deleteNode = xml.SelectSingleNode("//Catalogue[@name='" + catalogueName + "']");
                XmlNode root = xml.DocumentElement;
                if(deleteNode != null)
                {
                    root.RemoveChild(deleteNode);
                    xml.Save(databaseFilename);
                }
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
        public string set_NewMovieEpisode(string CatalougeName, string catalougeImage, string episodeName, string filePath, string imagePath)
        {
            string eturner = "";
            try
            { 
                XmlNode Catalouge = get_movieCatalogue(CatalougeName);
                if (Catalouge == null)
                {
                    set_NewMovieCatalogue(CatalougeName, catalougeImage);
                    Catalouge = get_movieCatalogue(CatalougeName);
                } 
                if (get_episode(CatalougeName, episodeName) == null)
                {
                    XmlElement newElement = xml.CreateElement("Episode");
                    newElement.SetAttribute("name", episodeName);
                    newElement.SetAttribute("filepath", filePath);
                    newElement.SetAttribute("image", imagePath);
                    Catalouge.AppendChild(newElement);
                    eturner = xml.ToString();
                }
                xml.Save(databaseFilename);
            }catch(Exception es)
            {
                eturner = es.Message;
            }
            return eturner;
        }
        public XmlNode get_episode(string catalougeName, string episodeName)
        { 
            XmlNode Catalogue = get_movieCatalogue(catalougeName);
            if (Catalogue == null)
                return null;
            XmlNode episodeElement = Catalogue.SelectSingleNode(".//Episode[@name='" + episodeName + "']");
            return episodeElement;
        }
        public void delete_Episode(string catalougeName, string episodeName)
        {
            XmlNode Catalogue = get_movieCatalogue(catalougeName);
            XmlNode Episode = get_episode(catalougeName, episodeName);

            if (Episode != null)
            {
                File.Delete(Episode.Attributes["filepath"].ToString());
                Catalogue.RemoveChild(Episode);
                xml.Save(databaseFilename);
            }

        }
        public bool Catalouge_exists(string name)
        {
            bool nameExists= false;
            XmlNodeList xnList = xml.SelectNodes("//Catalogue");

            foreach (XmlNode xn in xnList)
            {
                try
                {
                    if (xn.Attributes["name"].Value == name)
                        nameExists = true;
                }
                catch (Exception es) { }
            }
            return nameExists;
        }
        public List<movieCatalouge> get_all_Catalouges()
        {
            List<movieCatalouge> all_Catalouges = new List<movieCatalouge>();
           
            XmlNodeList xnList = xml.SelectNodes("//Catalogue");

            foreach (XmlNode xn in xnList)
            {
                try
                {
                    movieCatalouge tmp_movieCatalouge = new movieCatalouge();
                    tmp_movieCatalouge.CatalougeName = xn.Attributes["name"].Value;
                    tmp_movieCatalouge.CatalougeImage = xn.Attributes["image"].Value;

                    all_Catalouges.Add(tmp_movieCatalouge);       
                }
                catch (Exception es) { }
            }
            return all_Catalouges;
        }
        public List<movieEpisode> get_episodeList(string CatalougeName)
        {
            List<movieEpisode> episodeList = new List<movieEpisode>();
            XmlNode Catalogue = get_movieCatalogue(CatalougeName);
            XmlNodeList xnList = Catalogue.SelectNodes(".//Episode");

            foreach (XmlNode xn in xnList)
            {
                movieEpisode tmp_episode = new movieEpisode();
                tmp_episode.EpisodName = xn.Attributes["name"].Value;
                tmp_episode.episodeImage = xn.Attributes["image"].Value;
                tmp_episode.FilePath = xn.Attributes["filepath"].Value;
                episodeList.Add(tmp_episode);
            
            }
            return episodeList;
        }


    }
    
  
}
