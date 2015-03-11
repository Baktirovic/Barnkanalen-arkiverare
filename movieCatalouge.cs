using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barnkanalen_arkiverare
{
    class movieCatalouge
    {
        public string CatalougeName { set; get; }
        public string CatalougeImage { set; get; }
        public List<movieEpisode> episodes { set; get; }

        void movieEpisode(string downLoadedName)
        {
            List<movieEpisode> episodes = new List<movieEpisode>();
            CatalougeName = downLoadedName;
        }

    }
}
