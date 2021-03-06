﻿using DevExpress.Xpf.Charts;
using KryGamesBotControls.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KryGamesBotControls.Common
{
    /// <summary>
    /// Interaction logic for ProfitChart.xaml
    /// </summary>
    public partial class ProfitChart : UserControl
    {
        public int MaxItems { get; set; } = -1;
        RealTimeDataCollection DataPoints = new RealTimeDataCollection();
        public ProfitChart()
        {
            InitializeComponent();
            
            series.DataContext = DataPoints;
            //this.seriesIndex++;
            series.ArgumentScaleType = ScaleType.Numerical;
            series.DataSourceSorted = true;
            series.LineStyle = new LineStyle(1);
            series.CrosshairLabelPattern = "{S}: {V:0.00000000}";            
            series.MarkerSize = 3;
            
            bool dark = Helpers.ThemesProviderExtension.IsDark();
            trendSegmentColorizer.RisingTrendColor = dark? Colors.LightGreen: Colors.Green;
            trendSegmentColorizer.FallingTrendColor = dark? Colors.Brown : Colors.Red;
            series.Colorizer = new PointTrendColorizer() { FallingColor = Colors.Red, RisingColor = Colors.Green };
            series.DataSource = DataPoints;
            
            DataPoints.Add(new SimpleDataPoint(ChartItems++, 0));
        }
        
        int ChartItems = 0;
        decimal ChartProfit = 0;
        delegate void dAddPoint(decimal profit);
        public bool Enabled { get; set; } = true;
        public void AddPoint(decimal Profit)
        {
            if (Enabled)
            {
                if (!Dispatcher.CheckAccess())
                    Dispatcher.BeginInvoke(new dAddPoint(AddPoint), Profit);
                else
                {
                    ChartProfit += Profit;
                    DataPoints.Add(new SimpleDataPoint(ChartItems++, (double)ChartProfit));
                    //profitChart.AddPoint(ChartItems++, (double)Profit);
                    while (DataPoints.Count > (MaxItems>0?MaxItems: UISettings.Settings.ChartBets))
                    {
                        DataPoints.RemoveRangeAt(0, (DataPoints.Count - (MaxItems > 0 ? MaxItems : UISettings.Settings.ChartBets)) + 1);
                    }
                }
            }
        }
        public void AddRange(List<decimal> points)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.BeginInvoke(new Action<List<decimal>>(AddRange), points);
            else
            {
                DataPoints.AddRange(points.Select(m => new SimpleDataPoint(ChartItems++, (double)(ChartProfit += m))).ToList());
                while (DataPoints.Count > (MaxItems > 0 ? MaxItems : UISettings.Settings.ChartBets))
                {
                    DataPoints.RemoveRangeAt(0, (DataPoints.Count - (MaxItems > 0 ? MaxItems : UISettings.Settings.ChartBets)) + 1);
                }
            }
        }
        SolidColorBrush GreenBrush = new SolidColorBrush(Colors.Green);
        SolidColorBrush RedBrush = new SolidColorBrush(Colors.Red);

        public void Reset()
        {
            bool OriginalEnabled = Enabled;
            Enabled = false;
            ChartProfit = 0;
            ChartItems = 0;
            DataPoints.Clear();
            Enabled = OriginalEnabled;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void btnFreeze_Click(object sender, RoutedEventArgs e)
        {
            Enabled = !Enabled;
            if (Enabled)
                btnFreeze.Content = "Freeze Chart";
            else
                btnFreeze.Content = "Resume Chart";
        }
    }
    public class RealTimeDataCollection : ObservableCollection<SimpleDataPoint>
    {
        public void AddRange(IList<SimpleDataPoint> items)
        {
            foreach (SimpleDataPoint item in items)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)items, Items.Count - items.Count));
        }
        public void RemoveRangeAt(int startingIndex, int count)
        {
            var removedItems = new List<SimpleDataPoint>(count);
            for (int i = 0; i < count; i++)
            {
                removedItems.Add(Items[startingIndex]);
                Items.RemoveAt(startingIndex);
            }
            if (count > 0)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, startingIndex));
        }
    }

    public class PointTrendColorizer : ChartColorizerBase
    {
        double prevPointValue = double.NaN;

        public Color FallingColor { get; set; }
        public Color RisingColor { get; set; }

        public override Color? GetPointColor(object argument, object[] values, object colorKey, Palette palette)
        {
            double value = (double)values[0];
            Color resultColor = prevPointValue < value ? RisingColor : FallingColor;
            prevPointValue = value;
            return resultColor;
        }

        protected override ChartDependencyObject CreateObject()
        {
            return new PointTrendColorizer();
        }
    }
}
