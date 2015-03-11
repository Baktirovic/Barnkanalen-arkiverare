
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barnkanalen_arkiverare
{
    class movieEpisode
    {
        public string EpisodName { set; get; }
        public string FilePath { set; get; }
        public string episodeImage { set; get; }
        public int currentChunk { get; set; }
        //Full path to downloading episode
        public string fullPathFileNameDownloaded { get; set; }
        //Number of sile segments for this one episode
        public double episodeFileChunkCount { get; set; }
        //Total Number of files chunks for this one episode
        public double numberOfFileChunksDownloaded { get; set; } 
        public List<string> filenames { get; set; }
        public List<string> urls { get; set; } 
    }
}

