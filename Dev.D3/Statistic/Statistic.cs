using Enigma.D3.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Statistic
{
    public class Statistic : INotifyPropertyChanged
    {

        public int DtStartRun { get; set; }


        public int StartGold { get; set; }
        public int CurrentGold { get; set; }

        public List<StatisticItens> StatisticItensList { get; set; }

        public int GoldEarned
        {
            get { return CurrentGold - StartGold; }
        }



        /* INotifyPropertyChanged */

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void Refresh(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh()
        {
            PropertyChanged(this, new PropertyChangedEventArgs(null));
        }

    }
}
