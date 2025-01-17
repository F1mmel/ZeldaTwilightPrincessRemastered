using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ZeldaTesting;


    public class TevColorOverride : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<WLinearColor> Colors { get { return m_colors; } }
        public ObservableCollection<WLinearColor> ConstColors { get { return m_kColors; } }
        public ObservableCollection<bool> ColorsEnabled { get { return m_colorsEnabled; } }
        public ObservableCollection<bool> ConstColorsEnabled { get { return m_kColorsEnabled; } }

        private ObservableCollection<WLinearColor> m_colors;
        private ObservableCollection<WLinearColor> m_kColors;
        private ObservableCollection<bool> m_colorsEnabled;
        private ObservableCollection<bool> m_kColorsEnabled;

        public TevColorOverride()
        {
            m_colors = new ObservableCollection<WLinearColor>(new[] { WLinearColor.White, WLinearColor.White, WLinearColor.White, WLinearColor.White });
            m_kColors = new ObservableCollection<WLinearColor>(new[] { WLinearColor.White, WLinearColor.White, WLinearColor.White, WLinearColor.White });
            m_colorsEnabled = new ObservableCollection<bool>(new[] { false, false, false, false });
            m_kColorsEnabled = new ObservableCollection<bool>(new[] { false, false, false, false });
        }

        public void SetTevColorOverride(int index, WLinearColor overrideColor)
        {
            if (index < 0 || index >= 4)
                throw new ArgumentOutOfRangeException("index", "index must be between 0 and 3");

            m_colors[index] = overrideColor;
            m_colorsEnabled[index] = true;
        }

        public void SetTevkColorOverride(int index, WLinearColor overrideColor)
        {
            if (index < 0 || index >= 4)
                throw new ArgumentOutOfRangeException("index", "index must be between 0 and 3");

            m_kColors[index] = overrideColor;
            m_kColorsEnabled[index] = true;
        }


        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
