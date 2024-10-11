using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfWLTrend.Properties;

namespace WpfWLTrend
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClsSqlConfig _sqlconfig;

        List<string> cmbWLMonthTableList;

        private int iLayerCount;
        public int DefaultLayersCount = 16;
        public string totalcount;
        public int XColumns;

        public List<string> BalerList { get; set; }

        public ObservableCollection<ChartData> _Pos1 { get; set; }
        public ObservableCollection<ChartData> _Pos2 { get; set; }
        public ObservableCollection<ChartData> _Pos3 { get; set; }
        public ObservableCollection<ChartData> _Pos4 { get; set; }

        public double LayerAvg;
        public double LayerMax;
        public double LayerMin;

        public double MaximumHeight;
        public double MinimumHeight;

        public DataTable WetLayerDeltaTable;

        public MainWindow()
        {
            InitializeComponent();

           _sqlconfig = new ClsSqlConfig();

            cmbWLMonthTableList = new List<string>();

            cmbWLMonthTableList = _sqlconfig.GetAllWLTables();
            cmbWLMonthTable.ItemsSource = cmbWLMonthTableList;

            iSampleBales.Minimum = 3;
            iSampleBales.Maximum = 1000;

            iSampleBales.Text = "200";

            cmbBalerLst.IsEnabled = false;
            cbBaler.IsChecked = false;
        }

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextNumeric(e.Text);
        }
        private static bool IsTextNumeric(string str)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9.]+");
            return reg.IsMatch(str);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainWindow_Load(object sender, RoutedEventArgs e)
        {


            cmbWLMonthTableList = _sqlconfig.GetAllWLTables();

            if(cmbWLMonthTableList.Count > 0)
            {
                cmbWLMonthTable.ItemsSource = cmbWLMonthTableList;
                cmbWLMonthTable.SelectedIndex = 0;
                
                BalerList = GetBalerList(cmbWLMonthTableList[0]);
                if(BalerList.Count > 0)
                {
                    cmbBalerLst.ItemsSource = BalerList;
                    cmbBalerLst.SelectedIndex = 0;
                }
            }

            tbStatus.Text = "Connected to " + Settings.Default.LocalHost;
           
        }

        private List<string> GetBalerList(string selectTableValue)
        {
            return _sqlconfig.GetUniqueStrItemlist("BalerID", selectTableValue);
        }

        private void btnApply_click(object sender, RoutedEventArgs e)
        {
            GetWLDataGridview(cmbWLMonthTable.Text, iSampleBales.Text, cmbCurrent.Text);
        }

        private void GetWLDataGridview(object selectedValue, string balecount, string stroccr)
        {
            string strOccr = string.Empty;
            string strAllColumns = "*";

            if (stroccr == "Latest") strOccr = " ORDER BY ReadTime DESC ;";
            else strOccr = " ORDER BY ReadTime ASC ;";

            string strQuery = "SELECT TOP " + balecount + " " + strAllColumns + " From " + selectedValue + " with (NOLOCK) " + strOccr;


            try
            {
                DataTable WLDataTable = new DataTable();
                WLDataTable = _sqlconfig.GetNewWLDataTable(selectedValue, strQuery);

                if (WLDataTable.Rows.Count > 0)
                {
                    ProccessData(WLDataTable);
                    tbStatus.Text = "Each Layers are from the average of " + WLDataTable.Rows.Count.ToString() + " Bales";
                    xAxis.Title = "Layers Trend of " + WLDataTable.Rows.Count.ToString() + " Bales from " + selectedValue;

                    txttbInfo1.Text = WLDataTable.Rows.Count.ToString();
                }
                else
                {
                    MessageBox.Show("No Record found in  = " + selectedValue);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in GetWLDataGridview = " + ex.Message);
            }
        }

        private void ProccessData(DataTable wLDataTable)
        {
            int irows = wLDataTable.Rows.Count;
            List<Double> ListAvg = new List<double>();

            List<Double> ListDelAvg = new List<double>();


            if (wLDataTable.Rows[0]["Layers"] == null)
                iLayerCount = DefaultLayersCount;
            else
                iLayerCount = Convert.ToInt32(wLDataTable.Rows[0]["Layers"].ToString());

            totalcount = wLDataTable.Rows.Count.ToString();
            XColumns = iLayerCount + 1;

            xAxis.Maximum = XColumns;

            DataTable DtaTable = SetUpDatTable(XColumns);

            //Array of double for each layers.
            List<double>[] fLayer = new List<double>[iLayerCount];

            SetNewChart();

            try
            {
                for (int y = 0; y < iLayerCount; y++)
                {
                    fLayer[y] = new List<double>();
                    for (int i = 0; i < wLDataTable.Rows.Count; i++)
                    {
                        if (wLDataTable.Rows[i]["Layer" + (y + 1).ToString()].ToString() != String.Empty)
                            fLayer[y].Add(ConvToMR(Convert.ToDouble(wLDataTable.Rows[i]["Layer" + (y + 1).ToString()].ToString())));
                    }
                }
                DtaTable.Rows.Add("MR.%");
                for (int i = 1; i < iLayerCount + 1; i++)
                {
                    if (fLayer[i - 1].Count > 1)
                    {
                       // _Pos1.Add(new ChartData() { Index = i, Value = fLayer[i - 1].Average() });
                        ListAvg.Add(fLayer[i - 1].Average());
                        DtaTable.Rows[0][i] = fLayer[i - 1].Average().ToString("#0.00");
                    }
                }

                LayerAvg = Convert.ToDouble(ListAvg.Average().ToString("#0.00"));
                LayerMax = Convert.ToDouble(ListAvg.Max().ToString("#0.00"));
                LayerMin = Convert.ToDouble(ListAvg.Min().ToString("#0.00"));



                DtaTable.Rows.Add("Avg.- MR.%");
                for (int i = 1; i < iLayerCount + 1; i++)
                {
                    if (fLayer[i - 1].Count > 1)
                    {
                       
                        DtaTable.Rows[1][i] = (fLayer[i - 1].Average() - LayerAvg).ToString("#0.00");
                        ListDelAvg.Add((fLayer[i - 1].Average() - LayerAvg));
                        _Pos1.Add(new ChartData() { Index = i, Value = (fLayer[i - 1].Average() - LayerAvg) });
                    }
                }

                
                double LayerAvgD = Convert.ToDouble(ListDelAvg.Average().ToString("#0.00"));
                double LayerMaxD = Convert.ToDouble(ListDelAvg.Max().ToString("#0.00"));
                double LayerMinD = Convert.ToDouble(ListDelAvg.Min().ToString("#0.00"));
                

                txtLayerAvg.Text = LayerAvgD.ToString();
                txtLayerMax.Text = LayerMaxD.ToString();
                txtLayerMin.Text = LayerMinD.ToString();

                yAxis.Maximum = LayerMaxD + 0.4;
                yAxis.Minimum = LayerMinD - 0.4;


                for (int i = 0; i < iLayerCount + 2; i++)
                {
                    _Pos2.Add(new ChartData() { Index = i, Value = LayerAvgD });
                    _Pos3.Add(new ChartData() { Index = i, Value = LayerMaxD });
                    _Pos4.Add(new ChartData() { Index = i, Value = LayerMinD });
                }

                ((LineSeries)ChrtSer1).DataContext = _Pos1;
                ((LineSeries)ChrtAvg).DataContext = _Pos2;
                ((LineSeries)ChrtMax).DataContext = _Pos3;
                ((LineSeries)ChrtMin).DataContext = _Pos4;

                WetLayerDataGrid.DataContext = DtaTable;

            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in ProccessData = " + ex.Message);
            }
        }
        private double ConvToMR(double fMoisture)
        {
            return (fMoisture / (1 - fMoisture / 100));
        }

        private void SetNewChart()
        {
            if (_Pos1 != null) _Pos1 = null;
            _Pos1 = new ObservableCollection<ChartData>();

            if (_Pos2 != null) _Pos2 = null;
            _Pos2 = new ObservableCollection<ChartData>();

            if (_Pos3 != null) _Pos3 = null;
            _Pos3 = new ObservableCollection<ChartData>();

            if (_Pos4 != null) _Pos4 = null;
            _Pos4 = new ObservableCollection<ChartData>();
        }

        private DataTable SetUpDatTable(int LayersColumn)
        {
            DataTable NewTable = new DataTable();
            DataColumn[] DatColumn = new DataColumn[LayersColumn];

            DatColumn[0] = new DataColumn("Type", typeof(string));
            NewTable.Columns.Add(DatColumn[0]);

            for (int i = 1; i < LayersColumn; i++)
            {
                DatColumn[i] = new DataColumn("Layer" + i.ToString(), typeof(double));
                NewTable.Columns.Add(DatColumn[i]);
                //Console.WriteLine("DatColumn[i] = " + DatColumn[i] + " i= " + i);
            }
            return NewTable;
        }

        private void cbBaler_checked(object sender, RoutedEventArgs e)
        {
            cmbBalerLst.IsEnabled = true;
        }

        private void cbBaler_unchecked(object sender, RoutedEventArgs e)
        {
            cmbBalerLst.IsEnabled = false;
        }
    }

    /// <summary>
    /// Select different colors for Bar Graph
    /// </summary>
    public class ChartData //: BindableBase
    {
        //private int _Index;
        public int Index { get; set; }
        // private double _value;
        public double Value { get; set; }
        // private Brush _color;
        public Brush ChartColor { get; set; }
      
    }
}
